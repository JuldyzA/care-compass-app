using System.ComponentModel.DataAnnotations;

namespace TeamYellow.ViewModels
{
    public class CheckoutVM
    {
        public int PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string PlanDescription { get; set; } = string.Empty;

        public string BillingType { get; set; } = string.Empty;

        public decimal OriginalPrice { get; set; }

        [Display(Name = "Discount Code")]
        public string? DiscountCode { get; set; }

        public int? AppliedDiscountId { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal FinalAmount { get; set; }

        public bool DiscountApplied { get; set; }

        public string? DiscountMessage { get; set; }

        public List<PlanFeatureVM> PlanFeatures { get; set; } = [];
    }
}