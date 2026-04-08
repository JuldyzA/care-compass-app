using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models
{
    /// <summary>
    /// Defines the possible status values for a client record.
    /// </summary>
    public enum ClientStatus
    {
        Inactive = 0,
        Active = 1
    }

    /// <summary>
    /// Represents a client record managed by a counsellor.
    /// </summary>
    [Table("Client")]
    public class Client
    {
        [Key]
        [Column("pkClientId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ClientId { get; set; }

        [Column("firstName")]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Column("lastName")]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Column("email")]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Column("status")]
        public ClientStatus Status { get; set; } = ClientStatus.Active;

        [Column("createdAt")]
        public DateTime CreatedAt { get; set; }

        [Column("fkCounsellorId")]
        public int CounsellorId { get; set; }

        public virtual Counsellor Counsellor { get; set; } = null!;
    }
}