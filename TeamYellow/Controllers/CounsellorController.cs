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


        public async Task<IActionResult> index()
        {
            CounsellorDashboardDto dashboard = await _service.GetCounsellorDashboardAsync(User);
                
            return View("CounsellorDashboard", dashboard);
        }
    }
}