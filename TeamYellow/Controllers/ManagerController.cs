using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.Services;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    /// <summary>
    /// Handles manager-only workflows for dashboard reporting, plan management,
    /// and discount management.
    /// </summary>
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly CounsellorRepository _counsellorRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IPlanService _planService;
        private readonly DiscountRepository _discountRepository;
        private readonly UserProfileRepository _userProfileRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ManagerController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagerController"/> class.
        /// </summary>
        /// <param name="counsellorRepository">Provides access to counsellor and payment-related data.</param>
        /// <param name="planRepository">Provides access to plan lookup operations.</param>
        /// <param name="planService">Provides plan update operations and related business logic.</param>
        /// <param name="discountRepository">Provides access to discount lookup and management operations.</param>
        /// <param name="userProfileRepository">Provides access to user profile details such as display name and avatar.</param>
        /// <param name="userManager">Provides access to the current Identity user.</param>
        public ManagerController(
            CounsellorRepository counsellorRepository,
            IPlanRepository planRepository,
            IPlanService planService,
            DiscountRepository discountRepository,
            UserProfileRepository userProfileRepository,
            UserManager<IdentityUser> userManager,
            ILogger<ManagerController> logger)
        {
            _counsellorRepository = counsellorRepository;
            _planRepository = planRepository;
            _planService = planService;
            _discountRepository = discountRepository;
            _userProfileRepository = userProfileRepository;
            _userManager = userManager;

            _logger = logger;
        }

        /// <summary>
        /// Populates manager profile display data used by the shared dashboard layout.
        /// </summary>
 

        /// <summary>
        public override async Task OnActionExecutionAsync(
    ActionExecutingContext context,
    ActionExecutionDelegate next)
{
    var user = await _userManager.GetUserAsync(User);

    if (user != null)
    {
        var (profile, counsellorDisplayName) =
            await _userProfileRepository.GetByUserIdAsync(user.Id);

        if (!string.IsNullOrWhiteSpace(profile?.ProfilePhotoUrl))
        {
            ViewData["UserProfilePicture"] =
                profile.ProfilePhotoUrl;
        }

        var displayName =
            !string.IsNullOrWhiteSpace(counsellorDisplayName)
                ? counsellorDisplayName.Trim()
                : string.Join(
                    " ",
                    new[]
                    {
                        profile?.FirstName,
                        profile?.LastName
                    }
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)))
                .Trim();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName =
                await _userManager.GetUserNameAsync(user)
                ?? user.Email;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            ViewData["DisplayName"] = displayName;
        }
    }

    await next();
}
        /// Displays the manager dashboard with counsellor payment transactions and summary statistics.
        /// </summary>
        /// <param name="searchEmail">An optional email filter for transaction records.</param>
        /// <param name="startDate">An optional inclusive start date filter.</param>
        /// <param name="endDate">An optional inclusive end date filter.</param>
        /// <returns>The dashboard view populated with filtered transaction data and summary statistics.</returns>
        public async Task<IActionResult> Index(string? searchEmail, DateTime? startDate, DateTime? endDate)
        {
            var counsellors = await _counsellorRepository.GetCounsellorsWithPaymentsAsync();

            IEnumerable<ManagerDashboardVM> dashboardQuery = counsellors
                .SelectMany(GetManagerDashboardData);

            // Filter by email
            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                dashboardQuery = dashboardQuery
                    .Where(d => d.Email != null && d.Email.Contains(searchEmail, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by start date
            if (startDate.HasValue)
            {
                dashboardQuery = dashboardQuery
                    .Where(x => x.PaidAt.HasValue && x.PaidAt.Value >= startDate.Value);
            }
            // Filter by end date
            if (endDate.HasValue)
            {
                var endExclusive = endDate.Value.Date.AddDays(1);
                dashboardQuery = dashboardQuery
                    .Where(x => x.PaidAt.HasValue && x.PaidAt.Value < endExclusive);
            }

            var dashboardData = dashboardQuery
                .OrderByDescending(x => x.PaidAt)
                .ToList();

            var currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var monthlyRevenueBuckets = Enumerable
                .Range(0, 12)
                .Select(offset =>
                {
                    var monthStart = currentMonthStart.AddMonths(offset - 11);
                    return new
                    {
                        MonthStart = monthStart,
                        Label = monthStart.ToString("MMM yyyy")
                    };
                })
                .ToList();

            var monthlyRevenueMap = dashboardData
                .Where(x => string.Equals(x.SOP, "Paid", StringComparison.OrdinalIgnoreCase) && x.PaidAt.HasValue)
                .GroupBy(x => new DateTime(x.PaidAt!.Value.Year, x.PaidAt.Value.Month, 1))
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));

            var monthlyRevenueLabels = monthlyRevenueBuckets
                .Select(x => x.Label)
                .ToList();

            var monthlyRevenueSeries = monthlyRevenueBuckets
                .Select(x => monthlyRevenueMap.TryGetValue(x.MonthStart, out var value) ? value : 0m)
                .ToList();

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
                Counsellors = dashboardData,
                MonthlyRevenueLabels = monthlyRevenueLabels,
                MonthlyRevenueSeries = monthlyRevenueSeries
            };

            return View(pageVM);
        }

        /// <summary>
        /// Projects a counsellor's subscription payment data into dashboard rows for display.
        /// </summary>
        /// <param name="counsellor">The counsellor whose payment transaction data is being projected.</param>
        /// <returns>A sequence of dashboard rows for the counsellor's payment transactions.</returns>
        private IEnumerable<ManagerDashboardVM> GetManagerDashboardData(Counsellor counsellor)
        {
            return (counsellor.Subscriptions ?? Enumerable.Empty<Subscription>())
                .Where(s => s.PaymentTransaction != null)
                .Select(s =>
                {
                    var paymentTransaction = s.PaymentTransaction!;
                    
                    return new ManagerDashboardVM
                    {
                        CounsellorId = counsellor.CounsellorId,
                        PractitionerLicenceId = counsellor.PractitionerLicenceId,
                        CounsellorName = counsellor.DisplayName,
                        Email = !string.IsNullOrWhiteSpace(counsellor.User?.Email) ? 
                                counsellor.User.Email : !string.IsNullOrWhiteSpace(counsellor.ArchivedEmailDisplay) ?
                                $"{counsellor.ArchivedEmailDisplay} (deleted)" : "(deleted account)",
                        Amount = paymentTransaction.Amount,
                        PaymentTransactionId = paymentTransaction.PaymentTransactionId,
                        Currency = paymentTransaction.Currency,
                        SOP = paymentTransaction.Status == PaymentTransactionStatus.Failed ? "Failed" : "Paid",
                        PaidAt = paymentTransaction.PaidAt,
                        RegistrationDate = counsellor.CreatedAt.ToString("yyyy-MM-dd"),
                        BillingType = s.Plan?.BillingType ?? "N/A"
                    };
                });
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
                .SelectMany(GetManagerDashboardData)
                .FirstOrDefault(d => d.PaymentTransactionId == id);

            if (detailsData == null)
            {
                return NotFound();
            }
            return View(detailsData);
        }

        /// <summary>
        /// Displays all available subscription plans.
        /// </summary>
        /// <returns>The plans view populated with the current list of plans.</returns>
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
        /// Displays the edit form for a specific subscription plan.
        /// </summary>
        /// <param name="id">The unique identifier of the plan to edit.</param>
        /// <returns>
        /// The edit view for the specified plan, or a not found result if the plan does not exist.
        /// </returns>
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
                        PlanFeatureId = f.PlanFeatureId,
                        FeatureName = f.FeatureName,
                        FeatureDescription = f.FeatureDescription
                    })
                    .ToList()
            };
            return View(vm);
        }

        /// <summary>
        /// Processes the submitted plan edit form and updates the selected plan.
        /// </summary>
        /// <param name="vm">The view model containing the updated plan details and features.</param>
        /// <returns>
        /// Redirects to the plans page if the update succeeds; otherwise redisplays the edit form with validation errors.
        /// </returns>
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
        /// Displays all available discounts and their associated plans.
        /// </summary>
        /// <returns>The discounts view populated with existing discount records.</returns>
        public async Task<IActionResult> Discounts()
        {
            var vm = new DiscountVM
            {
                Discounts =  await _discountRepository.GetAllDiscountsWithPlansAsync()
            };

            return View(vm);
        }

        /// <summary>
        /// Displays the form for creating a new discount.
        /// </summary>
        /// <returns>The create discount view populated with eligible plans.</returns>
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
        /// Processes the submitted create discount form and creates a new discount linked to the selected plans.
        /// </summary>
        /// <param name="vm">The view model containing the new discount details.</param>
        /// <returns>
        /// Redirects to the discounts page if creation succeeds; otherwise redisplays the form with validation errors.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDiscount(DiscountVM vm)
        {
            var selectedPlanIds = (vm.PlanIds ?? new List<int>())
                .Distinct()
                .ToList();

            vm.DiscountCode = (vm.DiscountCode ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (await _discountRepository.DiscountCodeExistsAsync(vm.DiscountCode))
            {
                ModelState.AddModelError(nameof(vm.DiscountCode), "This discount code already exists.");
            }

            var plans = (await _planRepository.GetAllAsync())
                .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (selectedPlanIds.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.PlanIds), "Please select at least one plan.");
            }
            else
            {
                var allowedPlanIds = plans
                    .Select(p => p.PlanId)
                    .ToHashSet();

                var invalidPlanIds = selectedPlanIds
                    .Where(planId => !allowedPlanIds.Contains(planId))
                    .ToList();

                if (invalidPlanIds.Count > 0)
                {
                    ModelState.AddModelError(nameof(vm.PlanIds), "One or more selected plans are invalid.");
                }
            }

            if (!ModelState.IsValid)
            {
                vm.AvailablePlans = plans.Select(p => new SelectListItem
                {
                    Value = p.PlanId.ToString(),
                    Text = p.PlanName,
                    Selected = selectedPlanIds.Contains(p.PlanId)
                }).ToList();

                return View(vm);
            }

            var nowUtc = DateTime.UtcNow;
            var startUtc = DateTime.SpecifyKind(vm.StartDateTime, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(vm.EndDateTime, DateTimeKind.Local).ToUniversalTime();

            if (startUtc < nowUtc)
            {
                ModelState.AddModelError(nameof(vm.StartDateTime), "Start date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                vm.AvailablePlans = plans.Select(p => new SelectListItem
                {
                    Value = p.PlanId.ToString(),
                    Text = p.PlanName,
                    Selected = selectedPlanIds.Contains(p.PlanId)
                }).ToList();

                return View(vm);
            }

            var discount = new Discount
            {
                DiscountCode = vm.DiscountCode,
                DiscountType = vm.DiscountType!.Value,
                Value = vm.Value.GetValueOrDefault(),
                StartDateTime = startUtc,
                EndDateTime = endUtc,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _discountRepository.CreateDiscountWithPlansAsync(discount, selectedPlanIds);

                TempData["Success"] = "Discount created and applied to selected plans.";
                return RedirectToAction(nameof(Discounts));
            }
            catch
            {
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
        /// Displays the edit form for a specific discount and prepares its associated plan selections.
        /// </summary>
        /// <param name="id">The unique identifier of the discount to edit.</param>
        /// <returns>
        /// The edit discount view populated with the existing discount data, or a not found result if the discount does not exist.
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
            bool hasPlans = discount.PlanDiscounts.Count != 0;

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
                HasPlans = hasPlans,
                AvailablePlans = plans.Select(p => new SelectListItem
                {
                    Value = p.PlanId.ToString(),
                    Text = p.PlanName,
                    Selected = discount.PlanDiscounts.Any(pd => pd.PlanId == p.PlanId)
                }).ToList()
            };

            return View(vm);
        }

        /// <summary>
        /// Processes the submitted edit discount form and updates the selected discount and its associated plans.
        /// </summary>
        /// <param name="vm">The view model containing the updated discount data.</param>
        /// <returns>
        /// Redirects to the discounts page if the update succeeds; otherwise redisplays the edit form with validation errors.
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

            var plans = (await _planRepository.GetAllAsync())
                .Where(p => !string.Equals(p.BillingType, "Free", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!isCurrentlyActive)
            {
                if (selectedPlanIds.Count == 0)
                {
                    ModelState.AddModelError(nameof(vm.PlanIds), "Please select at least one plan.");
                }
                else
                {
                    var allowedPlanIds = plans
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

            if (!ModelState.IsValid)
            {
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

            if (!isCurrentlyActive)
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

        /// <summary>
        /// Displays a confirmation view before deleting a discount.
        /// </summary>
        /// <param name="id">The unique identifier of the discount to delete.</param>
        /// <returns>
        /// The delete confirmation view for the specified discount, or a not found result if the discount does not exist.
        /// </returns>
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

        /// <summary>
        /// Deletes the specified discount if it is not currently associated with any plans.
        /// </summary>
        /// <param name="id">The unique identifier of the discount to delete.</param>
        /// <returns>
        /// Redirects to the discounts page after the delete attempt completes.
        /// </returns>
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