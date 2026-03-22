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

        public async Task<List<Discount>> GetDiscountsForLinkingAsync()
        {
            var nowUtc = DateTime.UtcNow;
            return await _context.Discounts
                    .Where(d => d.StartDateTime <= nowUtc && d.EndDateTime >= nowUtc)
                    .OrderBy(d => d.DiscountCode)
                    .ToListAsync();
        }

        public async Task<bool> DiscountCodeExistsAsync(string discountCode, int? excludeDiscountId = null)
        {
            var normalizedCode = discountCode.Trim().ToUpperInvariant();

            return await _context.Discounts.AnyAsync(d =>
                d.DiscountCode == normalizedCode &&
                (!excludeDiscountId.HasValue || d.DiscountId != excludeDiscountId.Value));
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

        public async Task AddDiscountToPlansAsync(IEnumerable<int> planIds, int discountId)
        {
            var distinctPlanIds = (planIds ?? Enumerable.Empty<int>())
                .Distinct()
                .ToList();

            if (distinctPlanIds.Count == 0)
            {
                return;
            }

            var existingPlanIds = await _context.PlanDiscounts
                .Where(pd => pd.DiscountId == discountId && distinctPlanIds.Contains(pd.PlanId))
                .Select(pd => pd.PlanId)
                .ToListAsync();

            var existingPlanIdSet = existingPlanIds.ToHashSet();

            var newPlanDiscounts = distinctPlanIds
                .Where(planId => !existingPlanIdSet.Contains(planId))
                .Select(planId => new PlanDiscount
                {
                    PlanId = planId,
                    DiscountId = discountId
                })
                .ToList();

            if (newPlanDiscounts.Count == 0)
            {
                return;
            }

            await _context.PlanDiscounts.AddRangeAsync(newPlanDiscounts);
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

        public async Task<Discount?> GetDiscountForPlanByIdAsync(int planId, int discountId)
        {
            return await _context.PlanDiscounts
                .AsNoTracking()
                .Where(pd =>
                    pd.PlanId == planId &&
                    pd.DiscountId == discountId)
                .Select(pd => pd.Discount)
                .FirstOrDefaultAsync();
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
                .AsNoTracking()
                .Where(pd =>
                    pd.PlanId == planId &&
                    pd.Discount.StartDateTime <= nowUtc &&
                    pd.Discount.EndDateTime >= nowUtc &&
                    pd.Discount.DiscountCode == normalizedCode)
                .Select(pd => pd.Discount)
                .FirstOrDefaultAsync();
        }

        public async Task<Discount?> GetValidDiscountForPlanByIdAsync(int planId, int discountId)
        {
            var nowUtc = DateTime.UtcNow;

            return await _context.PlanDiscounts
                .AsNoTracking()
                .Where(pd =>
                    pd.PlanId == planId &&
                    pd.DiscountId == discountId &&
                    pd.Discount.StartDateTime <= nowUtc &&
                    pd.Discount.EndDateTime >= nowUtc)
                .Select(pd => pd.Discount)
                .FirstOrDefaultAsync();
        }
    }
}
