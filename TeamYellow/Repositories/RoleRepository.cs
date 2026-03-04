using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns all IdentityRole records from the database
        /// </summary>
        public IEnumerable<IdentityRole> GetAllRoles()
        {
            var roles = _context.Roles.ToList();
            return roles;
        }

        /// <summary>
        /// Returns all roles projected into a RoleVM
        /// </summary>
        public IEnumerable<RoleVM> GetAllRolesVM()
        {
            var roles = _context.Roles
                .Select(r => new RoleVM
                {
                    RoleName = r.Name
                }).ToList();

            return roles;
        }

        /// <summary>
        /// Finds a role by its name.
        /// </summary>
        public IdentityRole? GetRole(string roleName)
        {
            IdentityRole? role = _context.Roles
                .FirstOrDefault(r => r.Name == roleName);

            return role;
        }

        /// <summary>
        /// Finds a role by name and returns it as a RoleVM.
        /// </summary>
        public RoleVM? GetRoleVM(string roleName)
        {
            IdentityRole? role = GetRole(roleName);

            if (role != null)
            {
                return new RoleVM { RoleName = role.Name };
            }

            return null;
        }

        /// <summary>
        /// Checks if a role has any users assigned to it.
        /// </summary>
        public bool DoesRoleHaveUsers(string roleName)
        {
            IdentityRole? role = GetRole(roleName);
            if (role == null)
            {
                return false;
            }
            return _context.UserRoles.Any(r => r.RoleId == role.Id);
        }

        /// <summary>
        /// Creates a new role if it does not already exist.
        /// </summary>
        public bool CreateRole(string roleName)
        {
            if (GetRole(roleName) != null)
            {
                return false;
            }

            try
            {
                _context.Roles.Add(new IdentityRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                });

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating role: '" +
                                  roleName + "' : " +
                                  ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Deletes a role by name, but only if it exists and has no assigned users.
        /// </summary>
        public bool DeleteRole(string roleName)
        {
            try
            {
                IdentityRole? role = GetRole(roleName);

                if (role == null)
                {
                    Console.WriteLine("Role not found.");
                    return false;
                }

                if (DoesRoleHaveUsers(roleName))
                {
                    Console.WriteLine("Role ' " + roleName +
                                      "' cannot be deleted " +
                                      "because it has " +
                                      "associated users.");
                    return false;
                }

                _context.Roles.Remove(role);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting role '" +
                                   roleName + "' : " +
                                   ex.Message);

            }
            return false;
        }

        /// <summary>
        /// Builds a SelectList for role dropdowns
        /// Useful for forms where the user selects a role
        /// </summary>
        public SelectList GetRoleSelectList()
        {
            var roles = GetAllRoles()
                       .Select(r => new SelectListItem
                       {
                           Value = r.Name,
                           Text = r.Name
                       }).ToList();

            SelectList roleSelectList = new SelectList(roles, "Value", "Text");

            return roleSelectList;
        }
    }
}