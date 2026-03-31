using Microsoft.AspNetCore.Mvc;

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
        /// Displays a generic status-code page using shared views.
        /// </summary>
        /// <param name="statusCode">The HTTP status code captured by the status code pages middleware.</param>
        /// <returns>A status-specific error view.</returns>
        public IActionResult StatusCodeError(int? statusCode)
        {
            if (!statusCode.HasValue)
            {
                return RedirectToAction(nameof(Error));
            }

            switch (statusCode.Value)
            {
                case 404:
                    Response.StatusCode = 404;
                    return View("~/Views/Shared/NotFound.cshtml");
                case 500:
                    Response.StatusCode = 500;
                    return View("~/Views/Shared/ServerError.cshtml");
                case 403:
                    Response.StatusCode = 403;
                    return View("~/Views/Shared/Forbidden.cshtml");
                case 401:
                    Response.StatusCode = 401;
                    return View("~/Views/Shared/Unauthorized.cshtml");

                default:
                    Response.StatusCode = (statusCode.Value >= 400 && statusCode.Value <= 599)
                    ? statusCode.Value
                    : 500;
                    return View("~/Views/Shared/Error.cshtml");
            }
        }

        /// <summary>
        /// Displays the application error page.
        /// </summary>
        /// <returns>
        /// A generic error view.
        /// </returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            Response.StatusCode = 500;
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
