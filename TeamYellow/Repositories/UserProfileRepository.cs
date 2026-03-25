using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Models;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository for accessing <see cref="UserProfile"/> records.
    /// </summary>
    public class UserProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public UserProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a user profile associated with the specified identity user.
        /// </summary>
        /// <param name="userId">The identity user identifier.</param>
        /// <returns>The matching user profile, or <c>null</c> if not found.</returns>
        public async Task<UserProfile?> GetByUserIdAsync(string userId)
        {
            return await _context.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.UserId == userId);
        }
    }
}
