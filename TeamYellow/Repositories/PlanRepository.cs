using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository providing data access operations for <see cref="Plan"/> entities.
    /// </summary>
    public class PlanRepository : IPlanRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlanRepository> _logger;

        public PlanRepository(ApplicationDbContext context, ILogger<PlanRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all plans from the database.
        /// </summary>
        /// <returns>A collection of all plans.</returns>
        public async Task<IEnumerable<Plan>> GetAllAsync()
        {
            return await _context.Plans.ToListAsync();
        }

        /// <summary>
        /// Retrieves all active plans, including their features, ordered for display.
        /// </summary>
        /// <returns>A list of active plans.</returns>
        public async Task<List<Plan>> GetActivePlans()
        {
            var plans = await _context.Plans.Include(p => p.PlanFeatures.OrderBy(f => f.SortOrder))
                .Where(p => p.IsActive)
                .ToListAsync();

            return [.. plans.OrderBy(p => p.Price)];
        }

        /// <summary>
        /// Retrieves a specific plan with its related features.
        /// </summary>
        /// <param name="id">The plan identifier.</param>
        /// <returns>The matching plan with features, or <c>null</c> if not found.</returns>
        public async Task<Plan?> GetByIdWithFeaturesAsync(int id)
        {
            return await _context.Plans
                .Include(p => p.PlanFeatures
                    .OrderBy(f => f.SortOrder))
                .FirstOrDefaultAsync(p => p.PlanId == id);
        }

        /// <summary>
        /// Updates a plan and its related features after validating the submitted feature set.
        /// </summary>
        /// <param name="updatedPlan">The updated plan entity.</param>
        /// <returns><c>true</c> if the update succeeds; otherwise <c>false</c>.</returns>
        public async Task<bool> UpdatePlansWithFeaturesAsync(Plan updatedPlan)
        {
            var plan = await _context.Plans
                .Include(p => p.PlanFeatures)
                .FirstOrDefaultAsync(p => p.PlanId == updatedPlan.PlanId);

            if (plan == null)
            {
                _logger.LogWarning("Plan update skipped because plan {PlanId} was not found.", updatedPlan.PlanId);
                return false;
            }

            var existingFeatures = plan.PlanFeatures.ToList();
            var incomingFeatures = (updatedPlan.PlanFeatures ?? []).ToList();

            if (existingFeatures.Count != incomingFeatures.Count)
            {
                _logger.LogWarning("Plan update skipped for plan {PlanId} because feature count mismatch was detected. " +
                    "Existing: {ExistingCount}, Incoming: {IncomingCount}.",
                    updatedPlan.PlanId, existingFeatures.Count, incomingFeatures.Count);
                return false;
            }

            var existingIds = existingFeatures.Select(f => f.PlanFeatureId)
                                              .OrderBy(id => id)
                                              .ToList();

            var incomingIds = incomingFeatures.Select(f => f.PlanFeatureId)
                                              .OrderBy(id => id)
                                              .ToList();

            if (!existingIds.SequenceEqual(incomingIds))
            {
                _logger.LogWarning("Plan update skipped for plan {PlanId} because feature ID mismatch was detected.", updatedPlan.PlanId);
                return false;
            }

            var incomingById = incomingFeatures.ToDictionary(f => f.PlanFeatureId);

            foreach (var existingFeature in existingFeatures)
            {
                if (!incomingById.TryGetValue(existingFeature.PlanFeatureId, out var incomingFeature))
                {
                    _logger.LogWarning("Plan update skipped for plan {PlanId} because feature {PlanFeatureId} was missing from the incoming payload.",
                        updatedPlan.PlanId, existingFeature.PlanFeatureId);
                    return false;
                }

                existingFeature.FeatureName = (incomingFeature.FeatureName ?? string.Empty).Trim();
                existingFeature.FeatureDescription = (incomingFeature.FeatureDescription ?? string.Empty).Trim();
            }

            plan.PlanName = updatedPlan.PlanName;
            plan.PlanDescription = updatedPlan.PlanDescription;
            plan.Price = updatedPlan.Price;
            plan.IsActive = updatedPlan.IsActive;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Plan {PlanId} updated successfully.", updatedPlan.PlanId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating plan {PlanId}.", updatedPlan.PlanId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating plan {PlanId}.", updatedPlan.PlanId);
                throw;
            }
        }
    }
}