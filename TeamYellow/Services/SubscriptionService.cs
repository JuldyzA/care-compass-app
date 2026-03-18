using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Repositories;

namespace TeamYellow.Services;

/// <summary>
/// Service implementing subscription management business logic, including
/// free plan subscriptions, PayPal order creation, and PayPal payment capture.
/// Coordinates between the plan, subscription, transaction repositories and the PayPal API service.
/// </summary>
public class SubscriptionService(
    IPlanRepository planRepository,
    ISubscriptionRepository subscriptionRepository,
    ITransactionRepository transactionRepository,
    DiscountRepository discountRepository,
    IPayPalService payPalService) : ISubscriptionService
{
    private readonly IPlanRepository _planRepository = planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository = subscriptionRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly DiscountRepository _discountRepository = discountRepository;
    private readonly IPayPalService _payPalService = payPalService;

    /// <summary>
    /// Subscribes a counsellor to a free plan.
    /// If the counsellor already has an active subscription to a different plan, the existing
    /// subscription is cancelled before the new one is created.
    /// A zero-amount transaction record is created for audit purposes.
    /// </summary>
    /// <param name="counsellorId">The ID of the counsellor being subscribed.</param>
    /// <param name="userName">The username of the counsellor, used as the payer name on the transaction record.</param>
    /// <param name="planId">The ID of the free plan to subscribe the counsellor to.</param>
    /// <returns>
    /// A <see cref="SubscriptionResult"/> indicating whether the subscription was newly created,
    /// represents a plan change from an existing subscription, or was already active.
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown if the specified plan does not exist.</exception>
    public async Task<SubscriptionResult> SubscribeFree(int counsellorId, string userName, int planId)
    {
        var plan = await _planRepository.GetPlanById(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        var existing = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellorId);

        if (existing != null)
        {
            if (existing.PlanId == planId)
                return SubscriptionResult.AlreadySubscribed;

            existing.Status = Models.SubscriptionStatus.Cancelled;
            await _subscriptionRepository.UpdateSubscription(existing);
        }

        var subscription = await _subscriptionRepository.CreateSubscription(new AddSubscriptionDto
        {
            CounsellorId = counsellorId,
            PlanId = planId,
            BillingType = plan.BillingType
        });

        await _transactionRepository.CreateTransaction(new AddTransactionDto
        {
            SubscriptionId = subscription.SubscriptionId,
            PayerName = userName,
            Amount = 0,
            Currency = "CAD",
            Provider = "Free",
            ProviderOrderId = $"FREE-{counsellorId}-{DateTime.UtcNow:yyyyMMddHHmmss}"
        });

        return existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
    }

    /// <summary>
    /// Creates a PayPal checkout order for the specified plan and returns the buyer approval URL.
    /// Returns an empty string if the plan is free (price is 0), indicating that PayPal is not needed.
    /// </summary>
    /// <param name="planId">The ID of the plan to create a PayPal order for.</param>
    /// <param name="returnUrl">The URL PayPal redirects to after the buyer approves the payment.</param>
    /// <param name="cancelUrl">The URL PayPal redirects to if the buyer cancels the payment.</param>
    /// <returns>
    /// The PayPal buyer approval URL for paid plans, or <see cref="string.Empty"/> for free plans.
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown if the specified plan does not exist.</exception>
    public async Task<string> CreatePayPalOrder(int planId, string? discountCode, string returnUrl, string cancelUrl)
    {
        var plan = await _planRepository.GetPlanById(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        if (plan.Price == 0)
            return string.Empty;

        int? discountId = null;
        decimal finalAmount = plan.Price;

        if (!string.IsNullOrWhiteSpace(discountCode))
        {
            var discount = await _discountRepository.GetValidDiscountForPlanAsync(planId, discountCode);
            if (discount != null)
            {
                discountId = discount.DiscountId;
                var discountAmount = DiscountCalculator.CalculateDiscountAmount(plan.Price, discount);
                finalAmount = plan.Price - discountAmount;
            }
        }

        // If the discount fully covers the plan price, treat this like a free plan and do not attempt to create a zero-amount PayPal order.
        if (finalAmount <= 0)
        {
            return string.Empty;
        }

        var customId = $"{planId}|{discountId.GetValueOrDefault(0)}";

        return await _payPalService.CreateOrder(finalAmount, "CAD", returnUrl, cancelUrl, customId);
    }

    /// <summary>
    /// Completes a PayPal subscription by capturing the payment order, creating the subscription record,
    /// and recording the transaction. If the counsellor already has an active subscription to a different plan,
    /// the existing subscription is cancelled and replaced.
    /// Duplicate payment captures are detected via the provider order ID and safely returned as
    /// <see cref="SubscriptionResult.AlreadySubscribed"/>.
    /// </summary>
    /// <param name="token">The PayPal order approval token returned from the PayPal redirect.</param>
    /// <param name="counsellorId">The ID of the counsellor completing the subscription.</param>
    /// <param name="userName">The username of the counsellor, used as the payer name on the transaction record.</param>
    /// <returns>
    /// A <see cref="SubscriptionResult"/> indicating whether the subscription was newly created,
    /// represents a plan change, or was already active (including duplicate capture detection).
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown if the plan embedded in the PayPal custom ID does not exist.</exception>
    /// <exception cref="Exception">Thrown if the PayPal custom ID cannot be parsed as a valid plan identifier.</exception>
    public async Task<SubscriptionResult> CompletePayPalSubscription(string token, int counsellorId, string userName)
    {
        var (captureId, customId, capturedAmount) = await _payPalService.CaptureOrder(token);

        var parts = customId.Split('|', StringSplitOptions.TrimEntries);

        if (parts.Length == 0 || !int.TryParse(parts[0], out var planId))
            throw new Exception("PayPal response contained an invalid plan identifier.");

        int? discountId = null;
        if (parts.Length > 1 && int.TryParse(parts[1], out var parsedDiscountId) && parsedDiscountId > 0)
        {
            discountId = parsedDiscountId;
        }

        var plan = await _planRepository.GetPlanById(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        var existing = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellorId);

        if (existing != null && existing.PlanId == planId)
            return SubscriptionResult.AlreadySubscribed;

        if (await _transactionRepository.ExistsByProviderOrderId(captureId))
            return SubscriptionResult.AlreadySubscribed;

        if (existing != null)
        {
            existing.Status = Models.SubscriptionStatus.Cancelled;
            await _subscriptionRepository.UpdateSubscription(existing);
        }

        var subscription = await _subscriptionRepository.CreateSubscription(new AddSubscriptionDto
        {
            CounsellorId = counsellorId,
            PlanId = planId,
            BillingType = plan.BillingType
        });

        await _transactionRepository.CreateTransaction(new AddTransactionDto
        {
            SubscriptionId = subscription.SubscriptionId,
            PayerName = userName,
            Amount = capturedAmount,
            Currency = "CAD",
            Provider = "PayPal",
            ProviderOrderId = captureId,
            DiscountId = discountId
        });

        return existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
    }
}
