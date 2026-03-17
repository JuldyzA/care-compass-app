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

        /// <summary>
        /// Displays the page depending on the role of the user
        /// </summary>
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Administrator"))
                {
                    return RedirectToAction("UserRoleIndex", "Admin");
                }
                else if (User.IsInRole("Paid_Counselor") || User.IsInRole("Free_Counselor") || User.IsInRole("Registered_Visitor"))
                {
                    return RedirectToAction("Index", "Counsellor");
                }
                else if (User.IsInRole("Manager"))
                {
                    return RedirectToAction("Index", "Manager");
                }
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
