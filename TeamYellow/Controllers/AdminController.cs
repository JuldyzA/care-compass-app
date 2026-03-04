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
        /// Get all available users in the system.
        /// </summary>
        public IActionResult UserRoleIndex()
        {
            var users = _userRepository.GetAllUsers();
            return View(users);
        }

        /// <summary>
        /// Displays all available roles assigned to user in the system.
        /// </summary>
        public async Task<IActionResult> UserRoleDetail(string userName, string message = "")
        {
            var roles = await _userRoleRepository.GetUserRolesAsync(userName);
            ViewBag.Message = message;
            ViewBag.UserName = userName;
            
            return View(roles);
        }

        /// <summary>
        /// Displays the form for assigning a role to a user.
        /// </summary>
        public IActionResult UserRoleCreate(string? email)
        {
            ViewBag.RoleSelectList = _roleRepository.GetRoleSelectList();
            ViewBag.UserSelectList = _userRepository.GetUserSelectList(email);

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

            ViewBag.RoleSelectList = _roleRepository.GetRoleSelectList();
            ViewBag.UserSelectList = _userRepository.GetUserSelectList(userRoleVM.Email);

            return View(userRoleVM);
        }

        /// <summary>
        /// Displays a confirmation page before removing a role from a user.
        /// </summary>
        public IActionResult UserRoleDelete(string email, string roleName)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(roleName))
            {
                ModelState.AddModelError("",
                                         "Email and Role Name " +
                                         "are required.");

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
        public IActionResult RoleIndex(string message = "")
        {
            IEnumerable<RoleVM> roles = _roleRepository.GetAllRolesVM();
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
        public IActionResult RoleCreate(RoleVM roleVM)
        {
            if (ModelState.IsValid)
            {
                bool isSuccess = _roleRepository.CreateRole(roleVM.RoleName);

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
                    _logger.LogError(message);
                }
            }
            return View(roleVM);
        }

        /// <summary>
        /// Displays a confirmation page before removing a role.
        /// </summary>
        public IActionResult RoleDelete(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                string message = "Role name cannot be empty.";
                _logger.LogWarning(message);
                return RedirectToAction(nameof(RoleIndex), new { message });
            }

            RoleVM? role = _roleRepository.GetRoleVM(roleName);

            if (role == null)
            {
                string message = $"Role '{roleName}' not found";
                _logger.LogWarning(message);
                return RedirectToAction(nameof(RoleIndex), new { message = message });
            }
            return View(role);
        }

        /// <summary>
        /// Deletes a role securely using POST and Anti-Forgery Token validation.
        /// A role cannot be deleted if users are currently assigned to it.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RoleDelete(RoleVM roleVM)
        {
            if (ModelState.IsValid)
            {
                bool isSuccess = _roleRepository.DeleteRole(roleVM.RoleName);

                if (isSuccess)
                {
                    string message = "Successfully removed " +
                                     roleVM.RoleName +
                                     " from Roles.";
                    return RedirectToAction(nameof(RoleIndex), new { message = message });
                }
                else
                {
                    string message = "Role deletion failed. " +
                                     roleVM.RoleName +
                                     " may have users attached.";

                    ModelState.AddModelError("", message);
                    _logger.LogError(message);
                }
            }
            return View(roleVM);
        }

        /// <summary>
        /// Get all the user logs for Admin
        /// </summary>
        public IActionResult UserLogAll()
        {
            var userLogVM = _userLogRepository.GetAll();

            return View(userLogVM);
        }
    }
}
