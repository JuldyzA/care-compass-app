using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeamYellow.Models;

namespace TeamYellow.DTOs;

public class CounsellorDashboardDto
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Phone { get; set; }

    public DateTime profileCreateAt { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public int? UnitNumber { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? PostalCode { get; set; }

    public string PractitionerLicenceId { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool IsCounsellorActive { get; set; }

    public SubscriptionStatus status { get; set; }

    public DateTime CycleStart { get; set; }

    public DateTime CycleEnd { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public string PlanDescription { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string BillingType { get; set; } = string.Empty;

    public bool IsPlanActive { get; set; } = true;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    public IEnumerable<ClientDto> Clients { get; init; } = new List<ClientDto>();
}
