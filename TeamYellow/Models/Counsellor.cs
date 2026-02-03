using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models;

[Table("Counsellor")]
public class Counsellor
{
    [Key]
    [Column("pkCounsellorId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CounsellorId { get; set; }

    [Column("practitionerLicenceId")]
    [Required, MaxLength(50)]
    public string PractitionerLicenceId { get; set; } = string.Empty;

    [Column("displayName")]
    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Column("isActive")]
    public bool IsActive { get; set; } = true;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("fkUserId")]
    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public virtual IdentityUser User { get; set; } = null!;
}
