using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    [Authorize (Roles ="Manager")]
    public class ManagerController : Controller
    {
        private readonly CounsellorRepository CounsellorRepository;
        private readonly PlanRepository PlanRepository;
        private readonly DiscountRepository DiscountRepository;


        /// <summary>
        /// Initializes a new instance of the <see cref="ManagerController"/> class.
        /// /// <param name="counsellorRepository">The repository for counsellor data.</param>
        /// <param name="planRepository">The repository for plan data.</param>
        /// <param name="discountRepository">The repository for discount data.</param>
        public ManagerController(CounsellorRepository counsellorRepository, PlanRepository planRepository, DiscountRepository discountRepository)
        {
            CounsellorRepository = counsellorRepository;
            PlanRepository = planRepository;
            DiscountRepository = discountRepository;
        }

        /// <summary>
        /// Displays a summary of all counsellor payment transactions for the manager dashboard.
        /// Aggregates transaction statistics and details for each counsellor.
        /// </summary>
        /// <returns>The dashboard view with aggregated data.</returns>
        public async Task<IActionResult> Index(string searchEmail, DateTime? startDate, DateTime? endDate)
        {
            var counsellors = await CounsellorRepository.GetCounsellorsWithPaymentsAsync();

            var dashboardData = counsellors
                .Select(c => GetManagerDashboardData(c))
                .ToList();

            // Filter by email
            if (!string.IsNullOrEmpty(searchEmail))
            {
                dashboardData = dashboardData
                    .Where(d => d.Email != null && d.Email.Contains(searchEmail, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            };
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
        private ManagerDashboardVM GetManagerDashboardData(Counsellor counsellor)
        {
            var payments = counsellor.Subscriptions?
                .Select(s => s.PaymentTransaction)
                .Where(p => p != null)
                .ToList() ?? [];

            var firstPayment = payments.FirstOrDefault();

            return new ManagerDashboardVM
            {
                CounsellorId = counsellor.CounsellorId,
                PractitionerLicenceId = counsellor.PractitionerLicenceId,
                CounsellorName = counsellor.DisplayName,
                Email = counsellor.User?.Email ?? "No email",
                Amount = payments.Sum(p => p?.Amount ?? 0),
                PaymentTransactionId = firstPayment?.PaymentTransactionId ?? 0,
                Currency = firstPayment?.Currency ?? "CAD",
                SOP = payments.Any(p => p?.Status == PaymentTransactionStatus.Failed)
                    ? "Failed"
                    : "Paid",
                PaidAt = firstPayment?.PaidAt, 
                RegistrationDate = counsellor.CreatedAt.ToString("yyyy-MM-dd"),
            };
        }

        /// <summary>
        /// Displays detailed payment transaction information for a specific counsellor.
        /// </summary>
        /// <param name="id">The payment transaction ID.</param>
        /// <returns>The details view for the specified transaction, or NotFound if not found.</returns>
        public async Task<IActionResult> TransactionDetails(int id)
        {
            var counsellors = await CounsellorRepository.GetCounsellorsWithPaymentsAsync();
            var detailsData = counsellors
                .Select(c => GetManagerDashboardData(c))
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
            var plans = await PlanRepository.GetAllAsync();
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
            var plan = await PlanRepository.GetById(id);

            if (plan == null)
            {
                return NotFound();
            }
            ViewBag.BillingTypes = new List<string> { "Free Trial", "Monthly", "Yearly" };

            var vm = new PlanVM
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                PlanDescription = plan.PlanDescription,
                Price = plan.Price,
                BillingType = plan.BillingType,
                IsActive = plan.IsActive
            };
            return View(vm);
        }

        /// <summary>
        /// Processes the submission of the plan edit form and updates the plan details asynchronously.
        /// /// <param name="vm">The view model containing updated plan information.</param>
        /// <returns>Redirects to the plans list if successful, otherwise redisplays the edit form.</returns>
        [HttpPost]
        public async Task<IActionResult> PlanEdit(PlanVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            var plan = await PlanRepository.GetById(vm.PlanId);

            if (plan == null)
            {
                return NotFound();
            }
            //map VM -> Model
            plan.PlanName = vm.PlanName;
            plan.PlanDescription = vm.PlanDescription;
            plan.Price = vm.Price;
            plan.BillingType = vm.BillingType;
            plan.IsActive = vm.IsActive;
            await PlanRepository.Update(plan);

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
                Discounts =  await DiscountRepository.GetAllDiscountsWithPlansAsync()
            };

            return View(vm);
        }

        /// <summary>
        /// Displays the form to create a new discount.
        /// </summary>
        /// <returns>The create discount view.</returns>
        [HttpGet]
        public IActionResult CreateDiscount()
        {
            var vm = new DiscountVM(){
                    StartDateTime = DateTime.Now,
                    EndDateTime = DateTime.Now.AddMonths(1)
            };
            return View(vm);
        }

        /// <summary>
        /// Processes the submission of the create discount form and adds a new discount.
        /// </summary>
        /// <param name="vm">The view model containing discount information.</param>
        /// <returns>Redirects to the discounts list if successful, otherwise redisplays the form.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateDiscount(DiscountVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            if (vm.EndDateTime <= vm.StartDateTime)
            {
                ModelState.AddModelError("End DateTime", "End date must be after start date.");
                return View(vm);
            }

            var discount = new Discount
            {
                DiscountCode = vm.DiscountCode,
                DiscountType = vm.DiscountType,
                Value = vm.Value,
                StartDateTime = vm.StartDateTime,
                EndDateTime = vm.EndDateTime,
                CreatedAt = DateTime.UtcNow
            };

            await DiscountRepository.AddAsync(discount);

            return RedirectToAction(nameof(Discounts));
        }

        /// <summary>
        /// Displays the form to apply a discount to one or more plans asynchronously.
        /// </summary>
        /// <returns>The apply discount view with available discounts and plans.</returns>
        [HttpGet]
        public async Task<IActionResult> ApplyDiscountAsync()
        {
            var discounts = await DiscountRepository.GetActiveDiscountAsync();
            var plans = await PlanRepository.GetAllAsync();
            
            var vm = new DiscountVM
            {
                
                Plans = plans,
                DiscountCodeOptions = discounts.Select(d => new SelectListItem
                {
                    Value = d.DiscountId.ToString(),
                    Text = d.DiscountCode
                }).ToList()
            };         
            return View("ApplyDiscount", vm);
        }

        /// <summary>
        /// Processes the submission of the apply discount form and associates the selected discount with the selected plans.
        /// </summary>
        /// <param name="vm">The view model containing selected plan IDs and discount ID.</param>
        /// <returns>Redirects to the discounts list if successful, otherwise redisplays the form.</returns>
        [HttpPost]
        public  async Task<IActionResult> ApplyDiscount(DiscountVM vm)
        {
            if (vm.PlanIds == null || vm.PlanIds.Count == 0)
                return RedirectToAction("ApplyDiscount");

            foreach (var planId in vm.PlanIds)
            {
               await DiscountRepository.AddDiscountToPlanAsync(planId, vm.DiscountId);
            }

            return RedirectToAction("Discounts");
        }

        //// <summary>
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
            var discount = await DiscountRepository.GetDiscountByIdAsync(id);


            if (discount == null)
            {
                return NotFound();
            }
            var now = DateTime.Now;

            bool isStarted = discount.StartDateTime <= now;
            bool isExpired = discount.EndDateTime < now;
            bool HasPlans = discount.PlanDiscounts.Count != 0;

            // Await the plans and then use Select
            var plans = (await PlanRepository.GetAllAsync())
                .Where(p => p.PlanName != "Free");

            var vm = new DiscountVM
            {
                DiscountId = discount.DiscountId,
                DiscountCode = discount.DiscountCode,
                DiscountType = discount.DiscountType,
                Value = discount.Value,
                StartDateTime = discount.StartDateTime,
                EndDateTime = discount.EndDateTime,
                IsStarted = isStarted,
                IsExpired = isExpired,
                HasPlans = HasPlans,
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
        public async Task<IActionResult> EditDiscount(DiscountVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            var discount = await DiscountRepository.GetDiscountByIdAsync(vm.DiscountId);

            if (discount == null)
            {
                return NotFound();
            }

            //update date of discount
            discount.StartDateTime = vm.StartDateTime;
            discount.EndDateTime = vm.EndDateTime;

            //clear existing plan associations and add new ones based on selected plan IDs
            discount.PlanDiscounts.Clear();
            if (vm.PlanIds != null)
            {
                foreach (var planId in vm.PlanIds)
                {
                    discount.PlanDiscounts.Add(new PlanDiscount
                    {
                        DiscountId = discount.DiscountId,
                        PlanId = planId
                    });
                }
            }
            await DiscountRepository.UpdateAsync(discount);

            return RedirectToAction(nameof(Discounts));
        }


        [HttpGet]
        public async Task<IActionResult> DeleteDiscount(int id)
        {
            var discount = await DiscountRepository.GetDiscountByIdAsync(id);

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
        public async Task<IActionResult> DeleteDiscountConfirmed(int id)
        {
            var deleted = await DiscountRepository.DeleteIfUnusedAsync(id);

            if (!deleted)
            {
                TempData["Error"] = "Discount cannot be deleted because it is applied to plans.";
            }

            return RedirectToAction(nameof(Discounts));
        }
    }
}