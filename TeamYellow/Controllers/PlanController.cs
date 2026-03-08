using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.DTOs;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers;

[Authorize]
public class PlanController : Controller
{
    private readonly IPlanService _planService;
    private readonly ICounsellorRepository _counsellorRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly UserManager<IdentityUser> _userManager;

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

    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Manage()
    {
        var plans = await _planService.GetAllPlans();
        return View(plans.Select(MapToPlanVM).ToList());
    }

    [Authorize(Roles = "Manager")]
    public IActionResult Create()
    {
        return View();
    }

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
