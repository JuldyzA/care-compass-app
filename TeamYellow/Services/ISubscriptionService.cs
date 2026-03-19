namespace TeamYellow.Services;

public enum SubscriptionResult
{
    Created,
    AlreadySubscribed,
    PlanChanged
}

public class SubscriptionCheckoutResult
{
    public bool RequiresPayPal { get; set; }
    public bool IsActualFreePlan { get; set; }
    public int? DiscountId { get; set; }
    public string? ApprovalUrl { get; set; }
}

public interface ISubscriptionService
{
    Task<SubscriptionResult> SubscribeFree(int counsellorId, string userName, int planId);

    Task<SubscriptionCheckoutResult> CreatePayPalOrder(
        int planId,
        string? discountCode,
        string returnUrl,
        string cancelUrl);

    Task<SubscriptionResult> SubscribeDiscountedZeroAmount(
        int counsellorId,
        string userName,
        int planId,
        int? discountId);

    Task<SubscriptionResult> CompletePayPalSubscription(string token, int counsellorId, string userName);
}