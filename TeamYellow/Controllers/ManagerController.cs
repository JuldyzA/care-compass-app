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


        public ManagerController(CounsellorRepository counsellorRepository, PlanRepository planRepository)
        {
            CounsellorRepository = counsellorRepository;
            PlanRepository = planRepository;

        }

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


        // private Counsellor MapToCounsellor(ManagerDashboardVM vm)
        // {
        //     return new Counsellor
        //     {
        //         CounsellorId = vm.CounsellorId,
        //         PractitionerLicenceId = vm.PractitionerLicenceId.ToString(),
        //         DisplayName = vm.CounsellorName,
        //   
        //     };
        // }

        public IActionResult Details(int id)
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


        public IActionResult Plans()
        {
            var plans = PlanRepository.GetAll().ToList();
            return View(plans);
        }

        public IActionResult Edit(int id)
        {
            var plan = PlanRepository.GetById(id);

            if (plan == null)
            {
                return NotFound();
            }

            return View(plan);
        }

        [HttpPost]
        public IActionResult Edit(Plan entity)
        {
            if (!ModelState.IsValid)
            {
                return View(entity);
            }

            var result = PlanRepository.Update(entity);

            if (string.IsNullOrEmpty(result))
            {
                return NotFound();
            }

            return RedirectToAction("Plans");
        }
    }
}
