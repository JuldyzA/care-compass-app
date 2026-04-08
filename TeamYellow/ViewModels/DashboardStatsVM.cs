namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model representing summary statistics displayed on the manager dashboard.
    /// </summary>
    public class DashboardStatsVM
    {
        public int TotalTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int FailedPayments { get; set; }
        public int SuccessfulPayments { get; set; }

    }
}
