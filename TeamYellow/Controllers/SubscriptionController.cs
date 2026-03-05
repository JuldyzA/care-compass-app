using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Repositories;
using TeamYellow.Services;

namespace TeamYellow.Controllers;

[Authorize]
public class SubscriptionController(
    PayPalService payPalService,
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager,
    IPlanRepository planRepository,
    ISubscriptionRepository subscriptionRepository,
    ITransactionRepository transactionRepository) : Controller
{
    private readonly PayPalService _payPalService = payPalService;
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly IPlanRepository _planRepository = planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository = subscriptionRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;

    [HttpPost]
    public async Task<IActionResult> Subscribe(int planId)
    {
        var plan = await _planRepository.GetPlanById(planId);
        if (plan == null) return NotFound();

        var returnUrl = Url.Action("Success", "Subscription", new { planId }, Request.Scheme);
        var cancelUrl = Url.Action("Cancel", "Subscription", null, Request.Scheme);

        try
        {
            var approvalUrl = await _payPalService.CreateOrder(plan.Price, "CAD", returnUrl!, cancelUrl!);
            return Redirect(approvalUrl);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error creating payment: {ex.Message}");
        }
    }

    public async Task<IActionResult> Success(string token, int planId)
    {
        try
        {
            var captureId = await _payPalService.CaptureOrder(token);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var counsellor = await _context.Counsellors.FirstOrDefaultAsync(c => c.UserId == user.Id);
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
        catch (Exception ex)
        {
            return RedirectToAction("Index", "Home", new { error = $"Payment failed: {ex.Message}" });
        }
    }

    public IActionResult Cancel()
    {
        return View();
    }
}
