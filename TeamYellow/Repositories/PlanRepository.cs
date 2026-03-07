using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public class PlanRepository 
    {
        private readonly ApplicationDbContext Context;

        public PlanRepository(ApplicationDbContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Retrieves all plans from the database.
        /// </summary>
        public IEnumerable<Plan> GetAll()
        {
            return Context.Plans.ToList();
        }

        /// <summary>
        /// Retrieves a plan by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the plan.</param>
        /// <returns>The plan if found; otherwise, null.</returns>
        public Plan? GetById(int id)
        {
            return Context.Plans.Find(id);
        }

        /// <summary>
        /// Updates an existing plan in the database.
        /// </summary>
        /// <param name="entity">The plan entity with updated values.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        public bool Update(Plan entity)
        {
            var existingPlan = Context.Plans.Find(entity.PlanId);

            if (existingPlan == null)
            {
                return false;
            }

            existingPlan.PlanName = entity.PlanName;
            existingPlan.PlanDescription = entity.PlanDescription;
            existingPlan.Price = entity.Price;
            existingPlan.BillingType = entity.BillingType;
            existingPlan.IsActive = entity.IsActive;

            Context.SaveChanges();

            return true;
        }
    }
}
