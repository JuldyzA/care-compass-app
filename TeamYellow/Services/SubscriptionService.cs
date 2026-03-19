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

        if (!plan.IsActive)
            throw new KeyNotFoundException($"Plan {planId} not found.");

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
            ProviderOrderId = $"FREE-{counsellorId}-{1000 + subscription.SubscriptionId}",
        });

        return existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
    }

    public async Task<SubscriptionResult> SubscribeDiscountedZeroAmount(
    int counsellorId,
    string userName,
    int planId,
    int? discountId)
    {
        var plan = await _planRepository.GetPlanById(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        if (!plan.IsActive)
            throw new KeyNotFoundException($"Plan {planId} not found.");

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
            Provider = "Full Discount",
            ProviderOrderId = $"DISCOUNT-{counsellorId}-{1000 + subscription.SubscriptionId}",
            DiscountId = discountId
        });

        return existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
    }

    /// <summary>
    /// Creates a PayPal checkout order for the specified plan and returns checkout details.
    /// For plans or discounts that result in a non-zero amount, a PayPal order is created so the
    /// buyer can approve the payment. For free plans or plans that become zero after applying a
    /// discount, no PayPal order is created and the subscription is handled as a zero-amount flow.
    /// </summary>
    /// <param name="planId">The ID of the plan to start the checkout process for.</param>
    /// <param name="discountCode">An optional discount code to apply before determining the final amount.</param>
    /// <param name="returnUrl">The URL PayPal redirects to after the buyer approves the payment.</param>
    /// <param name="cancelUrl">The URL PayPal redirects to if the buyer cancels the payment.</param>
    /// <returns>
    /// A <see cref="SubscriptionCheckoutResult"/> describing the checkout flow, including whether
    /// PayPal approval is required and, for paid plans, the buyer approval URL to redirect the user to.
    /// For free or zero-after-discount plans, the result indicates that no PayPal redirect is needed.
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown if the specified plan does not exist.</exception>
    public async Task<SubscriptionCheckoutResult> CreatePayPalOrder(
    int planId,
    string? discountCode,
    string returnUrl,
    string cancelUrl)
    {
        var plan = await _planRepository.GetPlanById(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        if (!plan.IsActive)
            throw new KeyNotFoundException($"Plan {planId} not found.");

        if (plan.Price == 0m)
        {
            return new SubscriptionCheckoutResult
            {
                RequiresPayPal = false,
                IsActualFreePlan = true,
                DiscountId = null,
                ApprovalUrl = null
            };
        }

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

                if (finalAmount < 0m)
                {
                    finalAmount = 0m;
                }

                finalAmount = decimal.Round(finalAmount, 2, MidpointRounding.AwayFromZero);
            }
        }

        if (finalAmount == 0m)
        {
            return new SubscriptionCheckoutResult
            {
                RequiresPayPal = false,
                IsActualFreePlan = false,
                DiscountId = discountId,
                ApprovalUrl = null
            };
        }

        var customId = $"{planId}|{discountId.GetValueOrDefault(0)}";
        var approvalUrl = await _payPalService.CreateOrder(finalAmount, "CAD", returnUrl, cancelUrl, customId);

        return new SubscriptionCheckoutResult
        {
            RequiresPayPal = true,
            IsActualFreePlan = false,
            DiscountId = discountId,
            ApprovalUrl = approvalUrl
        };
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

        if (!plan.IsActive)
            throw new KeyNotFoundException($"Plan {planId} not found.");

        var existing = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellorId);

        if (existing != null && existing.PlanId == planId)
            return SubscriptionResult.AlreadySubscribed;

        if (await _transactionRepository.ExistsByProviderOrderId(captureId))
            return SubscriptionResult.AlreadySubscribed;

        decimal expectedAmount = plan.Price;

        if (discountId.HasValue)
        {
            var discount = await _discountRepository.GetDiscountByIdAsync(discountId.Value);

            if (discount == null)
                throw new InvalidOperationException($"Discount {discountId.Value} not found.");

            var discountAmount = DiscountCalculator.CalculateDiscountAmount(plan.Price, discount);
            expectedAmount = plan.Price - discountAmount;

            if (expectedAmount < 0m)
            {
                expectedAmount = 0m;
            }
        }

        expectedAmount = decimal.Round(expectedAmount, 2, MidpointRounding.AwayFromZero);

        const decimal amountTolerance = 0.01m;

        if (Math.Abs(expectedAmount - capturedAmount) > amountTolerance)
        {
            throw new InvalidOperationException(
                $"Captured amount {capturedAmount:F2} does not match expected amount {expectedAmount:F2}.");
        }

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
