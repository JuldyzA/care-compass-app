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
        /// Retrieves a user profile associated with the specified identity user,
        /// with a left join to the Counsellor table to include DisplayName for eligible users.
        /// </summary>
        /// <param name="userId">The identity user identifier.</param>
        /// <returns>A tuple containing the user profile and optional counsellor display name, or null if profile not found.</returns>
        public async Task<(UserProfile? Profile, string? CounsellorDisplayName)> GetByUserIdAsync(string userId)
        {
            var result = await _context.UserProfiles
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => new
                {
                    Profile = up,
                    DisplayName = _context.Counsellors
                        .Where(c => c.UserId == up.UserId)
                        .Select(c => c.DisplayName)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            return result != null 
                ? (result.Profile, result.DisplayName) 
                : (null, null);
        }

        /// <summary>
        /// Updates an existing user profile in the database.
        /// If displayName is not null, also updates the corresponding Counsellor record.
        /// Uses a database transaction with automatic rollback on failure.
        /// </summary>
        /// <param name="userProfile">The user profile entity with updated values.</param>
        /// <param name="displayName">The display name for counsellor/registered visitor users. 
        /// Pass a non-null string (including empty string) to update the Counsellor table, or null to skip counsellor update.</param>
        /// <returns><c>true</c> if the update was successful; otherwise <c>false</c>.</returns>
        public async Task<bool> UpdateAsync(UserProfile userProfile, string? displayName = null)
        {
            using (var dbContextTransaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Update user profile
                    userProfile.UpdatedAt = DateTime.UtcNow;
                    _context.UserProfiles.Update(userProfile);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated user profile for user ID: {UserId}", userProfile.UserId);

                    // If displayName is not null, also update the Counsellor record
                    if (displayName != null)
                    {
                        var counsellor = await _context.Counsellors
                            .FirstOrDefaultAsync(c => c.UserId == userProfile.UserId);

                        if (counsellor != null)
                        {
                            counsellor.DisplayName = displayName;
                            _context.Counsellors.Update(counsellor);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Successfully updated DisplayName for counsellor {CounsellorId}", counsellor.CounsellorId);
                        }
                        else
                        {
                            _logger.LogWarning("No counsellor record found for user ID: {UserId} while attempting to update DisplayName", userProfile.UserId);
                            // Continue without error - the profile update was successful
                        }
                    }

                    // All operations successful, commit the transaction
                    await dbContextTransaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully for user profile update of user ID: {UserId}", userProfile.UserId);
                    return true;
                }
                catch (Exception ex)
                {
                    // If any operation fails, roll back all changes
                    await dbContextTransaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating user profile for user ID: {UserId}. Transaction rolled back.", userProfile.UserId);
                    return false;
                }
            }
        }
    }
}
