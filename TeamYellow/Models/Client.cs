using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models
{


    [Table("Client")]
    public class Client
    {
        [Key]
        [Column("pkClientId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ClientId { get; set; }

        [Column("firstName")]
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Column("lastName")]
        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Column("email")]
        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        [Required, MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Column("status")]
        [Required, MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("fkCounsellorId")]
        public int CounsellorId { get; set; }

        public virtual Counsellor Counsellor { get; set; } = null!;
    }
}
