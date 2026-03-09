using TeamYellow.Models;

namespace TeamYellow.ViewModels;

public class CounsellorDashboardVM
{
    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

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

    public int PlanId { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public string PlanDescription { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string BillingType { get; set; } = string.Empty;

    public bool IsPlanActive { get; set; } = false;

    public DateTime PlanCreatedAt { get; set; }

    public int[] MonthlyClientCounts { get; set; } = new int[12];

    public double ClientGrowthFromLastMonth { get; set; }

    public int[] ActiveClientCount { get; set; } = new int[2];
}
