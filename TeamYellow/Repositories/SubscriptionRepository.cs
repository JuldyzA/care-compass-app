
using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository providing data access operations for <see cref="Subscription"/> entities.
    /// </summary>
    public class SubscriptionRepository(ApplicationDbContext context) : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Creates and persists a new subscription based on the provided DTO.
        /// The billing cycle start and end dates are computed automatically from the current UTC time,
        /// using a one-year cycle for <c>Yearly</c> billing and a one-month cycle for all other types.
        /// </summary>
        /// <param name="addSubscriptionDto">The DTO containing subscription creation data.</param>
        /// <returns>The newly created and persisted <see cref="Subscription"/> entity.</returns>
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

        /// <summary>
        /// Retrieves the active subscription for the specified counsellor, if one exists.
        /// </summary>
        /// <param name="counsellorId">The primary key of the counsellor to look up.</param>
        /// <returns>
        /// The counsellor's active <see cref="Subscription"/>, or <c>null</c> if no active subscription exists.
        /// </returns>
        public async Task<Subscription?> GetActiveSubscriptionByCounsellorId(int counsellorId)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.CounsellorId == counsellorId && s.Status == SubscriptionStatus.Active);
        }

        /// <summary>
        /// Retrieves the active subscription for the specified counsellor,
        /// including the associated <see cref="Plan"/> navigation property so that
        /// <see cref="Plan.PlanName"/> and other plan details are available without
        /// an additional query.
        /// </summary>
        /// <param name="counsellorId">The primary key of the counsellor to look up.</param>
        /// <returns>
        /// The counsellor's active <see cref="Subscription"/> with its <see cref="Plan"/> loaded,
        /// or <c>null</c> if no active subscription exists.
        /// </returns>
        public async Task<Subscription?> GetActiveSubscriptionWithPlanByCounsellorId(int counsellorId)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.PaymentTransaction)
                .FirstOrDefaultAsync(s => s.CounsellorId == counsellorId && s.Status == SubscriptionStatus.Active);
        }

        /// <summary>
        /// Persists changes to an existing <see cref="Subscription"/> entity.
        /// Typically called after modifying the subscription's status (for example, cancellation).
        /// </summary>
        /// <param name="subscription">The subscription entity with updated values to persist.</param>
        public async Task UpdateSubscription(Subscription subscription)
        {
            subscription.UpdatedAt = DateTime.UtcNow;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }
    }
}
