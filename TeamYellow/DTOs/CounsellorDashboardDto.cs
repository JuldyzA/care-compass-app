using TeamYellow.Models;

namespace TeamYellow.DTOs;

public class CounsellorDashboardDto
{
    public int UserProfileId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Phone { get; set; }

    public DateTime ProfileCreateAt { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public int? UnitNumber { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? PostalCode { get; set; }

    public int CounsellerId { get; set; }

    public string PractitionerLicenceId { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool IsCounsellorActive { get; set; }

    public int SubscriptionId { get; set; }

    public SubscriptionStatus Status { get; set; }

    public bool IsSubscriptionActive { get; set; }

    public DateTime CycleStart { get; set; }

    public DateTime CycleEnd { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int[] MonthlyClientCounts { get; set; } = new int[12];

    public int ActiveClientCount { get; set; }

    public int InActiveClientCount { get; set; }
}
