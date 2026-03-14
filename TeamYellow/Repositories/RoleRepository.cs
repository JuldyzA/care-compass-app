using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.ViewModels;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository responsible for managing ASP.NET Core Identity Roles
    /// Encapsulates data access for roles (AspNetRoles) and related lookups
    /// such as whether a role is assigned to any users (AspNetUserRoles)
    /// </summary>
    public class RoleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoleRepository> _logger;

        public RoleRepository(ApplicationDbContext context, ILogger<RoleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Returns all roles projected into a RoleVM
        /// </summary>
        public async Task<IEnumerable<RoleVM>> GetAllRolesVMAsync()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .Select(r => new RoleVM
                {
                    RoleName = r.Name ?? string.Empty
                }).ToListAsync();

            return roles;
        }

        /// <summary>
        /// Finds a role by its name.
        /// </summary>
        public async Task<IdentityRole?> GetRoleAsync(string roleName)
        {
            IdentityRole? role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == roleName);

            return role;
        }

        /// <summary>
        /// Finds a role by name and returns it as a RoleVM.
        /// </summary>
        public async Task<RoleVM?> GetRoleVMAsync(string roleName)
        {
            IdentityRole? role = await GetRoleAsync(roleName);

            if (role != null)
            {
                return new RoleVM { RoleName = role.Name ?? string.Empty };
            }

            return null;
        }

        /// <summary>
        /// Creates a new role if it does not already exist.
        /// </summary>
        public async Task<bool> CreateRoleAsync(string roleName)
        {
            if (await GetRoleAsync(roleName) != null)
            {
                _logger.LogInformation("Role '{RoleName}' already exists.", roleName);
                return false;
            }

            try
            {
                _context.Roles.Add(new IdentityRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role '{RoleName}'", roleName);
                return false;
            }
        }

        /// <summary>
        /// Deletes a role by name, but only if it exists and has no assigned users.
        /// </summary>
        public async Task<bool> DeleteRoleAsync(string roleName)
        {
            try
            {
                IdentityRole? role = await GetRoleAsync(roleName);

                if (role == null)
                {
                    _logger.LogWarning("Role '{RoleName}' not found.", roleName);
                    return false;
                }

                bool hasUsers = await _context.UserRoles.AnyAsync(r => r.RoleId == role.Id);

                if (hasUsers)
                {
                    _logger.LogWarning("Role '{RoleName}' cannot be deleted because it has associated users.", roleName);
                    return false;
                }

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Role '{RoleName}' deleted successfully.", roleName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role '{RoleName}'", roleName);
                return false;
            }
        }

        /// <summary>
        /// Builds a SelectList for role dropdowns
        /// Useful for forms where the user selects a role
        /// </summary>
        public async Task<SelectList> GetRoleSelectListAsync()
        {
            var roles = await _context.Roles
                       .AsNoTracking()
                       .Select(r => new SelectListItem
                       {
                           Value = r.Name,
                           Text = r.Name
                       }).ToListAsync();

            SelectList roleSelectList = new SelectList(roles, "Value", "Text");

            return roleSelectList;
        }
    }
}