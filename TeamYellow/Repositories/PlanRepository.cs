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

        public PlanRepository(ApplicationDbContext context)
        {
            _context = context;
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
                return false;

            plan.PlanName = updatedPlan.PlanName;
            plan.PlanDescription = updatedPlan.PlanDescription;
            plan.Price = updatedPlan.Price;
            plan.IsActive = updatedPlan.IsActive;

            var existingFeatures = plan.PlanFeatures
                    .OrderBy(f => f.SortOrder)
                    .ToList();

            var incomingFeatures = updatedPlan.PlanFeatures.ToList();

            for (int i = 0; i < existingFeatures.Count && i < incomingFeatures.Count; i++)
            {
                existingFeatures[i].FeatureName = incomingFeatures[i].FeatureName?.Trim() ?? string.Empty;
                existingFeatures[i].FeatureDescription = incomingFeatures[i].FeatureDescription?.Trim() ?? string.Empty;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}