// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TeamYellow.Data;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.Services;

namespace TeamYellow.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly CounsellorRepository _counsellorRepository;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ReCAPTCHA.ReCaptchaValidator _reCaptchaValidator;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<IdentityUser> userStore,
            CounsellorRepository counsellorRepository,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailService emailService,
            IConfiguration configuration,
            ApplicationDbContext context,
            ReCAPTCHA.ReCaptchaValidator reCaptchaValidator)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _counsellorRepository = counsellorRepository;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
            _context = context;
            _reCaptchaValidator = reCaptchaValidator;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required]
            [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }
        }

        /// <summary>
        /// Loads the registration page and prepares external login providers and reCAPTCHA settings.
        /// </summary>
        /// <param name="returnUrl">The URL to return to after registration completes.</param>
        public async Task OnGetAsync(string returnUrl = null)
        {
            ViewData["SiteKey"] = _configuration["Recaptcha:SiteKey"];

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        /// <summary>
        /// Processes the registration form submission, validates reCAPTCHA, creates the identity user,
        /// assigns the default role, creates the related user profile, and sends a confirmation email.
        /// </summary>
        /// <param name="returnUrl">The URL to return to after registration completes.</param>
        /// <returns>
        /// A redirect to the confirmation page or return URL when successful; otherwise returns the current page
        /// with validation or processing errors.
        /// </returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {           
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            ViewData["SiteKey"] = _configuration["Recaptcha:SiteKey"];
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            string captchaResponse = Request.Form["g-recaptcha-response"];
            string secret = _configuration["Recaptcha:SecretKey"] ?? string.Empty;

            ReCAPTCHA.ReCaptchaValidationResult resultCaptcha = await _reCaptchaValidator.IsValidAsync(secret, captchaResponse);
            
            if (!resultCaptcha.Success)
            {
                var serviceFailure = resultCaptcha.ErrorCodes.Contains("http-request-failed") ||
                    resultCaptcha.ErrorCodes.Contains("request-timeout") ||
                    resultCaptcha.ErrorCodes.Any(e => e.StartsWith("http-"));

                if (serviceFailure)
                {
                    _logger.LogWarning("Registration could not verify reCAPTCHA for email {Email} due to a verification service issue. ErrorCodes: {ErrorCodes}", 
                        Input.Email, string.Join(", ", resultCaptcha.ErrorCodes));

                    ModelState.AddModelError(string.Empty, "reCAPTCHA verification is temporarily unavailable. Please try again.");
                }
                else
                {
                    _logger.LogWarning("Registration blocked due to invalid reCAPTCHA for email {Email}. ErrorCodes: {ErrorCodes}", 
                        Input.Email, string.Join(", ", resultCaptcha.ErrorCodes));

                    ModelState.AddModelError(string.Empty, "The reCAPTCHA is invalid.");
                }
            }

            string normalizedEmail = _userManager.NormalizeEmail(Input.Email?.Trim());

            if (!string.IsNullOrWhiteSpace(normalizedEmail) && await _counsellorRepository.DeletedCounsellorExistsByNormalizedEmailAsync(normalizedEmail))
            {
                _logger.LogWarning("Registration attempt blocked for email {Email} because it is associated with a deleted practitioner account.", Input.Email);

                ViewData["SiteKey"] = _configuration["Recaptcha:SiteKey"];
                ModelState.AddModelError(string.Empty, "We are unable to process your registration with this email address. Please contact support if the problem persists.");
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                IDbContextTransaction transaction = null;

                try
                {
                    transaction = await _context.Database.BeginTransactionAsync();

                    var result = await _userManager.CreateAsync(user, Input.Password);
                    if (!result.Succeeded)
                    {
                        var createErrors = string.Join("; ", result.Errors.Select(e => $"[{e.Code}] {e.Description}"));
                        _logger.LogWarning("Registration failed for email {Email} due to identity validation errors. Errors: {Errors}", Input.Email, createErrors);

                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        await transaction.RollbackAsync();
                        return Page();
                    }

                    _logger.LogInformation("User created a new account with password. UserId: {UserId}", user.Id);

                    const string registeredVisitorRole = "Registered_Visitor";

                    if (!await _roleManager.RoleExistsAsync(registeredVisitorRole))
                    {
                        _logger.LogError(
                            "Required role {Role} does not exist during registration for email {Email}.",
                            registeredVisitorRole,
                            Input.Email);

                        ModelState.AddModelError(
                            string.Empty,
                            "Registration is temporarily unavailable. Please try again later.");

                        await transaction.RollbackAsync();
                        return Page();
                    }

                    var roleResult = await _userManager.AddToRoleAsync(user, registeredVisitorRole);
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join("; ", roleResult.Errors.Select(e => $"[{e.Code}] {e.Description}"));

                        _logger.LogWarning("Failed to add user {UserId} to role {Role}. Errors: {Errors}", user.Id, registeredVisitorRole, roleErrors);

                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        await transaction.RollbackAsync();
                        return Page();
                    }

                    var userProfile = new UserProfile
                    {
                        UserId = user.Id,
                        FirstName = Input.FirstName,
                        LastName = Input.LastName,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.UserProfiles.Add(userProfile);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation("Registration completed successfully for user {UserId}.", user.Id);
                }
                catch (Exception ex)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync();
                    }
                    _logger.LogError(ex, "Error occurred during registration for email {Email}.", Input.Email);
                    ModelState.AddModelError(string.Empty, "An error occurred while creating your account. Please try again.");
                    return Page();
                }
                finally
                {
                    if (transaction != null)
                    {
                        await transaction.DisposeAsync();
                    }
                }

                try
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId, code, returnUrl },
                        protocol: Request.Scheme);

                    ComposeEmailModel payload = new ComposeEmailModel
                    {
                        Email = Input.Email,
                        Subject = "Confirm your email - CareCompass",
                        Body = EmailTemplateService.GenerateRegistrationConfirmationEmail(callbackUrl)
                    };

                    using var response = await _emailService.SendEmailAsync(payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email for user {UserId}", user.Id);

                    var deleteResult = await _userManager.DeleteAsync(user);
                    if (!deleteResult.Succeeded)
                    {
                        _logger.LogError("Failed to delete user {UserId} after email send failure.", user.Id);
                        ModelState.AddModelError(string.Empty, "We couldn't send the confirmation email, and your account " +
                            "may already exist. Please contact support for assistance.");
                    }
                    else
                    {
                        _logger.LogWarning("Registration rolled back logically after email failure for user {UserId}.", user.Id);
                        ModelState.AddModelError(string.Empty, "We couldn't send the confirmation email. Please try registering again later.");
                    }
                    return Page();
                }
                

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                }
                else
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            return Page();
        }

        /// <summary>
        /// Creates a new <see cref="IdentityUser"/> instance for registration.
        /// </summary>
        /// <returns>A new <see cref="IdentityUser"/> instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an <see cref="IdentityUser"/> instance cannot be created.
        /// </exception>
        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        /// <summary>
        /// Returns the email-enabled user store required by the default Identity registration flow.
        /// </summary>
        /// <returns>An <see cref="IUserEmailStore{IdentityUser}"/> instance.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the configured user store does not support email.
        /// </exception>
        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
