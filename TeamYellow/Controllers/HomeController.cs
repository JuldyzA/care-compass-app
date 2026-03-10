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
                    return RedirectToAction("UserRoleIndex", "Admin");

                //TODO: Uncomment and update code for other roles' Index views
                /*if (User.IsInRole("Manager"))
                    return RedirectToAction("Index", "Manager");

                if (User.IsInRole("Paid_Counselor") || User.IsInRole("Free_Counselor"))
                    return RedirectToAction("Index", "Counsellor");*/
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
