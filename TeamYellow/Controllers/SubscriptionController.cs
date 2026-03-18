using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Repositories;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers;

/// <summary>
/// Controller responsible for handling subscription workflows,
/// including initiating PayPal payments, processing subscription success,
/// handling cancellations, and assigning counsellor roles.
/// Accessible to authenticated users with the roles
/// <c>Registered_Visitor</c>, <c>Paid_Counselor</c>, or <c>Free_Counselor</c>.
/// </summary>
[Authorize(Roles = "Registered_Visitor,Paid_Counselor,Free_Counselor")]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly CounsellorRepository _counsellorRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly UserProfileRepository _userProfileRepository;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<SubscriptionController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SubscriptionController"/>.
    /// </summary>
    /// <param name="subscriptionService">Service handling subscription business logic.</param>
    /// <param name="userManager">ASP.NET Identity user manager.</param>
    /// <param name="counsellorRepository">Repository for counsellor data access.</param>
    /// <param name="subscriptionRepository">Repository for subscription data access.</param>
    /// <param name="signInManager">ASP.NET Identity sign-in manager used to refresh user claims.</param>
    /// <param name="logger">Logger for recording error and diagnostic information.</param>
    public SubscriptionController(
        ISubscriptionService subscriptionService,
        UserManager<IdentityUser> userManager,
        CounsellorRepository counsellorRepository,
        ISubscriptionRepository subscriptionRepository,
        UserProfileRepository userProfileRepository,
        SignInManager<IdentityUser> signInManager,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _userManager = userManager;
        _counsellorRepository = counsellorRepository;
        _subscriptionRepository = subscriptionRepository;
        _userProfileRepository = userProfileRepository;
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Initiates the subscription process for the specified plan.
    /// Creates a counsellor profile if one does not yet exist for the current user.
    /// For free plans, completes the subscription immediately and assigns the <c>Free_Counselor</c> role.
    /// For paid plans, creates a PayPal order and redirects the user to the PayPal approval page.
    /// </summary>
    /// <param name="planId">The ID of the plan the user wants to subscribe to.</param>
    /// <returns>
    /// Redirects to the PayPal approval URL for paid plans,
    /// returns a success view for free plans, or returns an error result for invalid requests.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(int planId, string? discountCode)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var returnUrl = Url.Action("Success", "Subscription", null, Request.Scheme);
        var cancelUrl = Url.Action("Cancel", "Subscription", null, Request.Scheme);

        if (returnUrl is null || cancelUrl is null)
            return BadRequest("Unable to generate PayPal redirect URLs.");

        try
        {
            var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);

            // Only check active subscription if counsellor already exists
            if (counsellor != null)
            {
                var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellor.CounsellorId);
                if (existingSubscription?.PlanId == planId)
                {
                    TempData["Message"] = "You are already subscribed to this plan.";
                    TempData["MessageType"] = "info";
                    return RedirectToAction("Index", "Plan");
                }
            }

            var approvalUrl = await _subscriptionService.CreatePayPalOrder(planId, discountCode, returnUrl, cancelUrl);

            // Free plan path
            if (approvalUrl == string.Empty)
            {
                counsellor = await EnsureCounsellorAsync(user.Id, user.UserName, user.Email, counsellor);

                var result = await _subscriptionService.SubscribeFree(counsellor.CounsellorId, user.UserName ?? "Unknown", planId);
                try
                {
                    await AssignCounsellorRoleAsync(user.Id, "Free_Counselor");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to assign Free_Counselor role to user {UserId} after free subscription.", user.Id);
                }

                var subscription = await _subscriptionRepository.GetActiveSubscriptionWithPlanByCounsellorId(counsellor.CounsellorId);
                var vm = new SubscriptionSuccessVM
                {
                    Message = result == SubscriptionResult.PlanChanged
                        ? "Your plan has been updated successfully."
                        : "You have successfully subscribed to the Free plan.",
                    SubscriptionId = subscription?.SubscriptionId ?? 0,
                    PlanName = subscription?.Plan?.PlanName ?? string.Empty,
                    CycleStart = subscription?.CycleStart ?? DateTime.UtcNow,
                    CycleEnd = subscription?.CycleEnd ?? DateTime.UtcNow
                };
                return View("Success", vm);
            }

            return Redirect(approvalUrl);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Key not found for user email {Email}. Redirecting to plan selection.", user.Email);
            TempData["Message"] = "We couldn't find the requested subscription information. Please select a plan again.";
            TempData["MessageType"] = "danger";
            return RedirectToAction("Index", "Plan");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing subscription payment for user {UserId} and plan {PlanId}.",
                user.Id,
                planId);
            TempData["Message"] = "An unexpected error occurred while processing your subscription. Please try again.";
            TempData["MessageType"] = "danger";
            return RedirectToAction("Index", "Plan");
        }
    }

    /// <summary>
    /// Handles the PayPal payment return callback after the user approves the order.
    /// Captures the payment, creates the subscription record, and assigns the <c>Paid_Counselor</c> role.
    /// </summary>
    /// <param name="orderId">
    /// The PayPal order token returned as a query string parameter named <c>token</c>
    /// from the PayPal approval redirect.
    /// </param>
    /// <returns>
    /// The success view with a confirmation message, or a redirect to the plan page with an error
    /// if the payment token is missing, the counsellor profile is not found, or the payment fails.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> Success([FromQuery(Name = "token")] string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            TempData["Message"] = "Payment token is missing. Please try again.";
            TempData["MessageType"] = "danger";
            return RedirectToAction("Index", "Plan");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        try
        {
            var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);

            counsellor = await EnsureCounsellorAsync(user.Id, user.UserName, user.Email, counsellor);

            var result = await _subscriptionService.CompletePayPalSubscription(orderId, counsellor.CounsellorId, user.UserName ?? "Unknown");
            if (result == SubscriptionResult.AlreadySubscribed)
            {
                TempData["Message"] = "You are already subscribed to this plan.";
                TempData["MessageType"] = "info";
                return RedirectToAction("Index", "Plan");
            }
            try
            {
                await AssignCounsellorRoleAsync(user.Id, "Paid_Counselor");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign Paid_Counselor role to user {UserId} after PayPal subscription.", user.Id);
            }


            var subscription = await _subscriptionRepository.GetActiveSubscriptionWithPlanByCounsellorId(counsellor.CounsellorId);
            var vm = new SubscriptionSuccessVM
            {
                Message = result == SubscriptionResult.PlanChanged
                    ? "Your plan has been updated successfully."
                    : "Thank you for your subscription. Your plan is now active.",
                SubscriptionId = subscription?.SubscriptionId ?? 0,
                PlanName = subscription?.Plan?.PlanName ?? string.Empty,
                CycleStart = subscription?.CycleStart ?? DateTime.UtcNow,
                CycleEnd = subscription?.CycleEnd ?? DateTime.UtcNow
            };
            return View("Success", vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing subscription for order {OrderId}.", orderId);
            TempData["Message"] = "Payment failed. Please try again.";
            TempData["MessageType"] = "danger";
            return RedirectToAction("Index", "Plan");
        }
    }

    /// <summary>
    /// Handles the PayPal cancellation callback when the user cancels the payment flow.
    /// </summary>
    /// <returns>The cancellation view informing the user that no charge was made.</returns>
    [HttpGet]
    public IActionResult Cancel()
    {
        return View();
    }

    /// <summary>
    /// Removes any existing counsellor-related roles from the user and assigns the specified target role.
    /// Refreshes the user's sign-in cookie so the new role takes effect immediately.
    /// </summary>
    /// <param name="userId">The ID of the user whose roles should be updated.</param>
    /// <param name="targetRole">
    /// The role to assign to the user. Expected values are <c>Free_Counselor</c> or <c>Paid_Counselor</c>.
    /// </param>
    private async Task AssignCounsellorRoleAsync(string userId, string targetRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found while assigning role {TargetRole}.", userId, targetRole);
            return;
        }
        var rolesToRemove = new[] { "Registered_Visitor", "Free_Counselor", "Paid_Counselor" };
        var currentRoles = await _userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Intersect(rolesToRemove).ToList();
        if (toRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                _logger.LogError(
                    "Failed to remove roles {Roles} from user {UserId}. Errors: {Errors}",
                    string.Join(", ", toRemove),
                    userId,
                    errors);

                throw new InvalidOperationException($"Failed to remove existing counsellor roles: {errors}");
            }
        }
        if (!await _userManager.IsInRoleAsync(user, targetRole))
        {
            var addResult = await _userManager.AddToRoleAsync(user, targetRole);
            if (!addResult.Succeeded)
            {
                var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                _logger.LogError(
                    "Failed to add role {TargetRole} to user {UserId}. Errors: {Errors}",
                    targetRole,
                    userId,
                    errors);

                throw new InvalidOperationException($"Failed to assign role {targetRole}: {errors}");
            }
        }

        await _signInManager.RefreshSignInAsync(user);
    }

    private async Task<Models.Counsellor> EnsureCounsellorAsync(string userId, string? userName, string? email, Models.Counsellor? existingCounsellor)
    {
        if (existingCounsellor != null)
        {
            return existingCounsellor;
        }
        string licenceId;
        var random = new Random();
        do
        {
            licenceId = $"{(char)('A' + random.Next(0, 26))}{random.Next(100000, 1000000)}";
        }
        while (await _counsellorRepository.LicenceIdExistsAsync(licenceId));
        var profile = await _userProfileRepository.GetByUserIdAsync(userId);
        var displayName = string.Join(" ", new[]
                        {
                             profile?.FirstName,
                             profile?.LastName
                         }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = userName ?? email ?? "Unknown";
        }
        return await _counsellorRepository.CreateAsync(new Models.Counsellor
        {
            UserId = userId,
            DisplayName = displayName,
            PractitionerLicenceId = licenceId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }
}
