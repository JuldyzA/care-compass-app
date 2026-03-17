using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;


namespace TeamYellow.Repositories
{
    public class CounsellorRepository
    {
        private readonly ApplicationDbContext _context;

        public CounsellorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all counsellors including their associated user profiles.
        /// </summary>
        public async Task<IEnumerable<Counsellor>> GetAllAsync()
        {
            return await _context.Counsellors
                .Include(c => c.User)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves all counsellors with their user profiles, subscriptions, and related payment transactions.
        /// </summary>
        public async Task<IEnumerable<Counsellor>> GetCounsellorsWithPaymentsAsync()
        {
            return await _context.Counsellors
                .Include(c => c.User)
                .Include(c => c.Subscriptions)
                .ThenInclude(s => s.PaymentTransaction)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a counsellor by ID, including the associated user profile.
        /// </summary>
        //public Counsellor? GetById(int id)
        //{
        //    return Context.Counsellors.Include(c => c.User).FirstOrDefault(c => c.CounsellorId == id);
        //}
        public async Task<Counsellor?> GetByUserIdAsync(string userId)
        {
            return await _context.Counsellors
            .FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}
