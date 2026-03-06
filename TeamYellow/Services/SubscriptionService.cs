using TeamYellow.DTOs;
using TeamYellow.Repositories;

namespace TeamYellow.Services;

public class SubscriptionService(
    IPlanRepository planRepository,
    ISubscriptionRepository subscriptionRepository,
    ITransactionRepository transactionRepository,
    IPayPalService payPalService) : ISubscriptionService
{
    private readonly IPlanRepository _planRepository = planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository = subscriptionRepository;
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IPayPalService _payPalService = payPalService;

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

    public async Task<string> CreatePayPalOrder(int planId, string returnUrl, string cancelUrl)
    {
        var plan = await _planRepository.GetPlanById(planId)
            ?? throw new KeyNotFoundException($"Plan {planId} not found.");

        if (plan.Price == 0)
            return string.Empty;

        return await _payPalService.CreateOrder(plan.Price, "CAD", returnUrl, cancelUrl, planId.ToString());
    }

    public async Task<SubscriptionResult> CompletePayPalSubscription(string token, int counsellorId, string userName)
    {
        var (captureId, customId) = await _payPalService.CaptureOrder(token);

        if (!int.TryParse(customId, out var planId))
            throw new Exception("PayPal response contained an invalid plan identifier.");

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
            Amount = plan.Price,
            Currency = "CAD",
            Provider = "PayPal",
            ProviderOrderId = captureId
        });

        return existing != null ? SubscriptionResult.PlanChanged : SubscriptionResult.Created;
    }
}
