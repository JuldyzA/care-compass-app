using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    /// <summary>
    /// Handles counsellor dashboard and client management workflows for authorized counsellor users.
    /// </summary>
    [Authorize(Roles = "Paid_Counselor,Free_Counselor,Registered_Visitor")]
    public class CounsellorController : Controller
    {
        private readonly CounsellorService _counsellorService;
        private readonly ClientService _clientService;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CounsellorController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CounsellorController"/> class.
        /// </summary>
        /// <param name="counsellorService">Provides counsellor dashboard and profile-related operations.</param>
        /// <param name="clientService">Provides client management operations for counsellors.</param>
        public CounsellorController(
            CounsellorService counsellorService, 
            ClientService clientService,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<CounsellorController> logger
            )
        {
            _counsellorService = counsellorService;
            _clientService = clientService;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Runs before every counsellor action to evaluate subscription access state.
        /// If the subscription is expired or inactive, the user is redirected to the locked dashboard.
        /// </summary>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ViewData["Title"] = "Counsellor";

            var accessState = await _counsellorService.GetCounsellorPageAccessStateAsync(User);

            if (!string.IsNullOrEmpty(accessState.ProfilePhotoUrl))
                ViewData["UserProfilePicture"] = accessState.ProfilePhotoUrl;

            if (!string.IsNullOrEmpty(accessState.DisplayName))
                ViewData["DisplayName"] = $"Dr. {accessState.DisplayName}";

            ViewData["IsCounsellorAccessLocked"] = accessState.IsLocked;
            ViewData["DisablePageScroll"] = accessState.IsLocked;

            if (!string.IsNullOrWhiteSpace(accessState.ErrorMessage))
            {
                TempData["ErrorMessage"] = accessState.ErrorMessage;
            }

            if (accessState.ShouldRefreshSignIn)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    _logger.LogInformation("Refreshing sign-in for user {Email} after counsellor access state update.", user.Email);
                    await _signInManager.RefreshSignInAsync(user);
                }
                else
                {
                    _logger.LogWarning("Could not resolve current user for sign-in refresh.");
                    TempData["ErrorMessage"] = "We could not refresh your subscription access automatically. Please sign out and sign in again.";
                }

                context.Result = RedirectToAction(nameof(Index));
                return;
            }

            string? actionName = context.ActionDescriptor.RouteValues["action"];

            if (accessState.IsLocked)
            {
                if (TempData["ErrorMessage"] == null)
                {
                    TempData["ErrorMessage"] = "Your subscription is inactive or expired. Please activate a plan to continue.";
                }

                if (!string.Equals(actionName, nameof(Locked), StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = RedirectToAction(nameof(Locked));
                    return;
                }
            }

            await next();
        }

        /// <summary>
        /// Displays the counsellor dashboard with profile information, subscription details, and summary statistics.
        /// </summary>
        /// <returns>A view with the populated counsellor dashboard view model.</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            CounsellorDashboardVM dashboardVM = await _counsellorService.GetCounsellorDashboardAsync(User);
            return View(dashboardVM);
        }

        /// <summary>
        /// Displays a minimal locked dashboard state when subscription access is restricted.
        /// This avoids sending the full dashboard data payload to locked users.
        /// </summary>
        /// <returns>The counsellor dashboard view populated with a minimal locked-state model.</returns>
        [HttpGet]
        public IActionResult Locked()
        {
            ViewData["IsCounsellorAccessLocked"] = true;
            ViewData["DisablePageScroll"] = true;

            var vm = new CounsellorDashboardVM
            {
                IsDashboardLocked = true,
                IsSubscriptionActive = false,
                RemainingSubscriptionText = "expired",
                MonthlyClientCounts = new int[12],
                ActiveClientCount = 0,
                InactiveClientCount = 0,
                ClientGrowthFromLastMonth = 0,
                CycleStart = DateTime.MinValue,
                CycleEnd = DateTime.MinValue
            };

            return View(nameof(Index), vm);
        }

        /// <summary>
        /// Displays the client list page for authorized counsellors.
        /// </summary>
        /// <returns>The client list view.</returns>
        [HttpGet]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public IActionResult Clients()
        {
            return View();
        }

        /// <summary>
        /// Displays the form for creating a new client.
        /// </summary>
        /// <returns>A view containing a new <see cref="ClientVM"/> instance.</returns>
        [HttpGet]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public IActionResult CreateClient()
        {
            var vm = new ClientVM();
            return View(vm);
        }

        /// <summary>
        /// Creates a new client for the authenticated counsellor.
        /// </summary>
        /// <param name="model">The submitted client view model.</param>
        /// <returns>
        /// A redirect to the client list when successful; otherwise returns the create view with validation errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public async Task<IActionResult> CreateClient(ClientVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var counsellorInfo = await _counsellorService.GetCounsellorByUser(User);
            if (counsellorInfo == null)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while processing your request.");
                return View(model);
            }

            bool created = await _clientService.CreateClientAsync(model, counsellorInfo.CounsellorId);

            if (!created)
            {
                ModelState.AddModelError("Email", "A client with this email already exists.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Client {model.FirstName} {model.LastName} has been created successfully.";
            return RedirectToAction(nameof(Clients));
        }

        /// <summary>
        /// Returns a partial view containing a paginated, sortable, and filterable client table.
        /// </summary>
        /// <param name="page">The requested page number.</param>
        /// <param name="pageSize">The number of records per page.</param>
        /// <param name="isDashboard">Indicates whether the table is being rendered for the dashboard.</param>
        /// <param name="searchTerm">An optional search term used to filter clients.</param>
        /// <param name="startDate">An optional inclusive start date filter.</param>
        /// <param name="endDate">An optional inclusive end date filter.</param>
        /// <param name="sortColumn">An optional column name to sort by.</param>
        /// <param name="sortDir">An optional sort direction.</param>
        /// <returns>A partial view containing the filtered client table.</returns>
        [HttpGet]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public async Task<IActionResult> ClientTable
        (
            int page = 1,
            int pageSize = 10,
            bool isDashboard = false,
            string? searchTerm = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? sortColumn = null,
            string? sortDir = null
        ) {
            if (isDashboard) pageSize = 5;
            ClientTableVM clientTableVm = await _clientService.GetClientsByPageAndFilterAsync 
            (
                User,
                page,
                pageSize,
                searchTerm,
                startDate,
                endDate,
                sortColumn,
                sortDir
            );
            clientTableVm.IsDashboard = isDashboard;

            return PartialView("_ClientTable", clientTableVm);
        }

        /// <summary>
        /// Displays the details of a specific client.
        /// </summary>
        /// <param name="id">The client identifier.</param>
        /// <returns>
        /// The client detail view when the client exists; otherwise returns a not found result.
        /// </returns>
        [HttpGet]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public async Task<IActionResult> ClientDetail(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id, User);
        
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        /// <summary>
        /// Displays the edit form for a specific client.
        /// </summary>
        /// <param name="id">The client identifier.</param>
        /// <returns>
        /// The edit view when the client exists; otherwise returns a not found result.
        /// </returns>
        [HttpGet]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public async Task<IActionResult> EditClient(int id)
        {
            var client = await _clientService.GetClientByIdAsync(id, User);

            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        /// <summary>
        /// Updates an existing client and redirects back to the client detail page.
        /// </summary>
        /// <param name="id">The client identifier.</param>
        /// <param name="vm">The submitted client view model containing updated values.</param>
        /// <returns>
        /// A redirect to the client detail page when successful; otherwise returns the edit view with errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public async Task<IActionResult> EditClient(int id, ClientVM vm)
        {
            if (id != vm.ClientId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            bool updated = await _clientService.UpdateClientAsync(vm, User);

            if (!updated)
            {
                ModelState.AddModelError("Email", "A client with this email already exists, or the client could not be found.");
                return View(vm);
            }

            TempData["SuccessMessage"] = $"Client {vm.FirstName} {vm.LastName} has been updated successfully.";
            return RedirectToAction(nameof(ClientDetail), new { id = vm.ClientId });
        }

        /// <summary>
        /// Deletes a specific client and redirects back to the client list.
        /// </summary>
        /// <param name="id">The client identifier to delete.</param>
        /// <returns>
        /// A redirect to the client list when successful; otherwise redirects back to the client detail page with an error message.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            bool deleted = await _clientService.DeleteClientAsync(id, User);

            if (!deleted)
            {
                TempData["ErrorMessage"] = "Unable to delete the client. The client may not exist or you do not have permission to delete it.";
                return RedirectToAction(nameof(ClientDetail), new { id = id });
            }

            TempData["SuccessMessage"] = "Client has been deleted successfully.";
            return RedirectToAction(nameof(Clients));
        }

        /// <summary>
        /// Placeholder action for future session notes feature (not yet implemented).
        /// Currently displays a coming soon page with a redirect link to the dashboard.
        /// </summary>
        /// <returns>A view indicating the feature is under development.</returns>
        [HttpGet]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public IActionResult Appointments()
        {
            return View();
        }

        /// <summary>
        /// Displays the notifications page for counsellors.
        /// </summary>
        /// <returns>A view indicating the feature is under development.</returns>
        [HttpGet]
        [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
        public IActionResult Notifications()
        {
            return View();
        }
    }
}