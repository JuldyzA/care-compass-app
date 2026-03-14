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
        SignInManager<IdentityUser> signInManager,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _userManager = userManager;
        _counsellorRepository = counsellorRepository;
        _subscriptionRepository = subscriptionRepository;
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
    public async Task<IActionResult> Subscribe(int planId)
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
            if (counsellor == null)
            {
                string licenceId;
                var random = new Random();
                do
                {
                    licenceId = $"{(char)('A' + random.Next(0, 26))}{random.Next(100000, 1000000)}";
                }
                while (await _counsellorRepository.LicenceIdExistsAsync(licenceId));

                counsellor = await _counsellorRepository.CreateAsync(new Models.Counsellor
                {
                    UserId = user.Id,
                    DisplayName = user.UserName ?? user.Email ?? "Unknown",
                    PractitionerLicenceId = licenceId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var existingSubscription = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellor.CounsellorId);
            if (existingSubscription?.PlanId == planId)
                return RedirectToAction("Index", "Plan", new { error = "You are already subscribed to this plan." });

            var approvalUrl = await _subscriptionService.CreatePayPalOrder(planId, returnUrl, cancelUrl);

            if (approvalUrl == string.Empty)
            {
                var result = await _subscriptionService.SubscribeFree(counsellor.CounsellorId, user.UserName ?? "Unknown", planId);
                try
                {
                    await AssignCounsellorRole(user.Id, "Free_Counselor");
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
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing subscription payment for user {UserId} and plan {PlanId}.",
                user.Id,
                planId);
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
    /// The success view with a confirmation message, or a redirect to the home page with an error
    /// if the payment token is missing, the counsellor profile is not found, or the payment fails.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> Success([FromQuery(Name = "token")] string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
            return RedirectToAction("Index", "Home", new { error = "Payment token is missing. Please try again." });

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
        if (counsellor == null)
            return RedirectToAction("Index", "Home", new { error = "Counsellor profile not found." });

        try
        {
            var result = await _subscriptionService.CompletePayPalSubscription(orderId, counsellor.CounsellorId, user.UserName ?? "Unknown");
            if (result == SubscriptionResult.AlreadySubscribed)
                return RedirectToAction("Index", "Plan", new { error = "You are already subscribed to this plan." });
            try
            {
                await AssignCounsellorRole(user.Id, "Paid_Counselor");
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
        catch (Exception)
        {
            return RedirectToAction("Index", "Home", new { error = "Payment failed. Please try again." });
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
    private async Task AssignCounsellorRole(string userId, string targetRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        var rolesToRemove = new[] { "Registered_Visitor", "Free_Counselor", "Paid_Counselor" };
        var currentRoles = await _userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Intersect(rolesToRemove).ToList();
        if (toRemove.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, toRemove);
        if (!await _userManager.IsInRoleAsync(user, targetRole))
            await _userManager.AddToRoleAsync(user, targetRole);

        await _signInManager.RefreshSignInAsync(user);
    }
}
