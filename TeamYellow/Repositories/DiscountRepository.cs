using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public class DiscountRepository
    {
        private readonly ApplicationDbContext _context;

        public DiscountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all discounts from the database.
        /// </summary>
        /// <returns>An IEnumerable of all Discount entities.</returns>
        public IEnumerable<Discount> GetAll()
        {
            return _context.Discounts.ToList();
        }

        /// <summary>
        /// Adds a new discount to the database and saves changes.
        /// </summary>
        /// <param name="discount">The Discount entity to add.</param>
        public void Add(Discount discount)
        {
            _context.Discounts.Add(discount);
            _context.SaveChanges();
        }

        /// <summary>
        /// Associates a discount with a plan if not already associated.
        /// </summary>
        /// <param name="planId">The ID of the plan.</param>
        /// <param name="discountId">The ID of the discount.</param>
        public void AddDiscountToPlan(int planId, int discountId)
        {
            var plan = _context.Plans.Find(planId);
            var discount = _context.Discounts.Find(discountId);
            if (plan == null || discount == null)
                return;
            var exists = _context.PlanDiscounts
                    .Any(pd => pd.Plan.PlanId == planId && pd.Discount.DiscountId == discountId);

            if (exists)
                return;
            var planDiscount = new PlanDiscount
            {
                Plan = plan,
                Discount = discount
            };
            _context.PlanDiscounts.Add(planDiscount);
            _context.SaveChanges();
        }

        /// <summary>
        /// Retrieves all discounts including their associated plans.
        /// </summary>
        /// <returns>An IEnumerable of Discount entities with related PlanDiscounts and Plans.</returns>
        public IEnumerable<Discount> GetAllDiscountsWithPlans()
        {
            return _context.Discounts
                .Include(d => d.PlanDiscounts)
                .ThenInclude(pd => pd.Plan)
                .ToList();
        }
    }
}
