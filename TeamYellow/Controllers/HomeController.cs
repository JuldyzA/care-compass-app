using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Models;

namespace TeamYellow.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Paid_Counselor") || User.IsInRole("Free_Counselor") || User.IsInRole("Registered_Visitor"))
            {
                return RedirectToAction("Index", "Counsellor");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
