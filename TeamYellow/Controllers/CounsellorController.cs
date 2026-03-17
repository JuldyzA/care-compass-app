using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    [Authorize(Roles = "Paid_Counselor, Free_Counselor, Registered_Visitor")]
    public class CounsellorController : Controller
    {
        private readonly CounsellorService _counsellorService;
        private readonly ClientService _clientService;
        private readonly IConfiguration _configuration;

        public CounsellorController (
            CounsellorService counsellorService,
            ClientService clientService,
            IConfiguration configuration
        ) {
            _counsellorService = counsellorService;
            _clientService = clientService;
            _configuration = configuration;
        }

        /// <summary>
        /// Displays the counsellor dashboard, loading profile statistics and setting default profile picture path.
        /// </summary>
        /// <returns>A view with the populated counsellor dashboard view model.</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            CounsellorDashboardVM dashboardVM = await _counsellorService.GetCounsellorDashboardAsync(User);
            
            //TODO: Handle case when counsellor subscription (e.g. show message or redirect to subscription page)

            //TODO: Handle case when the user status is not valid (Blur the screen)

            ViewData["DefaultUserProfilePicture"] = _configuration["DefaultSettings:DefaultUserProfilePicture"];

            return View(dashboardVM);
        }


        /// <summary>
        /// Retrieves a paginated list of clients for the counsellor and returns a partial view.
        /// </summary>
        /// <param name="page">The current page number (defaults to 1).</param>
        /// <param name="pageSize">The number of records per page (defaults to 5).</param>
        /// <returns>A partial view containing the client table data.</returns>
        [HttpGet]
        public async Task<IActionResult> ClientTable(int page = 1, int pageSize = 5)
        {
            ClientTableVm clientTableVm = await _clientService.GetClientsByPageAsync(User, page, pageSize);

            return PartialView("_ClientTable", clientTableVm);
        }
    }
}