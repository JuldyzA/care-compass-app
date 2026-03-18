using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers;

/// <summary>
/// Controller responsible for managing subscription plans,
/// including listing, checkout, creation, editing, and deletion.
/// Requires authentication by default; individual actions may restrict or relax this requirement.
/// </summary>
[Authorize]
public class PlanController : Controller
{
    private readonly IPlanService _planService;
    private readonly CounsellorRepository _counsellorRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly UserManager<IdentityUser> _userManager;

    /// <summary>
    /// Initializes a new instance of <see cref="PlanController"/>.
    /// </summary>
    /// <param name="planService">Service used to retrieve and manage plans.</param>
    /// <param name="counsellorRepository">Repository for counsellor data access.</param>
    /// <param name="subscriptionRepository">Repository for subscription data access.</param>
    /// <param name="userManager">ASP.NET Identity user manager.</param>
    public PlanController(
        IPlanService planService,
        CounsellorRepository counsellorRepository,
        ISubscriptionRepository subscriptionRepository,
        UserManager<IdentityUser> userManager)
    {
        _planService = planService;
        _counsellorRepository = counsellorRepository;
        _subscriptionRepository = subscriptionRepository;
        _userManager = userManager;
    }

    /// <summary>
    /// Displays a list of all active subscription plans.
    /// If the current user is an authenticated <c>Paid_Counselor</c>, their current plan ID
    /// is injected into <see cref="Controller.ViewData"/> so the view can highlight it.
    /// </summary>
    /// <returns>The plan listing view with a list of <see cref="PlanVM"/> objects.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var plans = await _planService.GetActivePlans();

        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Paid_Counselor"))
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
                if (counsellor != null)
                {
                    var subscription = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellor.CounsellorId);
                    ViewData["CurrentPlanId"] = subscription?.PlanId;
                }
            }
        }
        // Copy TempData message (if any) into ViewData so the view can render it
        if (TempData.ContainsKey("Message"))
            ViewData["Message"] = TempData["Message"];
        if (TempData.ContainsKey("MessageType"))
            ViewData["MessageType"] = TempData["MessageType"];

        return View(plans.Select(MapToPlanVM).ToList());
    }

    /// <summary>
    /// Displays the checkout page for the specified plan.
    /// Redirects back to the plan index with an appropriate error message if the user
    /// is already subscribed to the requested plan or attempts an invalid plan change.
    /// </summary>
    /// <param name="id">The ID of the plan the user wants to check out.</param>
    /// <returns>
    /// The checkout view for the requested plan, or a redirect/not-found result
    /// if the plan is unavailable or the user is already subscribed.
    /// </returns>
    [Authorize(Roles = "Registered_Visitor,Paid_Counselor,Free_Counselor")]
    public async Task<IActionResult> Checkout(int id)
    {
        var plan = await _planService.GetPlanById(id);

        if (plan == null || !plan.IsActive)
            return NotFound();

        if (User.IsInRole("Free_Counselor") && plan.Price == 0)
        {

            TempData["Message"] = "You are already subscribed to this plan.";
            TempData["MessageType"] = "info";
            return RedirectToAction(nameof(Index));
        }

        if (User.IsInRole("Paid_Counselor"))
        {
            if (plan.Price == 0)
            {
                TempData["Message"] = "You cannot downgrade to the Free plan from here.";
                TempData["MessageType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
                if (counsellor != null)
                {
                    var subscription = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellor.CounsellorId);
                    if (subscription?.PlanId == id)
                    {
                        TempData["Message"] = "You are already subscribed to this plan.";
                        TempData["MessageType"] = "info";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
        }

        return View(MapToPlanVM(plan));
    }

    /// <summary>
    /// Maps a <see cref="Plan"/> domain model to a <see cref="PlanVM"/> view model.
    /// </summary>
    /// <param name="plan">The plan domain model to map.</param>
    /// <returns>A <see cref="PlanVM"/> populated from the given <paramref name="plan"/>.</returns>
    private static PlanVM MapToPlanVM(Plan plan) => new()
    {
        PlanId = plan.PlanId,
        PlanName = plan.PlanName,
        PlanDescription = plan.PlanDescription,
        Price = plan.Price,
        BillingType = plan.BillingType,
        IsActive = plan.IsActive,
        PlanFeatures = [.. plan.PlanFeatures.Select(f => new PlanFeatureVM
            {
                FeatureName = f.FeatureName,
                FeatureDescription = f.FeatureDescription
            })]
    };
}
