namespace TeamYellow.DTOs;

/// <summary>
/// Represents the data required to create a new subscription.
/// </summary>
public class AddSubscriptionDto
{
    public int CounsellorId { get; set; }
    public int PlanId { get; set; }
    public string BillingType { get; set; } = string.Empty;
}
