using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Repositories;
using TeamYellow.Services;

namespace TeamYellow.Controllers;

[Authorize(Roles = "Registered_Visitor,Paid_Counselor,Free_Counselor")]
public class SubscriptionController(
    ISubscriptionService subscriptionService,
    UserManager<IdentityUser> userManager,
    ICounsellorRepository counsellorRepository) : Controller
{
    private readonly ISubscriptionService _subscriptionService = subscriptionService;
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly ICounsellorRepository _counsellorRepository = counsellorRepository;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(int planId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
        if (counsellor == null)
            return RedirectToAction("Index", "Home", new { error = "Counsellor profile not found." });

        var returnUrl = Url.Action("Success", "Subscription", null, Request.Scheme);
        var cancelUrl = Url.Action("Cancel", "Subscription", null, Request.Scheme);

        if (returnUrl is null || cancelUrl is null)
            return BadRequest("Unable to generate PayPal redirect URLs.");

        try
        {
            var approvalUrl = await _subscriptionService.CreatePayPalOrder(planId, returnUrl, cancelUrl);

            if (approvalUrl == string.Empty)
            {
                var result = await _subscriptionService.SubscribeFree(counsellor.CounsellorId, user.UserName ?? "Unknown", planId);
                return result switch
                {
                    SubscriptionResult.AlreadySubscribed => RedirectToAction("Index", "Plan", new { error = "You are already subscribed to this plan." }),
                    SubscriptionResult.PlanChanged => View("Success", "Your plan has been updated successfully."),
                    _ => View("Success", "You have successfully subscribed to the Free plan.")
                };
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

    public async Task<IActionResult> Success(string token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Index", "Home", new { error = "Payment token is missing. Please try again." });

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
        if (counsellor == null)
            return RedirectToAction("Index", "Home", new { error = "Counsellor profile not found." });

        try
        {
            var result = await _subscriptionService.CompletePayPalSubscription(token, counsellor.CounsellorId, user.UserName ?? "Unknown");
            return result switch
            {
                SubscriptionResult.AlreadySubscribed => RedirectToAction("Index", "Plan", new { error = "You are already subscribed to this plan." }),
                SubscriptionResult.PlanChanged => View("Success", "Your plan has been updated successfully."),
                _ => View()
            };
        }
        catch (Exception)
        {
            return RedirectToAction("Index", "Home", new { error = "Payment failed. Please try again." });
        }
    }

    public IActionResult Cancel()
    {
        return View();
    }
}
