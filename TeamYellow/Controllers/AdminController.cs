using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    /// <summary>
    /// Manages administrator-only workflows for users, roles, and user log records.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly RoleRepository _roleRepository;
        private readonly UserRepository _userRepository;
        private readonly UserRoleRepository _userRoleRepository;
        private readonly UserLogRepository _userLogRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminController"/> class.
        /// </summary>
        /// <param name="logger">Logs controller events and warning conditions.</param>
        /// <param name="roleRepository">Provides access to role lookup and management operations.</param>
        /// <param name="userRepository">Provides access to user lookup and list operations.</param>
        /// <param name="userRoleRepository">Provides access to user-role assignment operations.</param>
        /// <param name="userLogRepo">Provides access to user login and logout audit logs.</param>
        public AdminController(ILogger<AdminController> logger,
                               RoleRepository roleRepository,
                               UserRepository userRepository,
                               UserRoleRepository userRoleRepository,
                               UserLogRepository userLogRepo)
        {
            _logger = logger;
            _roleRepository = roleRepository;
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _userLogRepository = userLogRepo;
        }

        /// <summary>
        /// Displays a paginated, sortable, and filterable list of users in the system.
        /// </summary>
        /// <param name="sortOrder">The current sort order for the user list.</param>
        /// <param name="emailFilter">An optional email filter applied to the user list.</param>
        /// <param name="pageNumber">The requested page number.</param>
        /// <returns>The user role index view with paginated user data.</returns>
        public async Task<IActionResult> UserRoleIndex(string? sortOrder, string? emailFilter, int? pageNumber)
        {
            string currentSortOrder = string.IsNullOrEmpty(sortOrder) ? "email_asc" : sortOrder;
            ViewBag.CurrentSortOrder = currentSortOrder;
            ViewBag.CurrentEmailFilter = emailFilter;

            ViewBag.EmailSortParam = currentSortOrder == "email_asc" ? "email_desc" : "email_asc";

            int pageSize = 5;
            int safePageNumber = Math.Max(1, pageNumber ?? 1);

            var users = await _userRepository.GetAllUsersAsync(emailFilter, currentSortOrder, safePageNumber, pageSize);
            return View(users);
        }

        /// <summary>
        /// Displays the roles currently assigned to a specific user.
        /// </summary>
        /// <param name="userName">The email or user name of the selected user.</param>
        /// <param name="message">An optional status message to display in the view.</param>
        /// <param name="isError">Indicates whether the supplied status message represents an error.</param>
        /// <returns>The user role detail view for the selected user.</returns>
        public async Task<IActionResult> UserRoleDetail(string userName, string message = "", bool isError = false)
        {
            var roles = await _userRoleRepository.GetUserRolesAsync(userName);
            ViewBag.Message = message;
            ViewBag.UserName = userName;
            ViewBag.IsError = isError;
            
            return View(roles);
        }

        /// <summary>
        /// Displays the form used to assign a role to a user.
        /// </summary>
        /// <param name="email">An optional email to preselect in the user dropdown.</param>
        /// <returns>The user role creation view.</returns>
        [HttpGet]
        public async Task<IActionResult> UserRoleCreate(string? email)
        {
            ViewBag.RoleSelectList = await _roleRepository.GetRoleSelectListAsync();
            ViewBag.UserSelectList = await _userRepository.GetUserSelectListAsync(email);

            return View();
        }

        /// <summary>
        /// Assigns the selected role to the specified user.
        /// </summary>
        /// <param name="userRoleVM">The submitted user-role assignment data.</param>
        /// <returns>
        /// A redirect to the user role detail page when successful; otherwise returns the form with validation errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserRoleCreate(UserRoleVM userRoleVM)
        {
            if (ModelState.IsValid)
            {
                var result = await _userRoleRepository.AddUserRoleAsync(userRoleVM.Email, userRoleVM.RoleName);

                if (result)
                {
                    string message = $"{userRoleVM.RoleName} permissions " +
                                     "successfully added to " +
                                     userRoleVM.Email;
                    return RedirectToAction(nameof(UserRoleDetail), new { userName = userRoleVM.Email, message });
                }
                else
                {
                    ModelState.AddModelError("",
                                             "Failed to add role to user." +
                                             " The role might already" +
                                             " exist for this user.");
                }
            }

            ViewBag.RoleSelectList = await _roleRepository.GetRoleSelectListAsync();
            ViewBag.UserSelectList = await _userRepository.GetUserSelectListAsync(userRoleVM.Email);

            return View(userRoleVM);
        }

        /// <summary>
        /// Displays a confirmation page before removing a role from a user.
        /// </summary>
        /// <param name="email">The email of the user whose role is being removed.</param>
        /// <param name="roleName">The role to remove.</param>
        /// <returns>
        /// The confirmation view when the parameters are valid; otherwise redirects to the user list.
        /// </returns>
        [HttpGet]
        public IActionResult UserRoleDelete(string email, string roleName)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(roleName))
            {
                TempData["ErrorMessage"] = "Email and Role Name are required.";
                return RedirectToAction(nameof(UserRoleIndex));
            }

            var userRoleVM = new UserRoleVM
            {
                Email = email,
                RoleName = roleName
            };

            return View(userRoleVM);
        }

        /// <summary>
        /// Removes a role from a user after confirmation.
        /// Prevents an administrator from removing the Administrator role from their own account.
        /// </summary>
        /// <param name="userRoleVM">The submitted user-role removal data.</param>
        /// <returns>
        /// A redirect to the user role detail page when successful; otherwise returns the confirmation view with errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserRoleDelete(UserRoleVM userRoleVM)
        {
            if (ModelState.IsValid)
            {
                string? currentEmail = User.Identity?.Name;

                if (string.Equals(userRoleVM.RoleName, "Administrator", StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(userRoleVM.Email, currentEmail, StringComparison.OrdinalIgnoreCase))
                {
                    string message = "You cannot remove the Administrator role from your own account.";
                    return RedirectToAction(nameof(UserRoleDetail), new { userName = userRoleVM.Email, message, isError = true });
                }

                var result = await _userRoleRepository.RemoveUserRoleAsync(userRoleVM.Email, userRoleVM.RoleName);

                if (result)
                {
                    string message = $"{userRoleVM.RoleName} permissions " +
                                     "successfully removed from " +
                                     userRoleVM.Email;

                    return RedirectToAction(nameof(UserRoleDetail), new { userName = userRoleVM.Email, message });
                }
                else
                {
                    ModelState.AddModelError("",
                                             "Failed to remove role " +
                                             "from user.");
                }
            }
            return View(userRoleVM);
        }

        /// <summary>
        /// Displays all available roles in the system.
        /// </summary>
        /// <param name="message">An optional status message to display in the view.</param>
        /// <returns>The role index view.</returns>
        public async Task<IActionResult> RoleIndex(string message = "")
        {
            IEnumerable<RoleVM> roles = await _roleRepository.GetAllRolesVMAsync();
            ViewBag.Message = message;

            return View(roles);
        }

        /// <summary>
        /// Displays the form for creating a new role.
        /// </summary>
        /// <returns>The role creation view.</returns>
        [HttpGet]
        public IActionResult RoleCreate()
        {
            return View(new RoleVM());
        }

        /// <summary>
        /// Creates a new role if it does not already exist.
        /// </summary>
        /// <param name="roleVM">The submitted role creation data.</param>
        /// <returns>
        /// A redirect to the role index when successful; otherwise returns the form with validation errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleCreate(RoleVM roleVM)
        {
            if (ModelState.IsValid)
            {
                bool isSuccess = await _roleRepository.CreateRoleAsync(roleVM.RoleName);

                if (isSuccess)
                {
                    string message = "Successfully added " +
                                     roleVM.RoleName +
                                     " to Roles.";
                    return RedirectToAction(nameof(RoleIndex), new { message });
                }
                else
                {
                    string message = "Role creation failed. " +
                                     roleVM.RoleName +
                                     " may already exist.";

                    ModelState.AddModelError("", message);
                }
            }
            return View(roleVM);
        }

        /// <summary>
        /// Displays a confirmation page before removing a role.
        /// </summary>
        /// <param name="roleName">The name of the role to remove.</param>
        /// <returns>
        /// The confirmation view when the role exists; otherwise redirects to the role index.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> RoleDelete(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                string message = "Role name cannot be empty.";
                _logger.LogWarning(message);
                return RedirectToAction(nameof(RoleIndex), new { message });
            }

            RoleVM? role = await _roleRepository.GetRoleVMAsync(roleName);

            if (role == null)
            {
                string message = $"Role '{roleName}' not found";
                _logger.LogWarning(message);
                return RedirectToAction(nameof(RoleIndex), new { message });
            }
            return View(role);
        }

        /// <summary>
        /// Deletes a role after confirmation.
        /// A role cannot be deleted if users are currently assigned to it.
        /// </summary>
        /// <param name="roleVM">The submitted role deletion data.</param>
        /// <returns>
        /// A redirect to the role index when successful; otherwise returns the confirmation view with errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RoleDelete(RoleVM roleVM)
        {
            if (ModelState.IsValid)
            {
                bool isSuccess = await _roleRepository.DeleteRoleAsync(roleVM.RoleName);

                if (isSuccess)
                {
                    string message = "Successfully removed " +
                                     roleVM.RoleName +
                                     " from Roles.";
                    return RedirectToAction(nameof(RoleIndex), new { message });
                }
                else
                {
                    string message = "Role deletion failed. " +
                                     roleVM.RoleName +
                                     " may have users attached.";

                    ModelState.AddModelError("", message);
                }
            }
            return View(roleVM);
        }

        /// <summary>
        /// Displays a paginated, sortable, and filterable list of user logs in the system.
        /// </summary>
        /// <param name="sortOrder">The current sort order for the log list.</param>
        /// <param name="emailFilter">An optional email filter.</param>
        /// <param name="abandonedFilter">An optional abandoned-session filter.</param>
        /// <param name="startDate">An optional inclusive start date.</param>
        /// <param name="endDate">An optional inclusive end date.</param>
        /// <param name="pageNumber">The requested page number.</param>
        /// <returns>The user log list view.</returns>
        public async Task<IActionResult> UserLogAll(string? sortOrder, string? emailFilter, string? abandonedFilter, DateTime? startDate, DateTime? endDate, int? pageNumber)
        {
            string currentSortOrder = string.IsNullOrEmpty(sortOrder) ? "login_desc" : sortOrder;

            if (startDate.HasValue && startDate.Value.Date > DateTime.Today)
            {
                startDate = DateTime.Today;
            }

            if (endDate.HasValue && endDate.Value.Date > DateTime.Today)
            {
                endDate = DateTime.Today;
            }

            if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
            {
                TempData["ErrorMessage"] = "Start date cannot be later than end date. Please try again.";
                endDate = startDate;
            }

            ViewBag.CurrentSortOrder = currentSortOrder;
            ViewBag.CurrentEmailFilter = emailFilter;
            ViewBag.CurrentAbandonedFilter = abandonedFilter;
            ViewBag.CurrentStartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentEndDate = endDate?.ToString("yyyy-MM-dd");

            ViewBag.EmailSortParam = currentSortOrder == "email_asc" ? "email_desc" : "email_asc";
            ViewBag.LoginSortParam = currentSortOrder == "login_asc" ? "login_desc" : "login_asc";

            int pageSize = 10;
            int safePageNumber = Math.Max(1, pageNumber ?? 1);

            var userLogVM = await _userLogRepository.GetAllAsync(emailFilter, abandonedFilter, startDate, endDate, currentSortOrder, safePageNumber, pageSize);

            return View(userLogVM);
        }
    }
}
