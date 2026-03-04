using Microsoft.AspNetCore.Mvc.Rendering;
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
        public IEnumerable<UserVM> GetAllUsers()
        {
            IEnumerable<UserVM> users = _context.Users
            .Select(u => new UserVM
            {
                Email = u.Email ?? "(no email)",
            }).ToList();

            return users;
        }

        /// <summary>
        /// Creates a SelectList of users for Razor dropdowns
        /// The provided email (if any) will be selected by default
        /// </summary>
        public SelectList GetUserSelectList(string? email)
        {
            IEnumerable<SelectListItem> users = GetAllUsers()
                       .Select(u => new SelectListItem
                       {
                           Value = u.Email,
                           Text = u.Email
                       });

            SelectList userSelectList = new SelectList(users, "Value", "Text", email);

            return userSelectList;
        }
    }
}