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
        /// <returns>A list of all Discount entities.</returns>
        public async Task<List<Discount>> GetAllAsync()
        {
            return await _context.Discounts.ToListAsync();
        }


        public async Task<List<Discount>> GetActiveDiscountsAsync()
        {
            var nowUtc = DateTime.UtcNow;
            return await _context.Discounts
                    .Where(d => d.StartDateTime <= nowUtc && d.EndDateTime >= nowUtc)
                    .ToListAsync();
        }
        /// <summary>
        /// Adds a new discount to the database and saves changes.
        /// </summary>
        /// <param name="discount">The Discount entity to add.</param>
        public async Task AddAsync(Discount discount)
        {
            await _context.Discounts.AddAsync(discount);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Associates a discount with a plan if not already associated.
        /// </summary>
        /// <param name="planId">The ID of the plan.</param>
        /// <param name="discountId">The ID of the discount.</param>
        public async Task AddDiscountToPlanAsync(int planId, int discountId)
        {
            var plan = await _context.Plans.FindAsync(planId);
            var discount = await _context.Discounts.FindAsync(discountId);

            if (plan == null || discount == null)
                return;

            var exists = await _context.PlanDiscounts
                .AnyAsync(pd => pd.PlanId == planId && pd.DiscountId == discountId);

            if (exists)
                return;

            var planDiscount = new PlanDiscount
            {
                Plan = plan,
                Discount = discount
            };

            await _context.PlanDiscounts.AddAsync(planDiscount);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves all discounts including their associated plans.
        /// </summary>
        /// <returns>A list of Discount entities with related PlanDiscounts and Plans.</returns>
        public async Task<List<Discount>> GetAllDiscountsWithPlansAsync()
        {
            return await _context.Discounts
                .Include(d => d.PlanDiscounts)
                .ThenInclude(pd => pd.Plan)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a discount by its ID, including associated plans.
        /// </summary>
        /// <param name="discountId">The ID of the discount.</param>
        /// <returns>The Discount entity if found; otherwise, null.</returns>
        public async Task<Discount?> GetDiscountByIdAsync(int discountId)
        {
            return await _context.Discounts
                .Include(d => d.PlanDiscounts)
                .ThenInclude(pd => pd.Plan)
                .FirstOrDefaultAsync(d => d.DiscountId == discountId);
        }

        /// <summary>
        /// Updates an existing discount entity in the database.
        /// </summary>
        /// <param name="entity">The Discount entity to update.</param>
        /// <returns>The ID of the updated discount as a string.</returns>
        /// <exception cref="ApplicationException">Thrown when an error occurs during update.</exception>
        public async Task<string> UpdateAsync(Discount entity)
        {
            try
            {
                _context.Discounts.Update(entity);
                await _context.SaveChangesAsync();
                return entity.DiscountId.ToString();
            }
            catch (DbUpdateException ex)
            {
                // Log exception or handle as needed
                throw new ApplicationException("An error occurred while updating the Discount in the database.", ex);
            }
            catch (Exception ex)
            {
                // Log exception or handle as needed
                throw new ApplicationException("An unexpected error occurred while updating the discount record.", ex);
            }
        }


        public async Task<bool> DeleteIfUnusedAsync(int discountId)
        {
            var discount = await _context.Discounts
                .Include(d => d.PlanDiscounts)
                .FirstOrDefaultAsync(d => d.DiscountId == discountId);

            if (discount == null)
                return false;

            // do not delete if it has dependencies
            if (discount.PlanDiscounts.Any())
                return false;

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Discount?> GetValidDiscountForPlanAsync(int planId, string discountCode)
        {
            if (string.IsNullOrWhiteSpace(discountCode))
                return null;

            var normalizedCode = discountCode.Trim().ToUpperInvariant();
            var nowUtc = DateTime.UtcNow;

            return await _context.PlanDiscounts
                .Where(pd =>
                    pd.PlanId == planId &&
                    pd.Discount.StartDateTime <= nowUtc &&
                    pd.Discount.EndDateTime >= nowUtc &&
                    pd.Discount.DiscountCode == normalizedCode)
                .Select(pd => pd.Discount)
                .FirstOrDefaultAsync();
        }
    }
}
