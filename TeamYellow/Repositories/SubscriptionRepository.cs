
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public class SubscriptionRepository(ApplicationDbContext context) : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Subscription> CreateSubscription(AddSubscriptionDto addSubscriptionDto)
        {
            var cycleStart = DateTime.UtcNow;
            var cycleEnd = addSubscriptionDto.BillingType == "Annual"
                ? cycleStart.AddYears(1)
                : cycleStart.AddMonths(1);

            var subscription = new Subscription
            {
                CounsellorId = addSubscriptionDto.CounsellorId,
                PlanId = addSubscriptionDto.PlanId,
                Status = SubscriptionStatus.Active,
                CycleStart = cycleStart,
                CycleEnd = cycleEnd,
                UpdatedAt = cycleStart
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }
    }
}
