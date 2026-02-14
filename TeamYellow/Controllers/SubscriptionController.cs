using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;
using TeamYellow.Services;

namespace TeamYellow.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly PayPalService _payPalService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public SubscriptionController(PayPalService payPalService, ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _payPalService = payPalService;
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(int planId)
    {
        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null) return NotFound();

        // Create PayPal Order
        var returnUrl = Url.Action("Success", "Subscription", new { planId = planId }, Request.Scheme);
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

    public async Task<IActionResult> Success(string token, string PayerID, int planId)
    {
        try
        {
            var captureId = await _payPalService.CaptureOrder(token);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Find existing Counsellor Profile for this user
            // Assuming 1-to-1 rel based on userId string on Counsellor
            var counsellor = await _context.Counsellors.FirstOrDefaultAsync(c => c.UserId == user.Id);
            
            if (counsellor == null)
            {
                // Should handle creating one if needed, but assuming user is a Counsellor
                // Here we fallback or error out
                return RedirectToAction("Index", "Home", new { error = "Counsellor profile not found." });
            }

            var plan = await _context.Plans.FindAsync(planId);

            // Create Subscription
            var subscription = new Subscription
            {
                CounsellorId = counsellor.CounsellorId,
                PlanId = planId,
                Status = SubscriptionStatus.Active,
                CycleStart = DateTime.UtcNow,
                CycleEnd = DateTime.UtcNow.AddMonths(1),
                UpdatedAt = DateTime.UtcNow
            };
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // Create Payment Transaction
            var transaction = new PaymentTransaction
            {
                SubscriptionId = subscription.SubscriptionId,
                PayerName = user.UserName ?? "Unknown",
                Amount = plan!.Price,
                Currency = "CAD",
                Provider = "PayPal",
                ProviderOrderId = captureId, // The ID returned from CaptureOrder
                Status = PaymentTransactionStatus.Captured,
                PaidAt = DateTime.UtcNow
            };
            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

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
