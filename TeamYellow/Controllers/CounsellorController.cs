using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    [Authorize]
    public class CounsellorController : Controller
    {
        private readonly CounsellorService _service;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<CounsellorController> _logger;

        public CounsellorController (
            CounsellorService service, 
            IConfiguration configuration, 
            SignInManager<IdentityUser> signInManager, 
            ILogger<CounsellorController> logger
        ) {
            _service = service;
            _configuration = configuration;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            CounsellorDashboardVM? dashboardVM = await _service.GetCounsellorDashboardAsync(User);

            //TODO: Handle null case (e.g. redirect to error page or show message)
            
            //TODO: Handle case when counsellor subscription (e.g. show message or redirect to subscription page)

            //TODO: Handle case when the user status is not valid (Blur the screen)
            if (dashboardVM == null)
            {
                dashboardVM = new CounsellorDashboardVM();
            }

            ViewData["DefaultUserProfilePicture"] = _configuration["DefaultSettings:DefaultUserProfilePicture"];

            return View(dashboardVM);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            // Break the session?
            //HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}