using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public class PlanRepository 
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
        public async Task<bool> Update(Plan entity)
        {
            try
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
            catch (Exception)
            {
                // Optionally log the exception here
                return false;
            }
        }
    }
}
