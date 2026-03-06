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

        public CounsellorController(CounsellorService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            CounsellorDashboardVM? dashboardVM = await _service.GetCounsellorDashboardAsync(User);

            //TODO: Handle null case (e.g. redirect to error page or show message)
            //TODO: Handle case when counsellor subscription (e.g. show message or redirect to subscription page)


            return View(dashboardVM);
        }
    }
}