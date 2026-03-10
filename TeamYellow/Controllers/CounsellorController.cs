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

        public CounsellorController(CounsellorService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            CounsellorDashboardVM? dashboardVM = await _service.GetCounsellorDashboardAsync(User);

            //TODO: Handle null case (e.g. redirect to error page or show message)
            
            //TODO: Handle case when counsellor subscription (e.g. show message or redirect to subscription page)

            //TODO: Handle case when the user status is not valid (Blur the screen)
            if (dashboardVM == null)
            {
                dashboardVM = new CounsellorDashboardVM();
            }

            ViewData["DefaultUserProfilePicture"] = _configuration["DefaultSettings:DefaultUserProfilePicture"];

            return View(dashboardVM);
        }
    }
}