using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.DTOs;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public class PlanRepository(ApplicationDbContext context) : IPlanRepository
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<List<Plan>> GetActivePlans()
        {
            var plans = await _context.Plans.Include(p => p.PlanFeatures.OrderBy(f => f.sortOrder))
                .Where(p => p.IsActive)
                .ToListAsync();

            return [.. plans.OrderBy(p => p.Price)];
        }

        public async Task<List<Plan>> GetAllPlans()
        {
            var plans = await _context.Plans.Include(p => p.PlanFeatures.OrderBy(f => f.sortOrder))
                .ToListAsync();

            return [.. plans.OrderBy(p => p.Price)];
        }

        public async Task<Plan?> GetPlanById(int id)
        {
            return await _context.Plans.Include(p => p.PlanFeatures.OrderBy(f => f.sortOrder))
                .FirstOrDefaultAsync(p => p.PlanId == id);
        }

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
                sortOrder = index + 1
            })];

            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task<bool> DeletePlan(int id)
        {
            var plan = await _context.Plans.FindAsync(id);

            if (plan is null) return false;

            plan.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

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
                    sortOrder = index + 1
                })]
            };

            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }


    }
}

