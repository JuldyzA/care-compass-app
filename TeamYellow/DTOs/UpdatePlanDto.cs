namespace TeamYellow.DTOs
{
    public class UpdatePlanDto
    {
        public int PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string PlanDescription { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string BillingType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<AddPlanFeatureDto> PlanFeatureDtos { get; set; } = new List<AddPlanFeatureDto>();
    }
}
