namespace TeamYellow.Services;

/// <summary>
/// Represents the possible outcomes of a subscription creation workflow.
/// </summary>
public enum SubscriptionResult
{
    Created,
    AlreadySubscribed,
    PlanChanged
}

/// <summary>
/// Defines business operations for creating checkout flows and managing subscriptions.
/// </summary>
public class SubscriptionCheckoutResult
{
    public bool RequiresPayPal { get; set; }
    public bool IsActualFreePlan { get; set; }
    public int? DiscountId { get; set; }
    public string? ApprovalUrl { get; set; }
}

/// <summary>
/// Defines business operations for creating checkout flows and managing subscriptions.
/// </summary>
public interface ISubscriptionService
{
    Task<SubscriptionResult> SubscribeFree(int counsellorId, string payerName, int planId);

    Task<SubscriptionCheckoutResult> CreatePayPalOrder(
        int planId,
        string? discountCode,
        string returnUrl,
        string cancelUrl);

    Task<SubscriptionResult> SubscribeDiscountedZeroAmount(
        int counsellorId,
        string payerName,
        int planId,
        int? discountId);

    Task<SubscriptionResult> CompletePayPalSubscription(string token, int counsellorId, string payerName);
}