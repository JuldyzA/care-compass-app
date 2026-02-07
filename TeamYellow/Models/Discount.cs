using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models;

public enum DiscountType
{
    Percent = 0,   // %
    Amount = 1     // $
}

[Table("Discount")]
public class Discount
{
    [Key]
    [Column("pkDiscountId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int DiscountId { get; set; }

    [Required]
    [Column("discountCode")]
    [RegularExpression(
        @"^[A-Z0-9]{3,40}$",
        ErrorMessage = "Discount code must be 3–40 characters and contain only uppercase letters (A–Z) and numbers (0–9).")]
    [MaxLength(40)]
    public string DiscountCode { get; set; } = null!;

    [Column("discountType")]
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;

    [Column("value", TypeName= "decimal(10,2)")]
    public decimal Value { get; set; }

    [Column("startDateTime")]
    public DateTime StartDateTime { get; set; }

    [Column("endDateTime")]
    public DateTime EndDateTime { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PlanDiscount> PlanDiscounts { get; set; } = new List<PlanDiscount>();

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}