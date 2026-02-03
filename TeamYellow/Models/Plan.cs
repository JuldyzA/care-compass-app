using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace TeamYellow.Models;
public enum BillingType { Monthly = 1, Yearly = 2 }
[Table("Plans")]
public class Plan
{
    [Key]
    [Column("pkPlanId")]
    public int PlanId { get; set; }

    [Required, MaxLength(80)]
    [Column("PlanName")]
    public string PlanName { get; set; } = String.Empty;

    [Required, MaxLength(500)]
    [Column("PlanDescription")]
    public string PlanDescription { get; set; } = String.Empty;

    [Required] 
    [Range(0, 10000)] 
    [Column("Price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    [Column("BillingType")]
    [EnumDataType(typeof(BillingType))]
    public string BillingType { get; set; } = String.Empty;
    // public BillingType BillingType { get; set; }

    [Required]
    [Column("IsActive")]
    public int IsActive { get; set; }
    // public bool IsActive { get; set; } = true;
   
    [Required]
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
