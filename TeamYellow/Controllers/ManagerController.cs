using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TeamYellow.Repositories;
using TeamYellow.Models;
using TeamYellow.ViewModels;
using Microsoft.EntityFrameworkCore;

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
            
            return View(dashboardData);
        }


        private ManagerDashboardVM GetManagerDashboardData(Counsellor counsellor)
        {
            var payments = counsellor.Subscriptions
                .Select(s => s.PaymentTransaction)
                .Where(p => p != null)
                .ToList() ?? new List<PaymentTransaction>();

            var firstPayment = payments.FirstOrDefault();
            var stats = new DashboardStatsVM
            {
                TotalTransactions = payments.Count,
                TotalRevenue = payments.Sum(p => p?.Amount ?? 0),
                FailedPayments = payments.Count(p => p.Status == PaymentTransactionStatus.Failed),
                SuccessfulPayments = payments.Count(p => p.Status == PaymentTransactionStatus.Captured),
                ActiveSubscriptions = counsellor.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active)
            };

            return new ManagerDashboardVM
            {
                CounsellorId = counsellor.CounsellorId,
                PractitionerLicenceId = counsellor.PractitionerLicenceId,
                CounsellorName = counsellor.DisplayName,
                Email = counsellor.User?.Email ?? "No email",
                Amount = payments.Sum(p => p?.Amount ?? 0),
                PaymentTransactionId = firstPayment?.PaymentTransactionId ?? 0,
                Currency = firstPayment?.Currency ?? "CAD",
                SOP = payments?.Any(p => p.Status == PaymentTransactionStatus.Failed) ?? false
                    ? "Failed"
                    : "Paid",
                PaidAt = firstPayment?.PaidAt.ToString("yyyy-MM-dd"),
                RegistrationDate = counsellor.CreatedAt.ToString("yyyy-MM-dd"),
                Stats = stats
            };
        }

        private static ManagerPlanDiscountVM GetManagerPlanDiscount(Plan plan)
        {
            var subscriptions = plan.Subscriptions ?? new List<Subscription>();

            var discounts = plan.PlanDiscounts?.Select(pd => new DiscountVM
            {
                DiscountId = pd.Discount.DiscountId,
                DiscountCode = pd.Discount.DiscountCode,
                DiscountType = pd.Discount.DiscountType,
                DiscountValue = pd.Discount.Value,
                StartDate = pd.Discount.StartDateTime,
                EndDate = pd.Discount.EndDate
            }).ToList() ?? new List<DiscountVM>();

            var stats = new DashboardStatsVM
            {
                TotalTransactions = subscriptions.Count,
                TotalRevenue = subscriptions
                    .Select(s => s.PaymentTransaction)
                    .Where(p => p != null)
                    .Sum(p => p.Amount),
                FailedPayments = subscriptions
                    .Select(s => s.PaymentTransaction)
                    .Count(p => p != null && p.Status == PaymentTransactionStatus.Failed),
                SuccessfulPayments = subscriptions
                    .Select(s => s.PaymentTransaction)
                    .Count(p => p != null && p.Status == PaymentTransactionStatus.Captured),
                ActiveSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Active)
            };

            return new ManagerPlanDiscountVM
            {
                Id = plan.PlanId,
                PlanName = plan.PlanName,
                PlanDescription = plan.PlanDescription,
                PlanPrice = plan.Price,
                PlanBillingType = plan.BillingType,
                PlanIsActive = plan.IsActive,
                PlanCreatedAt = plan.CreatedAt,
                Discounts = discounts,
                Stats = stats
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
            var datailsData = counsellors
                .Select(c => GetManagerDashboardData(c))
                .FirstOrDefault(d => d.PaymentTransactionId == id);

            if (datailsData == null)
            {
                return NotFound();
            }

            return View(datailsData);
        }

        //[Route("Manager/Plans")]
        public IActionResult Plans()
        {
            var plans = PlanRepository.GetAll() ?? new List<Plan>();
            return View(plans);
        }
    }
}
