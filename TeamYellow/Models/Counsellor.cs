using System.ComponentModel.DataAnnotations;
using TeamYellow.Data;

namespace TeamYellow.Models;

public class Counsellor
{
    public int PkCounsellorId { get; set; }

    [Required, MaxLength(50)]
    public string PractitionerLicenceId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(450)]
    public string FkUserId { get; set; } = string.Empty;

    public virtual ApplicationUser User { get; set; } = null!;
}
