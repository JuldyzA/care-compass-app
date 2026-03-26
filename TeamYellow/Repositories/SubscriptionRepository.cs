using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository providing data access operations for <see cref="Subscription"/> entities.
    /// </summary>
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubscriptionRepository> _logger;

        public SubscriptionRepository(ApplicationDbContext context, ILogger<SubscriptionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates and persists a new subscription based on the provided data transfer object.
        /// </summary>
        /// <param name="addSubscriptionDto">The DTO containing subscription creation data.</param>
        /// <returns>The newly created subscription.</returns>
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

            try
            {
                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Subscription {SubscriptionId} created successfully for counsellor {CounsellorId} and plan {PlanId}.",
                    subscription.SubscriptionId, subscription.CounsellorId, subscription.PlanId);
                return subscription;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while creating subscription for counsellor {CounsellorId} and plan {PlanId}.",
                    subscription.CounsellorId,
                    subscription.PlanId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while creating subscription for counsellor {CounsellorId} and plan {PlanId}.",
                    subscription.CounsellorId,
                    subscription.PlanId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the active subscription for the specified counsellor, if one exists.
        /// </summary>
        /// <param name="counsellorId">The counsellor identifier.</param>
        /// <returns>The active subscription, or <c>null</c> if no active subscription exists.</returns>
        public async Task<Subscription?> GetActiveSubscriptionByCounsellorId(int counsellorId)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.CounsellorId == counsellorId && s.Status == SubscriptionStatus.Active);
        }

        /// <summary>
        /// Retrieves the active subscription for the specified counsellor, including related plan and payment details.
        /// </summary>
        /// <param name="counsellorId">The counsellor identifier.</param>
        /// <returns>The active subscription with related data, or <c>null</c> if none exists.</returns>
        public async Task<Subscription?> GetActiveSubscriptionWithPlanByCounsellorId(int counsellorId)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.PaymentTransaction)
                .Include(s => s.Counsellor)
                .FirstOrDefaultAsync(s => s.CounsellorId == counsellorId && s.Status == SubscriptionStatus.Active);
        }

        /// <summary>
        /// Persists changes to an existing subscription.
        /// </summary>
        /// <param name="subscription">The subscription entity to update.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        public async Task UpdateSubscription(Subscription subscription)
        {
            try
            {
                subscription.UpdatedAt = DateTime.UtcNow;
                _context.Subscriptions.Update(subscription);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Subscription {SubscriptionId} updated successfully with status {Status}.", subscription.SubscriptionId, subscription.Status);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error while updating subscription {SubscriptionId}.",
                    subscription.SubscriptionId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while updating subscription {SubscriptionId}.",
                    subscription.SubscriptionId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves all active subscriptions where the billing cycle has passed.
        /// </summary>
        /// <returns>A list of active subscriptions with an expired cycle end date.</returns>
        public async Task<List<Subscription>> GetActiveExpiredSubscriptionsAsync()
        {
            return await _context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.CycleEnd < DateTime.UtcNow)
                .ToListAsync();
        }

    }


}
