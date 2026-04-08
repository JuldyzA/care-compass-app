namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model representing a single counsellor payment transaction row
    /// on the manager dashboard.
    /// </summary>
    public class ManagerDashboardVM
    {
        public int CounsellorId { get; set; }
        public required string PractitionerLicenceId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string CounsellorName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string SOP { get; set; } = string.Empty;
        public string RegistrationDate { get; set; } = string.Empty;
        public string Currency { get; set; } = "CAD";
        public DateTime? PaidAt { get; set; }
        public int PaymentTransactionId { get; set; }
        public string BillingType { get; set; } = string.Empty;
    }
}