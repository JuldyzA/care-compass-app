using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models;

public enum SubscriptionStatus
{
    Active = 1,
    Cancelled = 2,
    Expired = 3,
}

[Table("Subscription")]
public class Subscription
{
    [Key]
    [Column("pkSubscriptionId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SubscriptionId { get; set; }

    [Required]
    [Column("status")]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    [Column("cycleStart")]
    public DateTime CycleStart { get; set; }

    [Column("cycleEnd")]
    public DateTime CycleEnd { get; set; }

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    // Foreign Keys
    [Required]
    [Column("fkPlanId")]
    public int PlanId { get; set; }

    [Required]
    [Column("fkCounsellorId")]
    public int CounsellorId { get; set; }

    // Navigation Properties
    public virtual Plan Plan { get; set; } = null!;

    public virtual Counsellor Counsellor { get; set; } = null!;

    public virtual PaymentTransaction? PaymentTransaction { get; set; }
}
