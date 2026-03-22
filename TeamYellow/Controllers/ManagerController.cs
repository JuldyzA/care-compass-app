using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    [Authorize (Roles ="Manager")]
    public class ManagerController : Controller
    {
        private readonly CounsellorRepository _counsellorRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IPlanService _planService;
        private readonly DiscountRepository _discountRepository;


        /// <summary>
        /// Initializes a new instance of the <see cref="ManagerController"/> class.
        /// /// <param name="counsellorRepository">The repository for counsellor data.</param>
        /// <param name="planRepository">The repository for plan data.</param>
        /// <param name="discountRepository">The repository for discount data.</param>
        public ManagerController(CounsellorRepository counsellorRepository, IPlanRepository planRepository, IPlanService planService, DiscountRepository discountRepository)
        {
            _counsellorRepository = counsellorRepository;
            _planRepository = planRepository;
            _planService = planService;
            _discountRepository = discountRepository;
        }

        /// <summary>
        /// Displays a summary of all counsellor payment transactions for the manager dashboard.
        /// Aggregates transaction statistics and details for each counsellor.
        /// </summary>
        /// <returns>The dashboard view with aggregated data.</returns>
        public async Task<IActionResult> Index(string? searchEmail, DateTime? startDate, DateTime? endDate)
        {
            var counsellors = await _counsellorRepository.GetCounsellorsWithPaymentsAsync();

            var dashboardData = counsellors
                .SelectMany(c => GetManagerDashboardData(c))
                .OrderByDescending(x => x.PaidAt)
                .ToList();

            // Filter by email
            if (!string.IsNullOrEmpty(searchEmail))
            {
                dashboardData = dashboardData
                    .Where(d => d.Email != null && d.Email.Contains(searchEmail, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            // Filter by start date
            if (startDate.HasValue)
            {
                dashboardData = dashboardData
                    .Where(x => x.PaidAt.HasValue && x.PaidAt.Value >= startDate.Value)
                    .ToList();
            }
            // Filter by end date
            if (endDate.HasValue)
            {
                dashboardData = dashboardData
                    .Where(x => x.PaidAt.HasValue && x.PaidAt.Value <= endDate.Value)
                    .ToList();
            }

            var stats = new DashboardStatsVM
            {
                TotalTransactions = dashboardData.Count,
                TotalRevenue = dashboardData.Sum(x => x.Amount),
                FailedPayments = dashboardData.Count(x => x.SOP == "Failed"),
                SuccessfulPayments = dashboardData.Count(x => x.SOP == "Paid")
            };
            var pageVM = new ManagerDashboardPageVM
            {
                Stats = stats,
                Counsellors = dashboardData
            };

            return View(pageVM);
        }

        /// <summary>
        /// Aggregates payment transaction data for a given counsellor to be displayed on the manager dashboard.
        /// </summary>
        /// <param name="counsellor">The counsellor whose data is being aggregated.</param>
        /// <returns>A view model containing dashboard data for the counsellor.</returns>
        private List<ManagerDashboardVM> GetManagerDashboardData(Counsellor counsellor)
        {
            return counsellor.Subscriptions?
                .Where(s => s.PaymentTransaction != null)
                .Select(s => new ManagerDashboardVM
                {
                    CounsellorId = counsellor.CounsellorId,
                    PractitionerLicenceId = counsellor.PractitionerLicenceId,
                    CounsellorName = counsellor.DisplayName,
                    Email = counsellor.User?.Email ?? "No email",
                    Amount = s.PaymentTransaction?.Amount ?? 0,
                    PaymentTransactionId = s.PaymentTransaction?.PaymentTransactionId ?? 0,
                    Currency = s.PaymentTransaction?.Currency ?? "CAD",
                    SOP = s.PaymentTransaction?.Status == PaymentTransactionStatus.Failed ? "Failed" : "Paid",
                    PaidAt = s.PaymentTransaction?.PaidAt,
                    RegistrationDate = counsellor.CreatedAt.ToString("yyyy-MM-dd"),
                    BillingType = s.Plan?.BillingType ?? "N/A"
                })
                .OrderByDescending(x => x.PaidAt)
                .ToList() ?? [];
        }

        /// <summary>
        /// Displays detailed payment transaction information for a specific counsellor.
        /// </summary>
        /// <param name="id">The payment transaction ID.</param>
        /// <returns>The details view for the specified transaction, or NotFound if not found.</returns>
        public async Task<IActionResult> TransactionDetails(int id)
        {
            var counsellors = await _counsellorRepository.GetCounsellorsWithPaymentsAsync();
            var detailsData = counsellors
                .SelectMany(c => GetManagerDashboardData(c))
                .FirstOrDefault(d => d.PaymentTransactionId == id);

            if (detailsData == null)
            {
                return NotFound();
            }
            return View(detailsData);
        }

        /// <summary>
        /// Displays a list of all available plans asynchronously.
        /// </summary>
        /// <returns>The plans view with a list of plans.</returns>
        public async Task<IActionResult> Plans()
        {
            var plans = await _planRepository.GetAllAsync();
            var vm = new PlanVM
            {
                Plans = plans
            };

            return View(vm);
        }

        /// <summary>
        /// Displays the edit form for a specific plan, allowing the manager to modify plan details.
        /// </summary>
        /// <param name="id">The unique identifier of the plan to edit.</param>
        /// <returns>The edit view for the specified plan, or NotFound if not found.</returns>
        public async Task<IActionResult> PlanEdit(int id)
        {
            var plan = await _planRepository.GetByIdWithFeaturesAsync(id);

            if (plan == null)
            {
                return NotFound();
            }

            var vm = new PlanVM
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                PlanDescription = plan.PlanDescription,
                Price = plan.Price,
                BillingType = plan.BillingType,
                IsActive = plan.IsActive,
                PlanFeatures = plan.PlanFeatures
                    .OrderBy(f => f.SortOrder)
                    .Select(f => new PlanFeatureVM
                    {
                        FeatureName = f.FeatureName,
                        FeatureDescription = f.FeatureDescription
                    })
                    .ToList()
            };
            return View(vm);
        }

        /// <summary>
        /// Processes the submission of the plan edit form and updates the plan details asynchronously.
        /// <param name="vm">The view model containing updated plan information.</param>
        /// <returns>Redirects to the plans list if successful, otherwise redisplays the edit form.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlanEdit(PlanVM vm)
        {
            var plan = await _planRepository.GetByIdWithFeaturesAsync(vm.PlanId);
            if (plan == null)
            {
                return NotFound();
            }

            var currentBillingType = plan.BillingType;

            if (string.Equals(currentBillingType, "Free", StringComparison.OrdinalIgnoreCase))
            {
                if (vm.Price != 0m)
                {
                    ModelState.AddModelError(nameof(vm.Price), "Free plan must have a price of 0.00.");
                }
            }
            else
            {
                if (vm.Price <= 0m)
                {
                    ModelState.AddModelError(nameof(vm.Price), "Monthly and Yearly plans must have a price greater than 0.00.");
                }
            }

            if (vm.PlanFeatures != null)
            {
                for (int i = 0; i < vm.PlanFeatures.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(vm.PlanFeatures[i].FeatureName))
                    {
                        ModelState.AddModelError($"PlanFeatures[{i}].FeatureName", "Feature name is required.");
                    }
                    if (string.IsNullOrWhiteSpace(vm.PlanFeatures[i].FeatureDescription))
                    {
                        ModelState.AddModelError($"PlanFeatures[{i}].FeatureDescription", "Feature description is required.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                vm.BillingType = currentBillingType;
                return View(vm);
            }

            vm.BillingType = currentBillingType;

            var success = await _planService.UpdatePlansWithFeaturesAsync(vm);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "An error occurred while updating the plan. Please try again.");
                vm.BillingType = currentBillingType;
                return View(vm);
            }

            return RedirectToAction(nameof(Plans));
        }

        /// <summary>
        /// Displays a list of all available discounts with their associated plans.
        /// </summary>
        /// <returns>The discounts view with a list of discounts and plans.</returns>
        public async Task<IActionResult> Discounts()
        {
            var vm = new DiscountVM
            {
                Discounts =  await _discountRepository.GetAllDiscountsWithPlansAsync()
            };

            return View(vm);
        }

        /// <summary>
        /// Displays the form to create a new discount.
        /// </summary>
        /// <returns>The create discount view.</returns>
        [HttpGet]
        public async Task<IActionResult> CreateDiscount()
        {
            var plans = (await _planRepository.GetAllAsync())
                .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var vm = new DiscountVM
            {
                StartDateTime = DateTime.Now.AddMinutes(30),
                EndDateTime = DateTime.Now.AddDays(7),
                AvailablePlans = plans.Select(p => new SelectListItem
                {
                    Value = p.PlanId.ToString(),
                    Text = p.PlanName
                }).ToList()
            };

            return View(vm);
        }

        /// <summary>
        /// Processes the submission of the create discount form and adds a new discount.
        /// </summary>
        /// <param name="vm">The view model containing discount information.</param>
        /// <returns>Redirects to the discounts list if successful, otherwise redisplays the form.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDiscount(DiscountVM vm)
        {
            var selectedPlanIds = (vm.PlanIds ?? new List<int>())
                .Distinct()
                .ToList();

            vm.DiscountCode = (vm.DiscountCode ?? string.Empty).Trim().ToUpperInvariant();

            if (await _discountRepository.DiscountCodeExistsAsync(vm.DiscountCode))
            {
                ModelState.AddModelError(nameof(vm.DiscountCode), "This discount code already exists.");
            }

            if (selectedPlanIds.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.PlanIds), "Please select at least one plan.");
            }
            else
            {
                var allowedPlanIds = (await _planRepository.GetAllAsync())
                    .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.PlanId)
                    .ToHashSet();

                var invalidPlanIds = selectedPlanIds
                    .Where(id => !allowedPlanIds.Contains(id))
                    .ToList();

                if (invalidPlanIds.Count > 0)
                {
                    ModelState.AddModelError(nameof(vm.PlanIds), "One or more selected plans are invalid.");
                }
            }

            if (!ModelState.IsValid)
            {
                var plans = (await _planRepository.GetAllAsync())
                    .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                vm.AvailablePlans = plans.Select(p => new SelectListItem
                {
                    Value = p.PlanId.ToString(),
                    Text = p.PlanName,
                    Selected = selectedPlanIds.Contains(p.PlanId)
                }).ToList();

                return View(vm);
            }

            var startUtc = DateTime.SpecifyKind(vm.StartDateTime, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(vm.EndDateTime, DateTimeKind.Local).ToUniversalTime();

            var discount = new Discount
            {
                DiscountCode = vm.DiscountCode,
                DiscountType = vm.DiscountType,
                Value = vm.Value!.Value,
                StartDateTime = startUtc,
                EndDateTime = endUtc,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _discountRepository.AddAsync(discount);
                foreach (var planId in selectedPlanIds)
                {
                    await _discountRepository.AddDiscountToPlanAsync(planId, discount.DiscountId);
                }
                TempData["Success"] = "Discount created and applied to selected plans.";
                return RedirectToAction(nameof(Discounts));
            }
            catch
            {
                var plans = (await _planRepository.GetAllAsync())
                    .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                vm.AvailablePlans = plans.Select(p => new SelectListItem
                {
                    Value = p.PlanId.ToString(),
                    Text = p.PlanName,
                    Selected = selectedPlanIds.Contains(p.PlanId)
                }).ToList();

                ModelState.AddModelError(string.Empty, "Unable to create discount. Please try again.");
                return View(vm);
            }
        }

        /// <summary>
        /// Displays the edit form for a specific discount.
        /// Retrieves the discount by its ID, determines its current status
        /// (started or expired), and prepares the view model with the discount
        /// details and available plans for selection.
        /// </summary>
        /// <param name="id">The unique identifier of the discount to edit.</param>
        /// <returns>
        /// The edit discount view populated with the existing discount data and
        /// associated plan selections, or <see cref="NotFound"/> if the discount
        /// does not exist.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> EditDiscount(int id)
        {
            var discount = await _discountRepository.GetDiscountByIdAsync(id);


            if (discount == null)
            {
                return NotFound();
            }
            var nowUtc = DateTime.UtcNow;

            bool isStarted = discount.StartDateTime <= nowUtc;
            bool isExpired = discount.EndDateTime < nowUtc;
            bool HasPlans = discount.PlanDiscounts.Count != 0;

            // Await the plans and then use Select
            var plans = (await _planRepository.GetAllAsync())
                .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase));

            var vm = new DiscountVM
            {
                DiscountId = discount.DiscountId,
                DiscountCode = discount.DiscountCode,
                DiscountType = discount.DiscountType,
                Value = discount.Value,
                StartDateTime = DateTime.SpecifyKind(discount.StartDateTime, DateTimeKind.Utc).ToLocalTime(),
                EndDateTime = DateTime.SpecifyKind(discount.EndDateTime, DateTimeKind.Utc).ToLocalTime(),
                IsStarted = isStarted,
                IsExpired = isExpired,
                HasPlans = HasPlans,
                AvailablePlans = plans.Select(p => new SelectListItem
                {
                    Value = p.PlanId.ToString(),
                    Text = p.PlanName,
                    Selected = discount.PlanDiscounts.Any(pd => pd.Plan.PlanId == p.PlanId)
                }).ToList()
            };

            return View(vm);
        }

        /// <summary>
        /// Processes the submission of the edit discount form and updates the selected discount.
        /// Updates the start and end dates of the discount and modifies the associated plans
        /// based on the selected plan IDs from the form.
        /// </summary>
        /// <param name="vm">
        /// The view model containing the updated discount data, including the selected plan IDs.
        /// </param>
        /// <returns>
        /// Redirects to the Discounts page if the update is successful; otherwise redisplays
        /// the edit form with validation errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDiscount(DiscountVM vm)
        {
            var discount = await _discountRepository.GetDiscountByIdAsync(vm.DiscountId);

            if (discount == null)
            {
                return NotFound();
            }

            vm.DiscountCode = (vm.DiscountCode ?? string.Empty).Trim().ToUpperInvariant();

            if (await _discountRepository.DiscountCodeExistsAsync(vm.DiscountCode, vm.DiscountId))
            {
                ModelState.AddModelError(nameof(vm.DiscountCode), "This discount code already exists.");
            }

            var nowUtc = DateTime.UtcNow;
            var isCurrentlyActive = discount.StartDateTime <= nowUtc && discount.EndDateTime >= nowUtc;

            var selectedPlanIds = (vm.PlanIds ?? new List<int>())
                .Distinct()
                .ToList();

            if (!isCurrentlyActive)
            {
                if (selectedPlanIds.Count == 0)
                {
                    ModelState.AddModelError(nameof(vm.PlanIds), "Please select at least one plan.");
                }
                else
                {
                    var allowedPlanIds = (await _planRepository.GetAllAsync())
                        .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase))
                        .Select(p => p.PlanId)
                        .ToHashSet();

                    var invalidPlanIds = selectedPlanIds
                        .Where(id => !allowedPlanIds.Contains(id))
                        .ToList();

                    if (invalidPlanIds.Count > 0)
                    {
                        ModelState.AddModelError(nameof(vm.PlanIds), "One or more selected plans are invalid.");
                    }
                }
            }

            var vmStartUtc = DateTime.SpecifyKind(vm.StartDateTime, DateTimeKind.Local).ToUniversalTime();
            var vmEndUtc = DateTime.SpecifyKind(vm.EndDateTime, DateTimeKind.Local).ToUniversalTime();

            var effectiveStart = isCurrentlyActive
                ? discount.StartDateTime
                : vmStartUtc;

            if (vmEndUtc <= effectiveStart)
            {
                ModelState.AddModelError(nameof(vm.EndDateTime), "End date must be after start date.");
            }

            if (!ModelState.IsValid)
            {
                var plans = (await _planRepository.GetAllAsync())
                    .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                vm.IsStarted = discount.StartDateTime <= nowUtc;
                vm.IsExpired = discount.EndDateTime < nowUtc;
                vm.HasPlans = discount.PlanDiscounts.Any();
                vm.AvailablePlans = plans.Select(p => new SelectListItem
                {
                    Value = p.PlanId.ToString(),
                    Text = p.PlanName,
                    Selected = !isCurrentlyActive
                        ? selectedPlanIds.Contains(p.PlanId)
                        : discount.PlanDiscounts.Any(pd => pd.PlanId == p.PlanId)
                }).ToList();

                return View(vm);
            }

            if (discount.EndDateTime < nowUtc)
            {
                discount.StartDateTime = vmStartUtc;
                discount.EndDateTime = vmEndUtc;
            }
            else if (discount.StartDateTime <= nowUtc)
            {
                discount.EndDateTime = vmEndUtc;
            }
            else
            {
                discount.StartDateTime = vmStartUtc;
                discount.EndDateTime = vmEndUtc;
            }

            var isActive = discount.StartDateTime <= nowUtc && discount.EndDateTime >= nowUtc;

            if (!isActive)
            {
                discount.PlanDiscounts.Clear();

                foreach (var planId in selectedPlanIds)
                {
                    discount.PlanDiscounts.Add(new PlanDiscount
                    {
                        PlanId = planId,
                        DiscountId = discount.DiscountId
                    });
                }
            }

            try
            {
                await _discountRepository.UpdateAsync(discount);
                TempData["Success"] = "Discount updated successfully.";
                return RedirectToAction(nameof(Discounts));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Unable to update discount. Please try again.");

                var plans = (await _planRepository.GetAllAsync())
                    .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                vm.IsStarted = discount.StartDateTime <= nowUtc;
                vm.IsExpired = discount.EndDateTime < nowUtc;
                vm.HasPlans = discount.PlanDiscounts.Any();
                vm.AvailablePlans = plans.Select(p => new SelectListItem
                {
                    Value = p.PlanId.ToString(),
                    Text = p.PlanName,
                    Selected = !isCurrentlyActive
                        ? selectedPlanIds.Contains(p.PlanId)
                        : discount.PlanDiscounts.Any(pd => pd.PlanId == p.PlanId)
                }).ToList();

                return View(vm);
            }
        }


        [HttpGet]
        public async Task<IActionResult> DeleteDiscount(int id)
        {
            var discount = await _discountRepository.GetDiscountByIdAsync(id);

            if (discount == null)
                return NotFound();

            var vm = new DiscountVM
            {
                DiscountId = discount.DiscountId,
                DiscountCode = discount.DiscountCode,
                Value = discount.Value,
                StartDateTime = discount.StartDateTime,
                EndDateTime = discount.EndDateTime,
                HasPlans = discount.PlanDiscounts.Any()
            };

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDiscountConfirmed(int id)
        {
            var deleted = await _discountRepository.DeleteIfUnusedAsync(id);

            if (!deleted)
            {
                TempData["Error"] = "Discount cannot be deleted because it is applied to plans.";
            }

            return RedirectToAction(nameof(Discounts));
        }
    }
}