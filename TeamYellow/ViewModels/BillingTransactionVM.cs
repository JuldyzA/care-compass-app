namespace TeamYellow.ViewModels
{
    /// <summary>
    /// Represents a single transaction for display on the billing page.
    /// </summary>
    public class BillingTransactionVM
    {
        public int PaymentTransactionId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string BillingType { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "CAD";

        public DateTime PaidAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Provider { get; set; } = "PayPal";

        public string ProviderOrderId { get; set; } = string.Empty;

        public DateTime? CycleStart { get; set; }

        public DateTime? CycleEnd { get; set; }
    }
}