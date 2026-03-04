namespace TeamYellow.ViewModels
{
    public class PlanVM
    {
        public int PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string PlanDescription { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string BillingType { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public List<PlanFeatureVM> PlanFeatures { get; set; } = [];
    }
}
