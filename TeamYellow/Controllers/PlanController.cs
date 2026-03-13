using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.DTOs;
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
    private readonly ICounsellorRepository _counsellorRepository;
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
        ICounsellorRepository counsellorRepository,
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
            return RedirectToAction(nameof(Index), new { error = "You are already subscribed to this plan." });

        if (User.IsInRole("Paid_Counselor"))
        {
            if (plan.Price == 0)
                return RedirectToAction(nameof(Index), new { error = "You cannot downgrade to the Free plan from here." });

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var counsellor = await _counsellorRepository.GetByUserIdAsync(user.Id);
                if (counsellor != null)
                {
                    var subscription = await _subscriptionRepository.GetActiveSubscriptionByCounsellorId(counsellor.CounsellorId);
                    if (subscription?.PlanId == id)
                        return RedirectToAction(nameof(Index), new { error = "You are already subscribed to this plan." });
                }
            }
        }

        return View(MapToPlanVM(plan));
    }

    /// <summary>
    /// Displays the plan management page with all plans (active and inactive).
    /// Restricted to users with the <c>Manager</c> role.
    /// </summary>
    /// <returns>The management view with a full list of <see cref="PlanVM"/> objects.</returns>
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Manage()
    {
        var plans = await _planService.GetAllPlans();
        return View(plans.Select(MapToPlanVM).ToList());
    }

    /// <summary>
    /// Displays the plan creation form.
    /// Restricted to users with the <c>Manager</c> role.
    /// </summary>
    /// <returns>The plan creation view.</returns>
    [Authorize(Roles = "Manager")]
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    /// Processes the submitted plan creation form.
    /// Restricted to users with the <c>Manager</c> role.
    /// Returns the form view with errors if validation fails or an exception occurs.
    /// </summary>
    /// <param name="addPlanDto">The DTO containing new plan data submitted from the form.</param>
    /// <returns>Redirects to <see cref="Manage"/> on success; otherwise re-displays the form with errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create(AddPlanDto addPlanDto)
    {
        if (!ModelState.IsValid)
        {
            return View(addPlanDto);
        }

        try
        {
            await _planService.AddPlan(addPlanDto);
            return RedirectToAction(nameof(Manage));
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while creating the plan. Please try again.");
            return View(addPlanDto);
        }
    }

    /// <summary>
    /// Displays the plan edit form pre-populated with the current plan data.
    /// Restricted to users with the <c>Manager</c> role.
    /// </summary>
    /// <param name="id">The ID of the plan to edit.</param>
    /// <returns>The edit view with an <see cref="UpdatePlanDto"/>, or <c>404 Not Found</c> if the plan does not exist.</returns>
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var plan = await _planService.GetPlanById(id);

        if (plan == null)
        {
            return NotFound();
        }

        var updatePlanDto = new UpdatePlanDto
        {
            PlanId = plan.PlanId,
            PlanName = plan.PlanName,
            PlanDescription = plan.PlanDescription,
            Price = plan.Price,
            BillingType = plan.BillingType,
            IsActive = plan.IsActive,
            PlanFeatureDtos = [.. plan.PlanFeatures.Select(f => new AddPlanFeatureDto
                {
                    FeatureName = f.FeatureName,
                    FeatureDescription = f.FeatureDescription
                })],
        };

        return View(updatePlanDto);
    }

    /// <summary>
    /// Processes the submitted plan edit form.
    /// Restricted to users with the <c>Manager</c> role.
    /// </summary>
    /// <param name="updatePlanDto">The DTO containing updated plan data submitted from the form.</param>
    /// <returns>
    /// Redirects to <see cref="Manage"/> on success; returns <c>404 Not Found</c> if the plan
    /// does not exist; otherwise re-displays the form with an error message.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Edit(UpdatePlanDto updatePlanDto)
    {
        if (!ModelState.IsValid)
        {
            return View(updatePlanDto);
        }

        try
        {
            await _planService.UpdatePlan(updatePlanDto);
            return RedirectToAction(nameof(Manage));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while updating the plan. Please try again.");
            return View(updatePlanDto);
        }
    }

    /// <summary>
    /// Soft-deletes (deactivates) the specified plan.
    /// Restricted to users with the <c>Manager</c> role.
    /// </summary>
    /// <param name="id">The ID of the plan to delete.</param>
    /// <returns>
    /// Redirects to <see cref="Manage"/> on success or failure;
    /// returns <c>404 Not Found</c> if the plan does not exist.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _planService.DeletePlan(id);

            if (!deleted)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Manage));
        }
        catch (Exception)
        {
            return RedirectToAction(nameof(Manage));
        }
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
