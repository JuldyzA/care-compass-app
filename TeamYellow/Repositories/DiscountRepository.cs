using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public class DiscountRepository
    {
        private readonly ApplicationDbContext Context;

        public DiscountRepository(ApplicationDbContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Retrieves all discounts from the database.
        /// </summary>
        /// <returns>An IEnumerable of all Discount entities.</returns>
        public IEnumerable<Discount> GetAll()
        {
            return Context.Discounts.ToList();
        }

        /// <summary>
        /// Adds a new discount to the database and saves changes.
        /// </summary>
        /// <param name="discount">The Discount entity to add.</param>
        public void Add(Discount discount)
        {
            Context.Discounts.Add(discount);
            Context.SaveChanges();
        }

        /// <summary>
        /// Associates a discount with a plan if not already associated.
        /// </summary>
        /// <param name="planId">The ID of the plan.</param>
        /// <param name="discountId">The ID of the discount.</param>
        public void AddDiscountToPlan(int planId, int discountId)
        {
            var plan = Context.Plans.Find(planId);
            var discount = Context.Discounts.Find(discountId);
            if (plan == null || discount == null)
                return;
            var exists = Context.PlanDiscounts
                    .Any(pd => pd.Plan.PlanId == planId && pd.Discount.DiscountId == discountId);

            if (exists)
                return;
            var planDiscount = new PlanDiscount
            {
                Plan = plan,
                Discount = discount
            };
            Context.PlanDiscounts.Add(planDiscount);
            Context.SaveChanges();
        }

        /// <summary>
        /// Retrieves all discounts including their associated plans.
        /// </summary>
        /// <returns>An IEnumerable of Discount entities with related PlanDiscounts and Plans.</returns>
        public IEnumerable<Discount> GetAllDiscountsWithPlans()
        {
            return Context.Discounts
                .Include(d => d.PlanDiscounts)
                .ThenInclude(pd => pd.Plan)
                .ToList();
        }
    }
}
