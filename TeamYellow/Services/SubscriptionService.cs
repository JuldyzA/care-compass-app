using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Repositories;
using System.Globalization;

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
    IPayPalService payPalService,
    ApplicationDbContext context) : ISubscriptionService
{
    private readonly IPlanRepository _planRepository = planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository = subscriptionRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly DiscountRepository _discountRepository = discountRepository;
    private readonly IPayPalService _payPalService = payPalService;
    private readonly ApplicationDbContext _context = context;

    /// <summary>
    /// Subscribes a counsellor to a free plan.
    /// If the counsellor already has an active subscription to a different plan, the existing
    /// subscription is cancelled before the new one is created.
    /// A zero-amount transaction record is created for audit purposes.
    /// </summary>
    /// <param name="counsellorId">The ID of the counsellor being subscribed.</param>
    /// <param name="payerName">The name used as the payer name on the transaction.</param>
    /// <param name="planId">The ID of the free plan to subscribe the counsellor to.</param>
    /// <returns>
    /// A <see cref="SubscriptionResult"/> indicating whether the subscription was newly created,
    /// represents a plan change from an existing subscription, or was already active.
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown if the specified plan does not exist.</exception>
    public async Task<SubscriptionResult> SubscribeFree(int counsellorId, string payerName, int planId)
    {
        var plan = await _planRepository.GetByIdWithFeaturesAsync(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        if (!plan.IsActive)
            throw new KeyNotFoundException($"Plan {planId} not found.");

        if (plan.Price != 0m)
            throw new InvalidOperationException($"Plan {planId} is not a free plan.");

        var existing = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellorId);

        if (existing != null && existing.PlanId == planId)
            return SubscriptionResult.AlreadySubscribed;

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
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
                PayerName = payerName,
                Amount = 0m,
                Currency = "CAD",
                Provider = "Free",
                ProviderOrderId = $"FREE-{counsellorId}-{1000 + subscription.SubscriptionId}",
            });

            await tx.CommitAsync();
            return existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<SubscriptionResult> SubscribeDiscountedZeroAmount(
        int counsellorId,
        string payerName,
        int planId,
        int? discountId)
    {
        var plan = await _planRepository.GetByIdWithFeaturesAsync(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        if (!plan.IsActive)
            throw new KeyNotFoundException($"Plan {planId} not found.");

        if (!discountId.HasValue)
            throw new InvalidOperationException(
                "A discount is required for the zero-amount discounted subscription flow.");

        var discount = await _discountRepository.GetValidDiscountForPlanByIdAsync(planId, discountId.Value);

        if (discount == null)
            throw new InvalidOperationException(
                "The selected discount is not valid for this plan.");

        var discountAmount = DiscountCalculator.CalculateDiscountAmount(plan.Price, discount);
        var finalAmount = decimal.Round(
            Math.Max(0m, plan.Price - discountAmount),
            2,
            MidpointRounding.AwayFromZero);

        if (finalAmount != 0m)
            throw new InvalidOperationException("The selected discount does not reduce this plan to zero.");

        var existing = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellorId);

        if (existing != null && existing.PlanId == planId)
            return SubscriptionResult.AlreadySubscribed;

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
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
                PayerName = payerName,
                Amount = 0m,
                Currency = "CAD",
                Provider = "Full Discount",
                ProviderOrderId = $"DISCOUNT-{counsellorId}-{1000 + subscription.SubscriptionId}",
                DiscountId = discountId
            });

            await tx.CommitAsync();
            return existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
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
        var plan = await _planRepository.GetByIdWithFeaturesAsync(planId)
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
            var normalizedCode = discountCode.Trim().ToUpperInvariant();
            var discount = await _discountRepository.GetValidDiscountForPlanAsync(planId, normalizedCode);
            if (discount == null)
            {
                throw new InvalidOperationException("This discount code is no longer valid for this plan. Please review checkout again.");
            }

            discountId = discount.DiscountId;
            var discountAmount = DiscountCalculator.CalculateDiscountAmount(plan.Price, discount);
            finalAmount = decimal.Round(
                Math.Max(0m, plan.Price - discountAmount),
                2,
                MidpointRounding.AwayFromZero);
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

        var customId = $"{planId}|{discountId.GetValueOrDefault(0)}|{finalAmount.ToString("0.00", CultureInfo.InvariantCulture)}";
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
    /// <param name="payerName">The name used as the payer name on the transaction.</param>
    /// <returns>
    /// A <see cref="SubscriptionResult"/> indicating whether the subscription was newly created,
    /// represents a plan change, or was already active (including duplicate capture detection).
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown if the plan embedded in the PayPal custom ID does not exist.</exception>
    /// <exception cref="Exception">Thrown if the PayPal custom ID cannot be parsed as a valid plan identifier.</exception>
    public async Task<SubscriptionResult> CompletePayPalSubscription(string token, int counsellorId, string payerName)
    {
        var (captureId, customId, capturedAmount) = await _payPalService.CaptureOrder(token);

        var parts = customId.Split('|', StringSplitOptions.TrimEntries);

        if (parts.Length < 2 || !int.TryParse(parts[0], out var planId))
        {
            throw new Exception("PayPal response contained an invalid plan identifier.");
        }

        int? discountId = null;
        if (int.TryParse(parts[1], out var parsedDiscountId) && parsedDiscountId > 0)
        {
            discountId = parsedDiscountId;
        }

        decimal expectedAmount;

        if (parts.Length >= 3)
        {
            if (!decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out expectedAmount))
            {
                throw new Exception("PayPal response contained an invalid expected amount.");
            }
        }
        else
        {
            expectedAmount = decimal.Round(capturedAmount, 2, MidpointRounding.AwayFromZero);
        }

        var plan = await _planRepository.GetByIdWithFeaturesAsync(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        var existing = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellorId);

        if (existing != null && existing.PlanId == planId)
            return SubscriptionResult.AlreadySubscribed;

        if (await _transactionRepository.ExistsByProviderOrderId(captureId))
            return SubscriptionResult.AlreadySubscribed;

        const decimal amountTolerance = 0.01m;

        if (Math.Abs(expectedAmount - capturedAmount) > amountTolerance)
        {
            throw new InvalidOperationException(
                $"Captured amount {capturedAmount:F2} does not match expected amount {expectedAmount:F2}.");
        }

        int? persistedDiscountId = discountId;

        if (discountId.HasValue)
        {
            var discountStillExists = await _context.Discounts.FindAsync(discountId.Value) is not null;

            if (!discountStillExists)
            {
                persistedDiscountId = null;
            }
        }

        await using var tx = await _context.Database.BeginTransactionAsync();

        try
        {
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
                PayerName = payerName,
                Amount = capturedAmount,
                Currency = "CAD",
                Provider = "PayPal",
                ProviderOrderId = captureId,
                DiscountId = persistedDiscountId
            });

            await tx.CommitAsync();
            return existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
