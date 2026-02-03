using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models;

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
    [Required]
    public string UserId { get; set; } = string.Empty;

    public virtual IdentityUser User { get; set; } = null!;
}
