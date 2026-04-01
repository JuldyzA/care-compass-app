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
                .Include(s => s.Counsellor)
                .ThenInclude(c => c.User)
                .Where(s => s.Status == SubscriptionStatus.Active && s.CycleEnd <= DateTime.UtcNow)
                .ToListAsync();
        }

        /// <summary>
        /// Performs a bulk update on multiple subscriptions, setting their UpdatedAt timestamp to the current UTC time.
        /// </summary>
        /// <param name="subscriptions">The list of subscription entities to update.</param>
        /// <returns>A task that represents the asynchronous bulk update operation.</returns>
        public async Task BulkUpdateSubscriptionsAsync(List<Subscription> subscriptions)
        {
            if (subscriptions == null)
            {
                throw new ArgumentNullException(nameof(subscriptions));
            }

            if (subscriptions.Count == 0)
            {
                return;
            }

            var utcNow = DateTime.UtcNow;

            try
            {
                foreach (var sub in subscriptions)
                {
                    sub.UpdatedAt = utcNow;
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Bulk update of {SubscriptionCount} subscriptions completed successfully.", subscriptions.Count);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during bulk update of {SubscriptionCount} subscriptions.", subscriptions.Count);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during bulk update of {SubscriptionCount} subscriptions.", subscriptions.Count);
                throw;
            }
        }

        /// <summary>
        /// Determines whether the specified counsellor has previously used a free-plan subscription.
        /// </summary>
        /// <param name="counsellorId">The counsellor identifier.</param>
        /// <returns>
        /// <c>true</c> if the counsellor has at least one subscription associated with a plan
        /// whose price is 0; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> HasUsedFreeTrialAsync(int counsellorId)
        {
            return await _context.Subscriptions
                .AnyAsync(s =>
                    s.CounsellorId == counsellorId &&
                    s.Plan != null &&
                    s.Plan.Price == 0);
        }

        /// <summary>
        /// Determines whether the specified counsellor has any subscription history
        /// for a paid plan, such as a monthly or yearly plan.
        /// </summary>
        /// <param name="counsellorId">The unique identifier of the counsellor.</param>
        /// <returns>
        /// <c>true</c> if the counsellor has previously subscribed to any paid plan;
        /// otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> HasPaidPlanHistoryAsync(int counsellorId)
        {
            return await _context.Subscriptions
                .AnyAsync(s =>
                    s.CounsellorId == counsellorId &&
                    s.Plan != null &&
                    s.Plan.Price > 0m);
        }

        /// <summary>
        /// Retrieves the current valid active subscriptions for the specified counsellors in a single query.
        /// </summary>
        /// <param name="counsellorIds">The counsellor identifiers to check.</param>
        /// <param name="utcNow">The UTC timestamp used to determine whether a subscription is still valid.</param>
        /// <returns>
        /// A dictionary keyed by counsellor identifier containing the valid active subscription
        /// for each counsellor that currently has one.
        /// </returns>
        public async Task<Dictionary<int, Subscription>> GetValidActiveSubscriptionsByCounsellorIdsAsync(IEnumerable<int> counsellorIds, DateTime utcNow)
        {
            var ids = counsellorIds.Distinct().ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<int, Subscription>();
            }

            return await _context.Subscriptions
                .Where(s =>
                    ids.Contains(s.CounsellorId) &&
                    s.Status == SubscriptionStatus.Active &&
                    s.CycleEnd > utcNow)
                .GroupBy(s => s.CounsellorId)
                .Select(g => g.OrderByDescending(s => s.CycleEnd).First())
                .ToDictionaryAsync(s => s.CounsellorId);
        }
    }
}