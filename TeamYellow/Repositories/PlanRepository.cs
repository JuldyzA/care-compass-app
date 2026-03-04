using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public class PlanRepository : IRepository<Plan>
    {
        private readonly ApplicationDbContext Context;

        public PlanRepository(ApplicationDbContext context)
        {
            Context = context;
        }
        public IEnumerable<Plan> GetAll()
        {
            return Context.Plans.ToList();
        }

        public Plan? GetById(int id)
        {
            return Context.Plans.Find(id);
        }


        string IRepository<Plan>.Add(Plan entity)
        {
            Context.Plans.Add(entity);
            Context.SaveChanges();
            return entity.PlanId.ToString();
        }

        bool IRepository<Plan>.Any(int id)
        {
            return Context.Plans.Any(p => p.PlanId == id);
        }

        string IRepository<Plan>.Delete(int id)
        {
            var plan = Context.Plans.Find(id);
            if (plan == null)
            {
                return string.Empty;
            }
            Context.Plans.Remove(plan);
            Context.SaveChanges();
            return plan.PlanId.ToString();
        }


        string IRepository<Plan>.Update(Plan entity)
        {
            var existingPlan = Context.Plans.Find(entity.PlanId);
            if (existingPlan == null)
            {
                return string.Empty;
            }

            existingPlan.PlanName = entity.PlanName;
            existingPlan.PlanDescription = entity.PlanDescription;
            existingPlan.Price = entity.Price;
            existingPlan.BillingType = entity.BillingType;
            existingPlan.IsActive = entity.IsActive;
            existingPlan.CreatedAt = entity.CreatedAt;

            Context.SaveChanges();
            return existingPlan.PlanId.ToString();
        }
    }
}
