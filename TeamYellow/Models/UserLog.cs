using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TeamYellow.Models
{
    /// <summary>
    /// Represents a user login and logout audit record.
    /// </summary>
    [Table("UserLog")]
    public class UserLog
    {
        [Key]
        [Column("pkLogId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogId { get; set; }

        [Column("logInTime")]
        public DateTime LogInTime { get; set; }

        [Column("logOutTime")]
        public DateTime? LogOutTime { get; set; }

        [Column("abandoned")]
        public bool Abandoned { get; set; } = false;

        [Column("fkUserId")]
        public string? UserId { get; set; }

        [Column("userEmailSnapshot")]
        [MaxLength(256)]
        public string? UserEmailSnapshot { get; set; }

        public virtual IdentityUser? User { get; set; }
    }
}
