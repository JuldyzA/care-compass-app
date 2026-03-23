using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models;

[Table("Plan")]
public class Plan
{
    [Key]
    [Column("pkPlanId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PlanId { get; set; }

    [Required, MaxLength(80)]
    [Column("planName")]
    public string PlanName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    [Column("planDescription")]
    public string PlanDescription { get; set; } = string.Empty;

    [Required]
    [Range(0, 10000)]
    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    [Column("billingType")]
    public string BillingType { get; set; } = string.Empty;

    [Column("isActive")]
    public bool IsActive { get; set; } = true;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public virtual ICollection<PlanDiscount> PlanDiscounts { get; set; } = new List<PlanDiscount>();
}
