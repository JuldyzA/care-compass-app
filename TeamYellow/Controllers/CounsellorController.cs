using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.DTOs;
using TeamYellow.Services;

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
            CounsellorDashboardDto? dashboard = await _service.GetCounsellorDashboardAsync(User);

            //TODO: Handle null case (e.g. redirect to error page or show message)
            //TODO: Handle case when counsellor subscription (e.g. show message or redirect to subscription page)

            
            return View(dashboard);
        }
    }
}