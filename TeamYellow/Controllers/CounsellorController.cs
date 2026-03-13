using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    [Authorize]
    public class CounsellorController : Controller
    {
        private readonly CounsellorService _service;
        private readonly IConfiguration _configuration;

        public CounsellorController (
            CounsellorService service, 
            IConfiguration configuration,
            ILogger<CounsellorController> logger
        ) {
            _service = service;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            CounsellorDashboardVM dashboardVM = await _service.GetCounsellorDashboardAsync(User);
            
            //TODO: Handle case when counsellor subscription (e.g. show message or redirect to subscription page)

            //TODO: Handle case when the user status is not valid (Blur the screen)
            if (dashboardVM == null)
            {
                dashboardVM = new CounsellorDashboardVM();
            }

            ViewData["DefaultUserProfilePicture"] = _configuration["DefaultSettings:DefaultUserProfilePicture"];

            return View(dashboardVM);
        }

        [HttpGet]
        public async Task<IActionResult> ClientTable(int page = 1, int pageSize = 5)
        {
            ClientTableVm clientTableVm = await _service.GetClientsAsync(User, page, pageSize);

            return PartialView("_ClientTable", clientTableVm);
        }
    }
}