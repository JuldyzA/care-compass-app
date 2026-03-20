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

    public CounsellorController(CounsellorService counsellorService, ClientService clientService)
    {
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

    [HttpGet]
    [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
    public IActionResult CreateClient()
    {
        var vm = new ClientVM();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
    public async Task<IActionResult> CreateClient(ClientVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var counsellorInfo = await _counsellorService.GetCounsellorByUser(User);
        if (counsellorInfo == null)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while processing your request.");
            return View(model);
        }

        bool created = await _clientService.CreateClientAsync(model, User, counsellorInfo.CounsellorId);

        if (!created)
        {
            ModelState.AddModelError("Email", "A client with this email already exists.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Client {model.FirstName} {model.LastName} has been created successfully.";
        return RedirectToAction(nameof(Clients));
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
    /// <param name="sortColumn">Optional column to sort by (patient, datetime, email).</param>
    /// <param name="sortDir">Optional sort direction (asc or desc).</param>
    /// <returns>A partial view containing the client table data.</returns>
    [HttpGet]
    public async Task<IActionResult> ClientTable
    (
        int page = 1,
        int pageSize = 10,
        bool isDashboard = false,
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? sortColumn = null,
        string? sortDir = null
    ) {

        if (isDashboard) pageSize = 5;
        ClientTableVm clientTableVm = await _clientService.GetClientsByPageAndFilterAsync 
        (
            User,
            page,
            pageSize,
            searchTerm,
            startDate,
            endDate,
            sortColumn,
            sortDir
        );
        clientTableVm.IsDashboard = isDashboard;

        return PartialView("_ClientTable", clientTableVm);
    }

    /// <summary>
    /// Displays the details of a specific client.
    /// </summary>
    /// <param name="id">The client ID.</param>
    /// <returns>A view with the client details or a not found result if the client doesn't exist.</returns>
    [HttpGet]
    [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
    public async Task<IActionResult> ClientDetail(int id)
    {
        var client = await _clientService.GetClientByIdAsync(id, User);
        
        if (client == null)
        {
            return NotFound();
        }

        return View(client);
    }

    /// <summary>
    /// Displays the edit form for a specific client.
    /// </summary>
    /// <param name="id">The client ID.</param>
    /// <returns>A view with the client edit form or a not found result if the client doesn't exist.</returns>
    [HttpGet]
    [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
    public async Task<IActionResult> EditClient(int id)
    {
        var client = await _clientService.GetClientByIdAsync(id, User);

        if (client == null)
        {
            return NotFound();
        }

        return View(client);
    }

    /// <summary>
    /// Updates the client information and redirects back to the client detail view.
    /// </summary>
    /// <param name="id">The client ID.</param>
    /// <param name="vm">The updated client view model.</param>
    /// <returns>Redirects to ClientDetail on success or returns the edit view on failure.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
    public async Task<IActionResult> EditClient(int id, ClientVM vm)
    {
        if (id != vm.ClientId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        bool updated = await _clientService.UpdateClientAsync(vm, User);

        if (!updated)
        {
            ModelState.AddModelError("Email", "A client with this email already exists, or the client could not be found.");
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Client {vm.FirstName} {vm.LastName} has been updated successfully.";
        return RedirectToAction(nameof(ClientDetail), new { id = vm.ClientId });
    }

    /// <summary>
    /// Deletes a specific client and redirects back to the clients list.
    /// Performs authorization checks to ensure the client belongs to the authenticated counsellor.
    /// </summary>
    /// <param name="id">The client ID to delete.</param>
    /// <returns>Redirects to the Clients action with a success or error message.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Paid_Counselor,Free_Counselor")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        bool deleted = await _clientService.DeleteClientAsync(id, User);

        if (!deleted)
        {
            TempData["ErrorMessage"] = "Unable to delete the client. The client may not exist or you do not have permission to delete it.";
            return RedirectToAction(nameof(ClientDetail), new { id = id });
        }

        TempData["SuccessMessage"] = "Client has been deleted successfully.";
        return RedirectToAction(nameof(Clients));
    }
}