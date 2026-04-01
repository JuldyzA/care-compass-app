// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TeamYellow.Models;
using TeamYellow.Services;

namespace TeamYellow.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailModel> _logger;

        public EmailModel(
            UserManager<IdentityUser> userManager,
            IEmailService emailService,
            ILogger<EmailModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

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
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "New email")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            SetParentLayout();
            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                SetParentLayout();
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            // Use case-insensitive comparison to check if the email has actually changed
            if (!string.Equals(Input.NewEmail, email, StringComparison.OrdinalIgnoreCase))
            {
                // Check if the new email already exists in the database (for a different user)
                var existingUser = await _userManager.FindByEmailAsync(Input.NewEmail);
                if (existingUser != null && !string.Equals(existingUser.Id, user.Id, StringComparison.Ordinal))
                {
                    _logger.LogWarning("Email change attempt failed: email {NewEmail} is already in use by another user.", Input.NewEmail);
                    TempData["ErrorMessage"] = "This email address is already in use. Please use a different email.";
                    SetParentLayout();
                    await LoadAsync(user);
                    return Page();
                }

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, email = Input.NewEmail, code = code },
                    protocol: Request.Scheme);

                try
                {
                    ComposeEmailModel payload = new ComposeEmailModel
                    {
                        Email = Input.NewEmail,
                        Subject = "Confirm your email change - CareCompass",
                        Body = EmailTemplateService.GenerateEmailChangeConfirmationEmail(callbackUrl)
                    };

                    using var response = await _emailService.SendEmailAsync(payload);
                    _logger.LogInformation("Email change confirmation sent to {Email}", Input.NewEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email change confirmation to {Email}", Input.NewEmail);
                    TempData["ErrorMessage"] = "We couldn't send the confirmation email. Please try again later.";
                    SetParentLayout();
                    await LoadAsync(user);
                    return Page();
                }

                StatusMessage = "Confirmation link to change email sent. Please check your email.";
                return RedirectToPage();
            }

            StatusMessage = "Your email is unchanged.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                SetParentLayout();
                await LoadAsync(user);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);

            try
            {
                ComposeEmailModel payload = new ComposeEmailModel
                {
                    Email = email,
                    Subject = "Confirm your email",
                    Body = $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>."
                };

                using var response = await _emailService.SendEmailAsync(payload);
                _logger.LogInformation("Email verification sent to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email verification to {Email}", email);
                TempData["ErrorMessage"] = "We couldn't send the verification email. Please try again later.";
                SetParentLayout();
                await LoadAsync(user);
                return Page();
            }

            StatusMessage = "Verification email sent. Please check your email.";
            return RedirectToPage();
        }

        /// <summary>
        /// Sets the parent layout based on user authentication status.
        /// For authenticated dashboard users, uses the dashboard layout; otherwise uses the default identity layout.
        /// </summary>
        private void SetParentLayout()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                ViewData["ParentLayout"] = "/Views/Shared/_DashboardLayout.cshtml";
            }
        }
    }
}
