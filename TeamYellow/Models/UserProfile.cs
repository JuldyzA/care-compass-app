using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TeamYellow.Models;

[Table("UserProfile")]
public class UserProfile
{
    [Key]
    [Column("pkUserProfileId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserProfileId { get; set; }

    [Column("firstName")]
    public string FirstName { get; set; } = String.Empty;

    [Column("lastName")]
    public string LastName { get; set; } = String.Empty;

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    [Column("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [Column("profilePhotoUrl")]
    public string? ProfilePhotoUrl { get; set; }

    [Column("unitNumber")]
    public int? UnitNumber { get; set; }

    [Column("street")]
    [MaxLength(120)]
    public string? Street { get; set; }

    [Column("city")]
    [MaxLength(80)]
    [RegularExpression(@"^[A-Za-z\s\-'.]+$", ErrorMessage = "City can only contain letters, spaces, hyphens, and apostrophes")]
    public string? City { get; set; }

    [Column("province")]
    [MaxLength(2)]
    [RegularExpression(@"^(AB|BC|MB|NB|NL|NS|NT|NU|ON|PE|QC|SK|YT)$", ErrorMessage = "Invalid province code")]
    public string? Province { get; set; }

    [Column("postalCode")]
    [MaxLength(7)]
    [RegularExpression(@"^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$", ErrorMessage = "Invalid postal code format (e.g., V5K 0A1)")]
    public string? PostalCode { get; set; }

    [Required]
    [Column("fkUserId")]
    public string UserId { get; set; } = null!;

    public virtual IdentityUser User { get; set; } = null!;
}
