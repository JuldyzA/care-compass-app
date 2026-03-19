using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers;

[Authorize(Roles = "Paid_Counselor,Free_Counselor,Registered_Visitor")]
public class CounsellorController : Controller
{
    private readonly CounsellorService _counsellorService;
    private readonly ClientService _clientService;

    public CounsellorController (
        CounsellorService counsellorService,
        ClientService clientService,
        IConfiguration configuration
    ) {
        _counsellorService = counsellorService;
        _clientService = clientService;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["Title"] = "Counsellor";
        base.OnActionExecuting(context);
    }

    /// <summary>
    /// Displays the counsellor dashboard, loading profile statistics and setting default profile picture path.
    /// </summary>
    /// <returns>A view with the populated counsellor dashboard view model.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        CounsellorDashboardVM dashboardVM = await _counsellorService.GetCounsellorDashboardAsync(User);

        //TODO: Store these data to seesion for other controllers to access
        if (!string.IsNullOrEmpty(dashboardVM.ProfilePhotoUrl))
            ViewData["UserProfilePicture"] = dashboardVM.ProfilePhotoUrl;
        if (!string.IsNullOrEmpty(dashboardVM.DisplayName))
            ViewData["DisplayName"] = $"Dr. {dashboardVM.DisplayName}";

        //TODO: Handle case when counsellor subscription (e.g. show message or redirect to subscription page)
        //TODO: Handle case when the user status is not valid (Blur the screen)

        return View(dashboardVM);
    }

    [HttpGet]
    [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
    public IActionResult Clients()
    {
        return View();
    }


    /// <summary>
    /// Retrieves a paginated list of clients for the counsellor and returns a partial view.
    /// </summary>
    /// <param name="page">The current page number (defaults to 1).</param>
    /// <param name="pageSize">The number of records per page (defaults to 5).</param>
    /// <param name="isDashboard">Whether the request is from the dashboard.</param>
    /// <param name="searchTerm">An optional search term to filter clients.</param>
    /// <param name="startDate">Optional filter start date (inclusive).</param>
    /// <param name="endDate">Optional filter end date (inclusive).</param>
    /// <returns>A partial view containing the client table data.</returns>
    [HttpGet]
    public async Task<IActionResult> ClientTable(int page = 1, int pageSize = 10, bool isDashboard = false, string? searchTerm = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (isDashboard) pageSize = 5;
        ClientTableVm clientTableVm = await _clientService.GetClientsByPageAsync(User, page, pageSize, searchTerm, startDate, endDate);
        clientTableVm.IsDashboard = isDashboard;

        return PartialView("_ClientTable", clientTableVm);
    }
}