using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;
using TeamYellow.ViewModels;

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

        public async Task<bool> UpdatePlansWithFeaturesAsync(PlanVM vm)
        {
            var plan = await _context.Plans
                .Include(p => p.PlanFeatures)
                .FirstOrDefaultAsync(p => p.PlanId == vm.PlanId);

            if (plan == null)
                return false;

            plan.PlanName = vm.PlanName;
            plan.PlanDescription = vm.PlanDescription;
            plan.Price = vm.Price;
            plan.IsActive = vm.IsActive;

            if (vm.PlanFeatures != null)
            {
                var existingFeatures = plan.PlanFeatures
                    .OrderBy(f => f.SortOrder)
                    .ToList();

                for (int i = 0; i < existingFeatures.Count && i < vm.PlanFeatures.Count; i++)
                {
                    existingFeatures[i].FeatureName = vm.PlanFeatures[i].FeatureName?.Trim() ?? string.Empty;
                    existingFeatures[i].FeatureDescription = vm.PlanFeatures[i].FeatureDescription?.Trim() ?? string.Empty;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}