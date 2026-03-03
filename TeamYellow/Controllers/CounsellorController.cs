using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeamYellow.DTOs;
using TeamYellow.Services;

namespace TeamYellow.Controllers
{
    [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
    public class CounsellorController : Controller
    {
        private readonly CounsellorService _service;

        public CounsellorController(CounsellorService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            if (User.IsInRole("Counsellor"))
            {
                CounsellorDashboardDto dashboard = await _service.GetCounsellorDashboardAsync(User);
                return View("CounsellorDashboard", dashboard);
            }

            if (User.IsInRole("RegisteredVisitor"))
            {
                return View("VisitorDashboard");
            }

            return Forbid();
        }
    }
}