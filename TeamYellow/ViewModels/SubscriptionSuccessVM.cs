namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model representing the subscription success page, including payment
    /// and billing cycle details.
    /// </summary>
    public class SubscriptionSuccessVM
    {
        public string Message { get; set; } = string.Empty;

        public int SubscriptionId { get; set; }

        public string PlanName { get; set; } = string.Empty;
    
        public DateTime CycleStart { get; set; }
        public DateTime CycleEnd { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "CAD";

        public DateTime PaidAt { get; set; }

        public string ProviderOrderId { get; set; } = string.Empty;
    }
}
