
using Microsoft.EntityFrameworkCore;
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
            var cycleEnd = addSubscriptionDto.BillingType == "Yearly"
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

        public async Task<Subscription?> GetActiveSubscriptionByCounsellorId(int counsellorId)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.CounsellorId == counsellorId && s.Status == SubscriptionStatus.Active);
        }

        public async Task UpdateSubscription(Subscription subscription)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }
    }
}
