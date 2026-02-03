using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

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

    [Column("fkUserId")]
    [Required]
    public string UserId { get; set; } = string.Empty;

    public virtual IdentityUser User { get; set; } = null!;
}
