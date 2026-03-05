using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<Subscription> CreateSubscription(AddSubscriptionDto addSubscriptionDto);
    }
}
