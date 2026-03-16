using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    /// <summary>
    /// Controller responsible for managing application users and roles,
    /// and displaying user logs.
    /// Access is restricted to Administrator.
    /// </summary>
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly RoleRepository _roleRepository;
        private readonly UserRepository _userRepository;
        private readonly UserRoleRepository _userRoleRepository;
        private readonly UserLogRepository _userLogRepository;

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
        public async Task<IActionResult> UserRoleIndex(string? sortOrder, string? emailFilter, int? pageNumber)
        {
            string currentSortOrder = string.IsNullOrEmpty(sortOrder) ? "email_asc" : sortOrder;
            ViewBag.CurrentSortOrder = currentSortOrder;
            ViewBag.CurrentEmailFilter = emailFilter;

            ViewBag.EmailSortParam = currentSortOrder == "email_asc" ? "email_desc" : "email_asc";

            // Will change this later accordingly
            int pageSize = 5;
            int safePageNumber = Math.Max(1, pageNumber ?? 1);

            var users = await _userRepository.GetAllUsersAsync(emailFilter, currentSortOrder, safePageNumber, pageSize);
            return View(users);
        }

        /// <summary>
        /// Displays all available roles assigned to user in the system.
        /// </summary>
        public async Task<IActionResult> UserRoleDetail(string userName, string message = "", bool isError = false)
        {
            var roles = await _userRoleRepository.GetUserRolesAsync(userName);
            ViewBag.Message = message;
            ViewBag.UserName = userName;
            ViewBag.IsError = isError;
            
            return View(roles);
        }

        /// <summary>
        /// Displays the form for assigning a role to a user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UserRoleCreate(string? email)
        {
            ViewBag.RoleSelectList = await _roleRepository.GetRoleSelectListAsync();
            ViewBag.UserSelectList = await _userRepository.GetUserSelectListAsync(email);

            return View();
        }

        /// <summary>
        /// Assigns a selected role to a user.
        /// </summary>
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
        /// Removes a role from a user securely using POST and Anti-Forgery validation.
        /// </summary>
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
        public async Task<IActionResult> RoleIndex(string message = "")
        {
            IEnumerable<RoleVM> roles = await _roleRepository.GetAllRolesVMAsync();
            ViewBag.Message = message;

            return View(roles);
        }

        /// <summary>
        /// Displays the form for creating a new role.
        /// </summary>
        [HttpGet]
        public IActionResult RoleCreate()
        {
            return View(new RoleVM());
        }

        /// <summary>
        /// Creates a new role if it does not already exist.
        /// </summary>
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
        /// Deletes a role securely using POST and Anti-Forgery Token validation.
        /// A role cannot be deleted if users are currently assigned to it.
        /// </summary>
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

            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
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

            // Will change this later accordingly
            int pageSize = 10;
            int safePageNumber = Math.Max(1, pageNumber ?? 1);

            var userLogVM = await _userLogRepository.GetAllAsync(emailFilter, abandonedFilter, startDate, endDate, currentSortOrder, safePageNumber, pageSize);

            return View(userLogVM);
        }
    }
}
