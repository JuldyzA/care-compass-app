using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.DTOs;
using TeamYellow.Repositories;
using TeamYellow.Services;

namespace TeamYellow.Controllers;

[Authorize(Roles = "Paid_Counselor,Free_Counselor")]
public class SubscriptionController(
    IPayPalService payPalService,
    UserManager<IdentityUser> userManager,
    ICounsellorRepository counsellorRepository,
    IPlanRepository planRepository,
    ISubscriptionRepository subscriptionRepository,
    ITransactionRepository transactionRepository) : Controller
{
    private readonly IPayPalService _payPalService = payPalService;
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly ICounsellorRepository _counsellorRepository = counsellorRepository;
    private readonly IPlanRepository _planRepository = planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository = subscriptionRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscribe(int planId)
    {
        var plan = await _planRepository.GetPlanById(planId);
        if (plan == null) return NotFound();

        if (plan.Price == 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
            if (counsellor == null)
                return RedirectToAction("Index", "Home", new { error = "Counsellor profile not found." });

            var subscription = await _subscriptionRepository.CreateSubscription(new AddSubscriptionDto
            {
                CounsellorId = counsellor.CounsellorId,
                PlanId = planId,
                BillingType = plan.BillingType
            });

            await _transactionRepository.CreateTransaction(new AddTransactionDto
            {
                SubscriptionId = subscription.SubscriptionId,
                PayerName = user.UserName ?? "Unknown",
                Amount = 0,
                Currency = "CAD",
                Provider = "Free",
                ProviderOrderId = $"FREE-{counsellor.CounsellorId}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            });

            ViewData["Title"] = "Subscription Activated";
            ViewData["Message"] = "You have successfully subscribed to the Free plan.";
            return View("Success");
        }

        var returnUrl = Url.Action("Success", "Subscription", new { planId }, Request.Scheme);
        var cancelUrl = Url.Action("Cancel", "Subscription", null, Request.Scheme);

        try
        {
            var approvalUrl = await _payPalService.CreateOrder(plan.Price, "CAD", returnUrl!, cancelUrl!);
            return Redirect(approvalUrl);
        }
        catch (Exception)
        {
            return BadRequest("Unable to process payment. Please try again.");
        }
    }

    public async Task<IActionResult> Success(string token, int planId)
    {
        try
        {
            var captureId = await _payPalService.CaptureOrder(token);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
            if (counsellor == null)
                return RedirectToAction("Index", "Home", new { error = "Counsellor profile not found." });

            var plan = await _planRepository.GetPlanById(planId);
            if (plan == null) return NotFound();

            var subscription = await _subscriptionRepository.CreateSubscription(new AddSubscriptionDto
            {
                CounsellorId = counsellor.CounsellorId,
                PlanId = planId,
                BillingType = plan.BillingType
            });

            await _transactionRepository.CreateTransaction(new AddTransactionDto
            {
                SubscriptionId = subscription.SubscriptionId,
                PayerName = user.UserName ?? "Unknown",
                Amount = plan.Price,
                Currency = "CAD",
                Provider = "PayPal",
                ProviderOrderId = captureId
            });

            return View();
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
