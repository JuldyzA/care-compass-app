using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models;

[Table("PlanDiscount")]
public class PlanDiscount
{
    [Column("fkPlanId")]
    public int PlanId { get; set; }

    public virtual Plan Plan { get; set; } = null!;

    [Column("fkDiscountId")]
    public int DiscountId { get; set; }

    public virtual Discount Discount { get; set; } = null!;
}
