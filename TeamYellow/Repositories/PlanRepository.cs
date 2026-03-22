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
        /// Retrieves a plan by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the plan.</param>
        /// <returns>The plan if found; otherwise, null.</returns>
        public async Task<Plan?> GetById(int id)
        {
            return await _context.Plans.FindAsync(id);
        }

        /// <summary>
        /// Updates an existing plan in the database asynchronously.
        /// </summary>
        /// <param name="entity">The plan entity with updated values.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        public async Task<bool> UpdateAsync(Plan entity)
        {
            var existingPlan = await _context.Plans.FindAsync(entity.PlanId);
                if (existingPlan == null)
                {
                    return false;
                }

                existingPlan.PlanName = entity.PlanName;
                existingPlan.PlanDescription = entity.PlanDescription;
                existingPlan.Price = entity.Price;
                existingPlan.BillingType = entity.BillingType;
                existingPlan.IsActive = entity.IsActive;

                await _context.SaveChangesAsync();

                return true;
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

        /// <summary>
        /// Retrieves a single plan by its primary key, including its associated features.
        /// </summary>
        /// <param name="id">The primary key of the plan to retrieve.</param>
        /// <returns>The matching <see cref="Plan"/> with features, or <c>null</c> if not found.</returns>
        public async Task<Plan?> GetPlanById(int id)
        {
            return await _context.Plans.Include(p => p.PlanFeatures.OrderBy(f => f.SortOrder))
                .FirstOrDefaultAsync(p => p.PlanId == id);
        }

        public async Task<Plan?> GetByIdWithFeaturesAsync(int id)
        {
            return await _context.Plans
                .Include(p => p.PlanFeatures.OrderBy(f => f.SortOrder))
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