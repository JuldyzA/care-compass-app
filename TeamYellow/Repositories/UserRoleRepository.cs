using Microsoft.AspNetCore.Identity;
using TeamYellow.ViewModels;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository responsible for assigning and removing ASP.NET Core Identity roles for users.
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
        /// Adds the specified role to the user.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="roleName">The role name to assign.</param>
        /// <returns><c>true</c> if the role was added successfully; otherwise <c>false</c>.</returns>
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
        /// Removes the specified role from the user.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="roleName">The role name to remove.</param>
        /// <returns><c>true</c> if the role was removed successfully; otherwise <c>false</c>.</returns>
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
        /// Retrieves all roles assigned to the specified user.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>A collection of user-role view models for the user.</returns>
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

        /// <summary>
        /// Downgrades a counsellor user to Registered_Visitor by removing counsellor roles
        /// and assigning the Registered_Visitor role.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns><c>true</c> if the downgrade completed successfully; otherwise <c>false</c>.</returns>
        public async Task<bool> DowngradeCounsellorToRegisteredVisitorAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Role downgrade failed. No user found for email {Email}.", email);
                    return false;
                }

                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Free_Counselor"))
                {
                    var result = await _userManager.RemoveFromRoleAsync(user, "Free_Counselor");
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("Failed removing role Free_Counselor for user {Email}. Errors: {Errors}",
                            email, string.Join("; ", result.Errors.Select(e => e.Description)));
                        return false;
                    }
                }

                if (roles.Contains("Paid_Counselor"))
                {
                    var result = await _userManager.RemoveFromRoleAsync(user, "Paid_Counselor");
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("Failed removing role Paid_Counselor for user {Email}. Errors: {Errors}",
                            email, string.Join("; ", result.Errors.Select(e => e.Description)));
                        return false;
                    }
                }

                if (!roles.Contains("Registered_Visitor"))
                {
                    var result = await _userManager.AddToRoleAsync(user, "Registered_Visitor");
                    if (!result.Succeeded)
                    {
                        _logger.LogWarning("Failed adding role Registered_Visitor for user {Email}. Errors: {Errors}",
                            email, string.Join("; ", result.Errors.Select(e => e.Description)));
                        return false;
                    }
                }

                _logger.LogInformation("Successfully downgraded user {Email} to Registered_Visitor.", email);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while downgrading user {Email} to Registered_Visitor.", email);
                return false;
            }
        }
    }
}