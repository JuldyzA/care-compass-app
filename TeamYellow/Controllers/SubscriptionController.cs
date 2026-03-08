using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Repositories;
using TeamYellow.Services;

namespace TeamYellow.Controllers;

[Authorize(Roles = "Registered_Visitor,Paid_Counselor,Free_Counselor")]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ICounsellorRepository _counsellorRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<SubscriptionController> _logger;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        UserManager<IdentityUser> userManager,
        ICounsellorRepository counsellorRepository,
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
                ViewData["Message"] = result == SubscriptionResult.PlanChanged
                    ? "Your plan has been updated successfully."
                    : "You have successfully subscribed to the Free plan.";
                return View("Success");
            }

            return Redirect(approvalUrl);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            return BadRequest("Unable to process payment. Please try again.");
        }
    }

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
            try
            {
                await AssignCounsellorRole(user.Id, "Paid_Counselor");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign Paid_Counselor role to user {UserId} after PayPal subscription.", user.Id);
            }
            if (result == SubscriptionResult.AlreadySubscribed)
                return RedirectToAction("Index", "Plan", new { error = "You are already subscribed to this plan." });
            ViewData["Message"] = result == SubscriptionResult.PlanChanged
                ? "Your plan has been updated successfully."
                : "Thank you for your subscription. Your plan is now active.";
            return View("Success");
        }
        catch (Exception)
        {
            return RedirectToAction("Index", "Home", new { error = "Payment failed. Please try again." });
        }
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        return View();
    }

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
