using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public class DiscountRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DiscountRepository> _logger;

        public DiscountRepository(ApplicationDbContext context, ILogger<DiscountRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> DiscountCodeExistsAsync(string discountCode, int? excludeDiscountId = null)
        {
            var normalizedCode = discountCode.Trim().ToUpperInvariant();

            return await _context.Discounts.AnyAsync(d =>
                d.DiscountCode == normalizedCode &&
                (!excludeDiscountId.HasValue || d.DiscountId != excludeDiscountId.Value));
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
                _logger.LogInformation("Discount {DiscountId} updated successfully.", entity.DiscountId);
                return entity.DiscountId.ToString();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating discount {DiscountId}.", entity.DiscountId);
                throw new ApplicationException("An error occurred while updating the Discount in the database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating discount {DiscountId}.", entity.DiscountId);
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

            if (discount.PlanDiscounts.Any())
            {
                _logger.LogWarning("Delete skipped for discount {DiscountId} because it is linked to one or more plans.", discountId);
                return false;
            }

            try
            {
                _context.Discounts.Remove(discount);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Discount {DiscountId} deleted successfully.", discountId);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting unused discount {DiscountId}.", discountId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting unused discount {DiscountId}.", discountId);
                throw;
            }
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

        public async Task CreateDiscountWithPlansAsync(Discount discount, IEnumerable<int> planIds)
        {
            IDbContextTransaction? transaction = null;

            try
            {
                transaction = await _context.Database.BeginTransactionAsync();

                await _context.Discounts.AddAsync(discount);
                await _context.SaveChangesAsync();

                var distinctPlanIds = (planIds ?? Enumerable.Empty<int>())
                    .Distinct()
                    .ToList();

                if (distinctPlanIds.Count > 0)
                {
                    var newPlanDiscounts = distinctPlanIds
                        .Select(planId => new PlanDiscount
                        {
                            PlanId = planId,
                            DiscountId = discount.DiscountId
                        })
                        .ToList();

                    await _context.PlanDiscounts.AddRangeAsync(newPlanDiscounts);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Discount {DiscountCode} created successfully with {PlanCount} linked plan(s).", discount.DiscountCode, distinctPlanIds.Count);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating discount {DiscountCode} with plans.", discount.DiscountCode);

                if (transaction != null)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Rollback failed while creating discount {DiscountCode}.", discount.DiscountCode);
                    }
                }


                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating discount {DiscountCode} with plans.", discount.DiscountCode);

                if (transaction != null)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Rollback failed while creating discount {DiscountCode}.", discount.DiscountCode);
                    }
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }
    }
}
