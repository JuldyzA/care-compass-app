using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}