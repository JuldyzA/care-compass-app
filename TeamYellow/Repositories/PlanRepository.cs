using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    public class PlanRepository : IPlanRepository
    {

        private readonly ApplicationDbContext _context;

        public PlanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Plan>> GetActivePlans()
        {
            return await _context.Plans.Include(p => p.PlanFeatures.OrderBy(f => f.sortOrder))
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();
        }
    }
}

