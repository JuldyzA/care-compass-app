using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models
{
    /// <summary>
    /// Represents a counsellor account, including profile information, subscriptions, and clients.
    /// </summary>
    [Table("Counsellor")]
    public class Counsellor
    {
        [Key]
        [Column("pkCounsellorId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CounsellorId { get; set; }

        [Column("practitionerLicenceId")]
        [Required, MaxLength(50)]
        [RegularExpression(@"^[A-Z0-9][0-9]{6}$", ErrorMessage = "Practitioner Licence Id must be 7 characters: starting with an uppercase letter or digit, followed by 6 digits.")]
        public string PractitionerLicenceId { get; set; } = string.Empty;

        [Column("displayName")]
        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Column("isActive")]
        public bool IsActive { get; set; } = true;

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; }

        [Column("fkUserId")]
        [MaxLength(450)]
        public string? UserId { get; set; }

        [Column("archivedNormalizedEmail")]
        [MaxLength(256)]
        public string? ArchivedNormalizedEmail { get; set; }

        [Column("archivedEmailDisplay")]
        [MaxLength(256)]
        public string? ArchivedEmailDisplay { get; set; }

        public virtual IdentityUser? User { get; set; }

        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
    }
}