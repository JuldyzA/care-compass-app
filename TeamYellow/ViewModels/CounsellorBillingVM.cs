using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model representing the counsellor billing page with transaction history.
    /// </summary>
    public class CounsellorBillingVM
    {
        public string DisplayName { get; set; } = string.Empty;

        public string? ProfilePhotoUrl { get; set; }

        public List<BillingTransactionVM> Transactions { get; set; } = new List<BillingTransactionVM>();

        public decimal TotalSpent { get; set; }

        public int TotalTransactions { get; set; }

        public bool IsSubscriptionActive { get; set; }

        public DateTime? CurrentCycleEnd { get; set; }
    }
}