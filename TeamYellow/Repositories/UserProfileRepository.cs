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
        private readonly ILogger<UserProfileRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfileRepository"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="logger">Logs repository operations.</param>
        public UserProfileRepository(ApplicationDbContext context, ILogger<UserProfileRepository> logger)
        {
            _context = context;
            _logger = logger;
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

        /// <summary>
        /// Updates an existing user profile in the database.
        /// </summary>
        /// <param name="userProfile">The user profile entity with updated values.</param>
        /// <returns><c>true</c> if the update was successful; otherwise <c>false</c>.</returns>
        public async Task<bool> UpdateAsync(UserProfile userProfile)
        {
            try
            {
                userProfile.UpdatedAt = DateTime.UtcNow;
                _context.UserProfiles.Update(userProfile);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated user profile for user ID: {UserId}", userProfile.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user ID: {UserId}", userProfile.UserId);
                return false;
            }
        }
    }
}
