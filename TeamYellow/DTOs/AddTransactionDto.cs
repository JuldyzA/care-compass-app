namespace TeamYellow.DTOs;

/// <summary>
/// Represents the data required to create a new payment transaction.
/// </summary>
public class AddTransactionDto
{
    public int SubscriptionId { get; set; }
    public string PayerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ProviderOrderId { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public int? DiscountId { get; set; }
}
