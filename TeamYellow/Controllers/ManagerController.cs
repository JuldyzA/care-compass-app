using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TeamYellow.Models;
using TeamYellow.Repositories;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    [Authorize]
    public class ManagerController : Controller
    {
        private readonly CounsellorRepository CounsellorRepository;
        private readonly PlanRepository PlanRepository;
        private readonly DiscountRepository DiscountRepository;


        /// <summary>
        /// Initializes a new instance of the <see cref="ManagerController"/> class.
        /// </summary>
        /// <param name="counsellorRepository">The repository for counsellor data.</param>
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
        public async Task<IActionResult> Index()
        {
            var counsellors = await CounsellorRepository.GetAllAsync();

            var dashboardData = counsellors
                .Select(c => GetManagerDashboardData(c))
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
                .ToList() ?? new List<PaymentTransaction?>();

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
                PaidAt = firstPayment?.PaidAt.ToString("yyyy-MM-dd"),
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
            var plans = await Task.Run(() => PlanRepository.GetAll());
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
            var plan = await Task.Run(() => PlanRepository.GetById(id));

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
        /// </summary>
        /// <param name="vm">The view model containing updated plan information.</param>
        /// <returns>Redirects to the plans list if successful, otherwise redisplays the edit form.</returns>
        [HttpPost]
        public async Task<IActionResult> PlanEdit(PlanVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            var plan = await Task.Run(() => PlanRepository.GetById(vm.PlanId));

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
            await Task.Run(() => PlanRepository.Update(plan));

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
                Discounts = await Task.Run(() => DiscountRepository.GetAllDiscountsWithPlans())
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
            var vm = new DiscountVM();
            return View(vm);
        }

        /// <summary>
        /// Processes the submission of the create discount form and adds a new discount.
        /// </summary>
        /// <param name="vm">The view model containing discount information.</param>
        /// <returns>Redirects to the discounts list if successful, otherwise redisplays the form.</returns>
        [HttpPost]
        public IActionResult CreateDiscount(DiscountVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var discount = new Discount
            {
                DiscountCode = vm.DiscountCode,
                DiscountType = vm.DiscountType,
                Value = vm.Value,
                StartDateTime = vm.StartDateTime,
                EndDateTime = vm.EndDateTime,
                CreatedAt = DateTime.UtcNow
            };

            DiscountRepository.Add(discount);

            return RedirectToAction("Discounts");
        }

        /// <summary>
        /// Displays the form to apply a discount to one or more plans asynchronously.
        /// </summary>
        /// <returns>The apply discount view with available discounts and plans.</returns>
        [HttpGet]
        public async Task<IActionResult> ApplyDiscountAsync()
        {
            var discounts = await Task.Run(() => DiscountRepository.GetAll());
            var plans = await PlanRepository.GetAll();

            var vm = new DiscountVM
            {
                Discounts = discounts,
                Plans = plans
            };

            return View("ApplyDiscount", vm);
        }

        /// <summary>
        /// Processes the submission of the apply discount form and associates the selected discount with the selected plans.
        /// </summary>
        /// <param name="vm">The view model containing selected plan IDs and discount ID.</param>
        /// <returns>Redirects to the discounts list if successful, otherwise redisplays the form.</returns>
        [HttpPost]
        public IActionResult ApplyDiscount(DiscountVM vm)
        {
            if (vm.PlanIds == null || !vm.PlanIds.Any())
                return RedirectToAction("ApplyDiscount");

            foreach (var planId in vm.PlanIds)
            {
                DiscountRepository.AddDiscountToPlan(planId, vm.DiscountId);
            }

            return RedirectToAction("Discounts");
        }
    }
}
