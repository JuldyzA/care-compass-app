using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamYellow.Data;

namespace TeamYellow.Services
{
    /// <summary>
    /// Handles the MVP account deletion workflow.
    /// </summary>
    public class UserAccountDeletionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly ILogger<UserAccountDeletionService> _logger;

        public UserAccountDeletionService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IAzureBlobStorageService blobStorageService,
            ILogger<UserAccountDeletionService> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Deletes the currently authenticated user's account using MVP rules.
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteCurrentUserAsync(ClaimsPrincipal principal)
        {
            string? userId = _userManager.GetUserId(principal);

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("DeleteCurrentUserAsync failed because no user ID was found in claims.");
                return (false, "Unable to identify the current user.");
            }

            IdentityUser? user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("DeleteCurrentUserAsync failed because no Identity user was found for user ID {UserId}.", userId);
                return (false, "Unable to find your account.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            bool hasBlockedRole = roles.Contains("Administrator") || roles.Contains("Manager");

            if (hasBlockedRole)
            {
                _logger.LogWarning("Blocked self-delete attempt for privileged user {UserId}. Roles: {Roles}", userId, string.Join(", ", roles));
                return (false, "This account type cannot be deleted through self-service.");
            }

            bool hasAllowedRole = roles.Contains("Registered_Visitor") || roles.Contains("Free_Counselor") || roles.Contains("Paid_Counselor");

            if (!hasAllowedRole)
            {
                _logger.LogWarning("Blocked self-delete attempt for user {UserId} without an allowed role. Roles: {Roles}", userId, string.Join(", ", roles));
                return (false, "This account type cannot be deleted through self-service.");
            }

            string? profilePhotoUrl = await _context.UserProfiles
                .Where(up => up.UserId == userId)
                .Select(up => up.ProfilePhotoUrl)
                .FirstOrDefaultAsync();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var counsellor = await _context.Counsellors
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (counsellor != null)
                {
                    counsellor.IsActive = false;
                    counsellor.UserId = null;
                }

                var userLogs = await _context.UserLogs
                    .Where(ul => ul.UserId == userId)
                    .ToListAsync();

                foreach (var log in userLogs)
                {
                    log.UserId = null;
                }

                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (userProfile != null)
                {
                    _context.UserProfiles.Remove(userProfile);
                }

                await _context.SaveChangesAsync();

                IdentityResult deleteResult = await _userManager.DeleteAsync(user);

                if (!deleteResult.Succeeded)
                {
                    await transaction.RollbackAsync();

                    string errorMessage = string.Join(" ", deleteResult.Errors.Select(e => e.Description));

                    _logger.LogWarning("Identity deletion failed for user {UserId}. Errors: {Errors}", userId, errorMessage);

                    return (false, string.IsNullOrWhiteSpace(errorMessage) ? "Unable to delete your account." : errorMessage);
                }

                await transaction.CommitAsync();

                if (!string.IsNullOrWhiteSpace(profilePhotoUrl))
                {
                    bool deleted = await _blobStorageService.DeleteFileAsync(profilePhotoUrl);

                    if (!deleted)
                    {
                        _logger.LogWarning("Profile photo cleanup was skipped or the file was not found after account deletion for user {UserId}. URL: {BlobUrl}", 
                            userId, profilePhotoUrl);
                    }
                }

                await _signInManager.SignOutAsync();

                _logger.LogInformation("Account deleted successfully for user ID {UserId}.", userId);

                return (true, "Your account has been deleted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error deleting account for user ID {UserId}.", userId);
                return (false, "An error occurred while deleting your account. Please try again.");
            }
        }
    }
}
