using System;
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
    public string PlanName { get; set; } = String.Empty;

    [Required, MaxLength(500)]
    [Column("planDescription")]
    public string PlanDescription { get; set; } = String.Empty;

    [Required] 
    [Range(0, 10000)] 
    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    [Column("billingType")]

    public string  { get; set; } = String.Empty;
    [Required]
    [Column("isActive")]
    public bool IsActive { get; set; } = true;
   
    [Required]
    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
