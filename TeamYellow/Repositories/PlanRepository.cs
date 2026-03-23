using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
	/// <summary>
    /// Repository providing data access operations for Plans entities.
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
        /// Retrieves all plans from the database asynchronously.
        /// </summary>
        public async Task<IEnumerable<Plan>> GetAllAsync()
        {
            return await _context.Plans.ToListAsync();
        }

		/// <summary>
        /// Retrieves all plans that are currently active, ordered by price ascending.
        /// Each plan includes its features ordered by <see cref="PlanFeature.SortOrder"/>.
        /// </summary>
        /// <returns>A list of active <see cref="Plan"/> entities ordered by price.</returns>
        public async Task<List<Plan>> GetActivePlans()
        {
            var plans = await _context.Plans.Include(p => p.PlanFeatures.OrderBy(f => f.SortOrder))
                .Where(p => p.IsActive)
                .ToListAsync();

            return [.. plans.OrderBy(p => p.Price)];
        }

        public async Task<Plan?> GetByIdWithFeaturesAsync(int id)
        {
            return await _context.Plans
                .Include(p => p.PlanFeatures
                    .OrderBy(f => f.SortOrder))
                .FirstOrDefaultAsync(p => p.PlanId == id);
        }

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