using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;

namespace TeamYellow.Controllers
{
    public class PlanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlanController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var plans = (await _context.Plans
                .Include(p => p.PlanFeatures.OrderBy(f => f.sortOrder))
                .Where(p => p.IsActive)
                .ToListAsync())
                .OrderBy(p => p.Price)
                .ToList();

            return View(plans);
        }

        [Authorize]
        public async Task<IActionResult> Checkout(int id)
        {
            var plan = await _context.Plans
                .Include(p => p.PlanFeatures.OrderBy(f => f.sortOrder))
                .FirstOrDefaultAsync(p => p.PlanId == id && p.IsActive);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }
    }
}
