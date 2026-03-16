using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Helpers;
using TeamYellow.ViewModels;

namespace TeamYellow.Repositories
{
    /// <summary>
    /// Repository for reading application users and preparing user dropdown lists
    /// </summary>
    public class UserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns a paginated, sortable, and filterable list of users projected into UserVM.
        /// Used for the admin user role management screen.
        /// </summary>
        public async Task<PaginatedList<UserVM>> GetAllUsersAsync(string? emailFilter = null, string? sortOrder = null, int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<UserVM> query = _context.Users
                                       .AsNoTracking()
                                       .Select(u => new UserVM
                                       {
                                           UserId = u.Id, 
                                           Email = u.Email ?? "(no email)",
                                       });

            if (!string.IsNullOrWhiteSpace(emailFilter))
            {
                string trimmedEmail = emailFilter.Trim();
                query = query.Where(u => u.Email.Contains(trimmedEmail));
            }

            switch (sortOrder)
            {
                case "email_desc":
                    query = query.OrderByDescending(u => u.Email)
                        .ThenByDescending(u => u.UserId);
                    break;
                case "email_asc":
                    query = query.OrderBy(u => u.Email)
                        .ThenBy(u => u.UserId);
                    break;
                default:
                    query = query.OrderBy(u => u.Email)
                        .ThenBy(u => u.UserId);
                    break;
            }

            return await PaginatedList<UserVM>.CreateAsync(query, pageNumber, pageSize);
        }

        /// <summary>
        /// Creates a SelectList of users for Razor dropdowns
        /// The provided email (if any) will be selected by default
        /// </summary>
        public async Task<SelectList> GetUserSelectListAsync(string? email)
        {
            var users = await _context.Users
                       .AsNoTracking()
                       .Select(u => new SelectListItem
                       {
                           Value = u.Email ?? string.Empty,
                           Text = u.Email ?? "(no email)"
                       }).ToListAsync();

            SelectList userSelectList = new SelectList(users, "Value", "Text", email);

            return userSelectList;
        }
    }
}