using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TeamYellow.Repositories;
using TeamYellow.Models;
using TeamYellow.ViewModels;

namespace TeamYellow.Controllers
{
    [Authorize]
    public class ManagerController : Controller
    {
        private readonly CounsellorRepository CounsellorRepository;
        private readonly PlanRepository PlanRepository;
        private readonly DiscountRepository DiscountRepository;


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
        public IActionResult Index()
        {
            var counsellors = CounsellorRepository.GetCounsellorsWithPayments();
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
        public IActionResult TransactionDetails(int id)
        {
            var counsellors = CounsellorRepository.GetCounsellorsWithPayments();
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
        /// Displays a list of all available plans.
        /// </summary>
        /// <returns>The plans view with a list of plans.</returns>
        public IActionResult Plans()
        {
            var vm = new PlanVM
            {
                Plans = PlanRepository.GetAll()
            };

            return View(vm);
        }

        /// <summary>
        /// Displays the edit form for a specific plan, allowing the manager to modify plan details.
        /// </summary>
        public IActionResult PlanEdit(int id)
        {
            var plan = PlanRepository.GetById(id);

            if (plan == null)
            {
                return NotFound();
            }
            ViewBag.BillingTypes = new List<string> { "Free Trial","Monthly", "Yearly" };

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
        /// Processes the submission of the plan edit form and updates the plan details.
        /// </summary>
        /// <param name="vm">The view model containing updated plan information.</param>
        /// <returns>Redirects to the plans list if successful, otherwise redisplays the edit form.</returns>
        [HttpPost]
        public IActionResult PlanEdit(PlanVM vm)
        {

            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            var plan = PlanRepository.GetById(vm.PlanId);

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
            PlanRepository.Update(plan);

            return RedirectToAction(nameof(Plans));
        }

  
    }
}
