using TeamYellow.Models;
namespace TeamYellow.ViewModels
{
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
        public string? PaidAt { get; set; }
        public int PaymentTransactionId { get; set; }
      
    }
}