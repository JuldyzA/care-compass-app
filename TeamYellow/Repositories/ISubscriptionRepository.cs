using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Defines data access operations for <see cref="Subscription"/> entities.
    /// </summary>
    public interface ISubscriptionRepository
    {
        Task<Subscription> CreateSubscription(AddSubscriptionDto addSubscriptionDto);

        Task<Subscription?> GetActiveSubscriptionByCounsellorId(int counsellorId);

        Task<Subscription?> GetActiveSubscriptionWithPlanByCounsellorId(int counsellorId);

        Task UpdateSubscription(Subscription subscription);

        Task<List<Subscription>> GetActiveExpiredSubscriptionsAsync();

        Task BulkUpdateSubscriptionsAsync(List<Subscription> subscriptions);

        Task<bool> HasUsedFreeTrialAsync(int counsellorId);
    }
}
