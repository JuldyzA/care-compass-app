using System;
using System.ComponentModel.DataAnnotations;


namespace TeamYellow.Models;
// public enum BillingType { Monthly = 1, Yearly = 2 }
public class Plan
{
    [Key]
    public int PlanId { get; set; }

    [Required, MaxLength(80)]
    public string PlanName { get; set; } = String.Empty;

    [Required, MaxLength(500)]
    public string PlanDescription { get; set; } = String.Empty;

    [Required] 
    [Range(0, 100000)] 
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    public string BillingType { get; set; } = String.Empty;
    // public BillingType BillingType { get; set; }

    
    [Required]
    public int IsActive { get; set; }
    // public bool IsActive { get; set; } = true;
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
