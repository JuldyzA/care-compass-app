using TeamYellow.Models;

namespace TeamYellow.DTOs;

/// <summary>
/// Handles subscription workflows, including PayPal checkout, success and failure callbacks,
/// cancellation handling, and counsellor role assignment.
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
