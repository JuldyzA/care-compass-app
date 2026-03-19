using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    [Authorize(Roles = "Paid_Counselor, Free_Counselor, Registered_Visitor")]
    public class CounsellorController : Controller
    {
        private readonly CounsellorService _service;
        private readonly IConfiguration _configuration;

        public CounsellorController (
            CounsellorService service, 
            IConfiguration configuration
        ) {
            _service = service;
            _configuration = configuration;
        }

        /// <summary>
        /// Displays the counsellor dashboard, loading profile statistics and setting default profile picture path.
        /// </summary>
        /// <returns>A view with the populated counsellor dashboard view model.</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            CounsellorDashboardVM dashboardVM = await _service.GetCounsellorDashboardAsync(User);
            
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
        [Authorize(Roles = "Free_Counselor,Paid_Counselor")]
        public async Task<IActionResult> ClientTable(int page = 1, int pageSize = 5)
        {
            ClientTableVm clientTableVm = await _service.GetClientsAsync(User, page, pageSize);

            return PartialView("_ClientTable", clientTableVm);
        }
    }
}