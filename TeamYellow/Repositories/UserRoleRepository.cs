using Microsoft.AspNetCore.Identity;
using TeamYellow.ViewModels;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository responsible for assigning and removing ASP.NET Core Identity roles for a user
    /// </summary>
    public class UserRoleRepository
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UserRoleRepository> _logger;

        public UserRoleRepository(UserManager<IdentityUser> userManager, ILogger<UserRoleRepository> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Adds the specified role to the user
        /// </summary>
        public async Task<bool> AddUserRoleAsync(string email, string roleName)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User with email '{Email}' was not found.", email);
                    return false;
                }

                var result = await _userManager.AddToRoleAsync(user, roleName);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to add role '{RoleName}' to user '{Email}'.", roleName, email);
                    return false;
                }
                
                _logger.LogInformation("Role '{RoleName}' added successfully to user '{Email}'.", roleName, email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding role '{RoleName}' to user '{Email}'.", roleName, email);
                return false;
            }
        }

        /// <summary>
        /// Removes the specified role from the user
        /// </summary>
        public async Task<bool> RemoveUserRoleAsync(string email, string roleName)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User with email '{Email}' was not found.", email);
                    return false;
                }

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to remove role '{RoleName}' from user '{Email}'.", roleName, email);
                    return false;
                }

                _logger.LogInformation("Role '{RoleName}' removed successfully from user '{Email}'.", roleName, email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while removing role '{RoleName}' from user '{Email}'.", roleName, email);
                return false;
            }
        }

        /// <summary>
        /// Gets all roles assigned to the user
        /// </summary>
        public async Task<IEnumerable<UserRoleVM>> GetUserRolesAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("User with email '{Email}' was not found.", email);
                return Enumerable.Empty<UserRoleVM>();
            }

            var roles = await _userManager.GetRolesAsync(user);
            return roles.Select(r => new UserRoleVM { Email = email, RoleName = r });
        }
    }
}