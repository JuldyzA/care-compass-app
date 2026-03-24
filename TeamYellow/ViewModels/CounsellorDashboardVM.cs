using TeamYellow.Models;

namespace TeamYellow.ViewModels;

/// <summary>
/// View model representing counsellor dashboard information, including subscription
/// details, client statistics, and growth metrics.
/// </summary>
public class CounsellorDashboardVM
{
    public string? ProfilePhotoUrl { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public SubscriptionStatus Status { get; set; }

    public bool IsSubscriptionActive { get; set; }

    public DateTime CycleStart { get; set; }

    public DateTime CycleEnd { get; set; }

    public int[] MonthlyClientCounts { get; set; } = new int[12];

    public double ClientGrowthFromLastMonth { get; set; }

    public int ActiveClientCount { get; set; }

    public int InactiveClientCount { get; set; }

    public int TotalSubscriptionDays { get; set; }

    public int RemainingSubscriptionDays { get; set; }
}
