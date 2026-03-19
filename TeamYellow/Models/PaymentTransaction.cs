using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeamYellow.Models;

public enum PaymentTransactionStatus
{
    Captured = 0,
    Failed = 1,
}

[Table("PaymentTransaction")]
public class PaymentTransaction
{
    [Key]
    [Column("pkPaymentTransactionId")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PaymentTransactionId { get; set; }

    [Required]
    [Column("payerName")]
    [MaxLength(100)]
    public string PayerName { get; set; } = null!;

    [Column("amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Column("currency", TypeName = "char(3)")]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "CAD";

    [Column("provider")]
    [MaxLength(20)]
    public string Provider { get; set; } = "PayPal";

    [Required]
    [Column("providerOrderId")]
    [MaxLength(100)]
    public string ProviderOrderId { get; set; } = null!;

    [Column("status")]
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Captured;

    [Column("paidAt")]
    public DateTime PaidAt { get; set; }

    [Column("fkSubscriptionId")]
    public int SubscriptionId { get; set; }

    [Column("fkDiscountId")]
    public int? DiscountId { get; set; }

    public virtual Discount? Discount { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;
}
