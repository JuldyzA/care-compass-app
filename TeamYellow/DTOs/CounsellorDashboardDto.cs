namespace TeamYellow.DTOs;

public class CounsellorDashboardDto
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public int? UnitNumber { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }

    public string? PostalCode { get; set; }

    public string PractitionerLicenceId { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool IsActive { get; set; }

    // 1-to-many lists to avoid row explosion and tracking duplicates
    public IReadOnlyCollection<string> CounsellorSubscriptions { get; init; } = new List<string>();

    public IReadOnlyCollection<ClientDto> Clients { get; init; } = new List<ClientDto>();
}
