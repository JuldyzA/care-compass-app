using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamYellow.DTOs;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{

    [Authorize(Roles = "Manager")]
    public class PlanController(IPlanService planService) : Controller
    {
        private readonly IPlanService _planService = planService;

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var plans = await _planService.GetActivePlans();
            var planVMs = plans.Select(p => new PlanVM
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                PlanDescription = p.PlanDescription,
                Price = p.Price,
                BillingType = p.BillingType,
                IsActive = p.IsActive,
                PlanFeatures = [.. p.PlanFeatures.Select(f => new PlanFeatureVM
                {
                    FeatureName = f.FeatureName,
                    FeatureDescription = f.FeatureDescription
                })]
            }).ToList();

            return View(planVMs);
        }

        [Authorize]
        public async Task<IActionResult> Checkout(int id)
        {
            var plan = await _planService.GetPlanById(id);

            if (plan == null || !plan.IsActive)
            {
                return NotFound();
            }

            var planVM = new PlanVM
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

            return View(planVM);
        }

        public async Task<IActionResult> Manage()
        {
            var plans = await _planService.GetAllPlans();
            var planVMs = plans.Select(p => new PlanVM
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                PlanDescription = p.PlanDescription,
                Price = p.Price,
                BillingType = p.BillingType,
                IsActive = p.IsActive,
                PlanFeatures = [.. p.PlanFeatures.Select(f => new PlanFeatureVM
                {
                    FeatureName = f.FeatureName,
                    FeatureDescription = f.FeatureDescription
                })]
            }).ToList();

            return View(planVMs);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
    }
}
