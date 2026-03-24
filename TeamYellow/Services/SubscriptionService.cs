using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Helpers;
using TeamYellow.Repositories;
using System.Globalization;

namespace TeamYellow.Services;

/// <summary>
/// Service implementing subscription management business logic, including
/// free plan subscriptions, PayPal order creation, and PayPal payment capture.
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly DiscountRepository _discountRepository;
    private readonly IPayPalService _payPalService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository,
        ITransactionRepository transactionRepository,
        DiscountRepository discountRepository,
        IPayPalService payPalService,
        ApplicationDbContext context,
        ILogger<SubscriptionService> logger)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _transactionRepository = transactionRepository;
        _discountRepository = discountRepository;
        _payPalService = payPalService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Subscribes a counsellor to a free plan.
    /// </summary>
    /// <param name="counsellorId">The counsellor identifier.</param>
    /// <param name="payerName">The payer name recorded for the transaction.</param>
    /// <param name="planId">The free plan identifier.</param>
    /// <returns>
    /// A result indicating whether the subscription was created, changed from an existing plan,
    /// or was already active.
    /// </returns>
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
        {
            _logger.LogWarning("Free subscription skipped because counsellor {CounsellorId} is already subscribed to plan {PlanId}.", counsellorId, planId);
            return SubscriptionResult.AlreadySubscribed;
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
                Amount = 0m,
                Currency = "CAD",
                Provider = "Free",
                ProviderOrderId = $"FREE-{counsellorId}-{1000 + subscription.SubscriptionId}",
            });

            await tx.CommitAsync();

            var result = existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
            _logger.LogInformation("Free subscription completed for counsellor {CounsellorId}, plan {PlanId}, result {Result}.", counsellorId, planId, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating free subscription for counsellor {CounsellorId} and plan {PlanId}.", counsellorId, planId);
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Subscribes a counsellor to a paid plan whose final amount becomes zero after applying a valid discount.
    /// </summary>
    /// <param name="counsellorId">The counsellor identifier.</param>
    /// <param name="payerName">The payer name recorded for the transaction.</param>
    /// <param name="planId">The plan identifier.</param>
    /// <param name="discountId">The discount identifier to apply.</param>
    /// <returns>
    /// A result indicating whether the subscription was created, changed from an existing plan,
    /// or was already active.
    /// </returns>
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
        {
            _logger.LogWarning("Discounted zero-amount subscription skipped because counsellor {CounsellorId} is already subscribed to plan {PlanId}.", counsellorId, planId);
            return SubscriptionResult.AlreadySubscribed;
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
                Amount = 0m,
                Currency = "CAD",
                Provider = "Full Discount",
                ProviderOrderId = $"DISCOUNT-{counsellorId}-{1000 + subscription.SubscriptionId}",
                DiscountId = discountId
            });

            await tx.CommitAsync();

            var result = existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
            _logger.LogInformation("Discounted zero-amount subscription completed for counsellor {CounsellorId}, plan {PlanId}, discount {DiscountId}, result {Result}.", 
                counsellorId, planId, discountId, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating discounted zero-amount subscription for counsellor {CounsellorId}, plan {PlanId}, discount {DiscountId}.", 
                counsellorId, planId, discountId);
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Creates a PayPal checkout order for a plan and optional discount code.
    /// </summary>
    /// <param name="planId">The plan identifier.</param>
    /// <param name="discountCode">An optional discount code.</param>
    /// <param name="returnUrl">The URL PayPal redirects to after approval.</param>
    /// <param name="cancelUrl">The URL PayPal redirects to if the buyer cancels.</param>
    /// <returns>
    /// A checkout result describing whether PayPal approval is required and, if so, the approval URL.
    /// </returns>
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
            _logger.LogInformation("Checkout for plan {PlanId} is an actual free plan and does not require PayPal.", planId);

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
            _logger.LogInformation("Checkout for plan {PlanId} became zero-amount after discount {DiscountId}; PayPal not required.", planId, discountId);

            return new SubscriptionCheckoutResult
            {
                RequiresPayPal = false,
                IsActualFreePlan = false,
                DiscountId = discountId,
                ApprovalUrl = null
            };
        }

        _logger.LogInformation("Creating PayPal order for plan {PlanId}, discount {DiscountId}, final amount {FinalAmount}.", planId, discountId, finalAmount);

        var customId = $"{planId}|{discountId.GetValueOrDefault(0)}|{finalAmount.ToString("0.00", CultureInfo.InvariantCulture)}";
        var approvalUrl = await _payPalService.CreateOrder(finalAmount, "CAD", returnUrl, cancelUrl, customId);

        _logger.LogInformation("PayPal order created successfully for plan {PlanId}.", planId);

        return new SubscriptionCheckoutResult
        {
            RequiresPayPal = true,
            IsActualFreePlan = false,
            DiscountId = discountId,
            ApprovalUrl = approvalUrl
        };
    }

    /// <summary>
    /// Completes a PayPal subscription by capturing the approved payment and creating the subscription record.
    /// </summary>
    /// <param name="token">The PayPal order approval token.</param>
    /// <param name="counsellorId">The counsellor identifier.</param>
    /// <param name="payerName">The payer name recorded for the transaction.</param>
    /// <returns>
    /// A result indicating whether the subscription was created, changed from an existing plan,
    /// or was already active.
    /// </returns>
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
        {
            _logger.LogWarning("PayPal subscription completion skipped because counsellor {CounsellorId} is already subscribed to plan {PlanId}.", counsellorId, planId);
            return SubscriptionResult.AlreadySubscribed;
        }

        if (await _transactionRepository.ExistsByProviderOrderId(captureId))
        {
            _logger.LogWarning("PayPal subscription completion skipped because capture {CaptureId} was already processed.", captureId);
            return SubscriptionResult.AlreadySubscribed;
        }

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
            var result = existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
            _logger.LogInformation("PayPal subscription completed for counsellor {CounsellorId}, plan {PlanId}, capture {CaptureId}, amount {CapturedAmount}, result {Result}.", 
                counsellorId, planId, captureId, capturedAmount, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while completing PayPal subscription for counsellor {CounsellorId}, plan {PlanId}, capture {CaptureId}.", counsellorId, planId, captureId);
            await tx.RollbackAsync();
            throw;
        }
    }
}
