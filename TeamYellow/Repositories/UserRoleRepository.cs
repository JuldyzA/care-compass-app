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

        public UserRoleRepository(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Adds the specified role to the user
        /// </summary>
        public async Task<bool> AddUserRoleAsync(string email, string roleName)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }

        /// <summary>
        /// Removes the specified role from the user
        /// </summary>
        public async Task<bool> RemoveUserRoleAsync(string email, string roleName)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }

        /// <summary>
        /// Gets all roles assigned to the user
        /// </summary>
        public async Task<IEnumerable<UserRoleVM>> GetUserRolesAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return Enumerable.Empty<UserRoleVM>();
            }

            var roles = await _userManager.GetRolesAsync(user);
            return roles.Select(r => new UserRoleVM { Email = email, RoleName = r });
        }
    }
}