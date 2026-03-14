namespace TeamYellow.Services;

public enum SubscriptionResult
{
    Created,
    AlreadySubscribed,
    PlanChanged
}

public interface ISubscriptionService
{
    Task<SubscriptionResult> SubscribeFree(int counsellorId, string userName, int planId);
    Task<string> CreatePayPalOrder(int planId, string returnUrl, string cancelUrl);
    Task<SubscriptionResult> CompletePayPalSubscription(string token, int counsellorId, string userName);
}
