using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Models;

namespace TeamYellow.Controllers
{
    /// <summary>
    /// Handles the public home pages and redirects authenticated users to the appropriate dashboard based on their role.
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Displays the landing page for unauthenticated users or redirects authenticated users to the appropriate dashboard based on their role.
        /// </summary>
        /// <returns>
        /// The home page view for anonymous users, or a redirect to the Admin, Counsellor, or Manager dashboard for authenticated users.
        /// </returns>
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

        /// <summary>
        /// Displays the privacy page.
        /// </summary>
        /// <returns>The privacy view.</returns>
        public IActionResult Privacy()
        {
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Solution()
        {
            return View();
        }

        /// <summary>
        /// Displays the application error page.
        /// </summary>
        /// <returns>
        /// The error view populated with the current request identifier.
        /// </returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
