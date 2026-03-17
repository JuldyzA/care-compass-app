using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository for accessing UserProfile records
    /// </summary>
    public class UserProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public UserProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a user profile associated with the specified ASP.NET Identity user ID.
        /// </summary>
        public async Task<UserProfile?> GetByUserIdAsync(string userId)
        {
            return await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == userId);
        }
    }
}
