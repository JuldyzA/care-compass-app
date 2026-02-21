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
        public ManagerController(CounsellorRepository counsellorRepository)
        {
            CounsellorRepository = counsellorRepository;

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

            return new ManagerDashboardVM
            {
                CounsellorId = counsellor.CounsellorId,
                PractitionerLicenceId = counsellor.PractitionerLicenceId,
                CounsellorName = counsellor.DisplayName,
                Email = counsellor.User?.Email ?? "No email",
                Amount = payments.Sum(p => p?.Amount ?? 0),
                PaymentTransactionId = firstPayment?.PaymentTransactionId ?? 0,
                SOP = payments?.Any(p => p.Status == PaymentTransactionStatus.Failed) ?? false
                    ? "Failed"
                    : "Paid",
                PaidAt = firstPayment?.PaidAt.ToString("yyyy-MM-dd"),
                RegistrationDate = counsellor.CreatedAt.ToString("yyyy-MM-dd")
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
    }
}
