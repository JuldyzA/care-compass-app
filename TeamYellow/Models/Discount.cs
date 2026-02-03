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
    [MaxLength(40)]
    public string DiscountCode { get; set; } = null!;

    [Required]
    [Column("discountType")]
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;

    [Column("value")]
    public decimal Value { get; set; }

    [Column("startDateTime")]
    public DateTime StartDateTime { get; set; }

    [Column("endDateTime")]
    public DateTime EndDateTime { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}