using TeamYellow.Models;

namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model representing counsellor dashboard information, including subscription
    /// details, client statistics, growth metrics, and dashboard access state.
    /// </summary>
    public class CounsellorDashboardVM
    {
        public string? ProfilePhotoUrl { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public SubscriptionStatus Status { get; set; }

        public bool IsSubscriptionActive { get; set; }

        public bool IsDashboardLocked { get; set; }

        public DateTime CycleStart { get; set; }

        public DateTime CycleEnd { get; set; }

        public int[] MonthlyClientCounts { get; set; } = new int[12];

        public double ClientGrowthFromLastMonth { get; set; }

        public int ActiveClientCount { get; set; }

        public int InactiveClientCount { get; set; }

        public string RemainingSubscriptionText { get; set; } = string.Empty;
    }
}