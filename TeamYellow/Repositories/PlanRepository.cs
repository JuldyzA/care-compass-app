using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository providing data access operations for <see cref="Plan"/> entities.
    /// Plans are ordered by price and include their associated <see cref="PlanFeature"/> records.
    /// </summary>
    public class PlanRepository(ApplicationDbContext context) : IPlanRepository
    {
        private readonly ApplicationDbContext _context = context;

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
        /// Retrieves all plans regardless of active status, ordered by price ascending.
        /// Each plan includes its features ordered by <see cref="PlanFeature.SortOrder"/>.
        /// </summary>
        /// <returns>A list of all <see cref="Plan"/> entities ordered by price.</returns>
        public async Task<List<Plan>> GetAllPlans()
        {
            var plans = await _context.Plans.Include(p => p.PlanFeatures.OrderBy(f => f.SortOrder))
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

        /// <summary>
        /// Updates an existing plan with the data provided in the DTO.
        /// Existing plan features are removed and replaced with the new set from the DTO,
        /// preserving the supplied sort order.
        /// </summary>
        /// <param name="updatePlanDto">The DTO containing updated plan data.</param>
        /// <returns>The updated <see cref="Plan"/> entity.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no plan with the given ID exists.</exception>
        public async Task<Plan> UpdatePlan(UpdatePlanDto updatePlanDto)
        {
            var plan = await _context.Plans
                .Include(p => p.PlanFeatures)
                .FirstOrDefaultAsync(p => p.PlanId == updatePlanDto.PlanId) ?? throw new KeyNotFoundException("Plan not found");

            plan.PlanName = updatePlanDto.PlanName;
            plan.PlanDescription = updatePlanDto.PlanDescription;
            plan.Price = updatePlanDto.Price;
            plan.BillingType = updatePlanDto.BillingType;
            plan.IsActive = updatePlanDto.IsActive;

            _context.PlanFeatures.RemoveRange(plan.PlanFeatures);
            plan.PlanFeatures = [.. updatePlanDto.PlanFeatureDtos.Select((f, index) => new PlanFeature
            {
                FeatureName = f.FeatureName,
                FeatureDescription = f.FeatureDescription,
                SortOrder = index + 1
            })];

            await _context.SaveChangesAsync();
            return plan;
        }

        /// <summary>
        /// Soft-deletes a plan by setting its <see cref="Plan.IsActive"/> flag to <c>false</c>.
        /// The plan record is retained in the database for historical referencing.
        /// </summary>
        /// <param name="id">The ID of the plan to deactivate.</param>
        /// <returns><c>true</c> if the plan was found and deactivated; <c>false</c> if the plan does not exist.</returns>
        public async Task<bool> DeletePlan(int id)
        {
            var plan = await _context.Plans.FindAsync(id);

            if (plan is null) return false;

            plan.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Creates and persists a new plan from the supplied DTO.
        /// Plan features from the DTO are assigned a sequential <see cref="PlanFeature.SortOrder"/>
        /// based on their position in the DTO list.
        /// </summary>
        /// <param name="addPlanDto">The DTO containing the new plan data.</param>
        /// <returns>The newly created and persisted <see cref="Plan"/> entity.</returns>
        public async Task<Plan> AddPlan(AddPlanDto addPlanDto)
        {
            var plan = new Plan
            {
                PlanName = addPlanDto.PlanName,
                PlanDescription = addPlanDto.PlanDescription,
                Price = addPlanDto.Price,
                BillingType = addPlanDto.BillingType,
                IsActive = addPlanDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                PlanFeatures = [.. addPlanDto.PlanFeatureDtos.Select((f, index) => new PlanFeature
                {
                    FeatureName = f.FeatureName,
                    FeatureDescription = f.FeatureDescription,
                    SortOrder = index + 1
                })]
            };

            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }


    }
}

