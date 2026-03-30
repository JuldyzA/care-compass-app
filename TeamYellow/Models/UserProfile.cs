using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TeamYellow.Models 
{ 
    /// <summary>
    /// Represents additional profile information stored for an identity user.
    /// </summary>
    [Table("UserProfile")]
    public class UserProfile
    {
        [Key]
        [Column("pkUserProfileId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserProfileId { get; set; }

        [Column("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [Column("lastName")]
        public string LastName { get; set; } = string.Empty;

        [Column("phone")]
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
        public string? Street { get; set; }

        [Column("city")]
        public string? City { get; set; }

        [Column("province")]
        public string? Province { get; set; }

        [Column("postalCode")]
        public string? PostalCode { get; set; }

        [Required]
        [Column("fkUserId")]
        public string UserId { get; set; } = null!;

        public virtual IdentityUser User { get; set; } = null!;
    }
}
