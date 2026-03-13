using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
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
        /// Returns all users projected into a lightweight UserVM.
        /// This is typically used for admin screens or dropdowns.
        /// </summary>
        public async Task<IEnumerable<UserVM>> GetAllUsersAsync()
        {
            IEnumerable<UserVM> users = await _context.Users
                                        .AsNoTracking()
                                        .Select(u => new UserVM
                                        {
                                            Email = u.Email ?? "(no email)",
                                        }).ToListAsync();

            return users;
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