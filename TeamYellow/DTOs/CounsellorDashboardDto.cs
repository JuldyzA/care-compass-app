using TeamYellow.Models;

namespace TeamYellow.DTOs
{
    /// <summary>
    /// Represents the data required to display the counsellor dashboard,
    /// including profile details, subscription status, and client summary metrics.
    /// </summary>
    public class CounsellorDashboardDto
    {
        public string? ProfilePhotoUrl { get; set; }

        public string DisplayName { get; set; } = null!;

        public SubscriptionStatus Status { get; set; }

        public bool IsSubscriptionActive { get; set; }

        public DateTime CycleStart { get; set; }

        public DateTime CycleEnd { get; set; }

        public int[] MonthlyClientCounts { get; set; } = new int[12];

        public int ActiveClientCount { get; set; }

        public int InactiveClientCount { get; set; }
    }
}