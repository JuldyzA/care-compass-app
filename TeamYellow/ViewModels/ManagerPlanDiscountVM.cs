using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    public class ManagerPlanDiscountVM
    {
        public int Id { get; set; }
        public string? PlanName { get; set; } 
        public string? PlanDescription { get; set; }

        public decimal PlanPrice { get; set; }
         public string? PlanBillingType { get; set; }

        public bool PlanIsActive { get; set; }

        public DateTime PlanCreatedAt { get; set; }

        public List<DiscountVM> Discounts { get; set; } = new();

        public DashboardStatsVM? Stats { get; set; }

    }
}
