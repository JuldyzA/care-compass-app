using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
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
        /// Retrieves a single plan by its primary key, including its associated features.
        /// </summary>
        /// <param name="id">The primary key of the plan to retrieve.</param>
        /// <returns>The matching <see cref="Plan"/> with features, or <c>null</c> if not found.</returns>
        public async Task<Plan?> GetPlanById(int id)
        {
            return await _context.Plans.Include(p => p.PlanFeatures.OrderBy(f => f.SortOrder))
                .FirstOrDefaultAsync(p => p.PlanId == id);
        }
    }
}

