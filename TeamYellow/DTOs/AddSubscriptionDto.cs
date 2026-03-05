namespace TeamYellow.DTOs;

public class AddSubscriptionDto
{
    public int CounsellorId { get; set; }
    public int PlanId { get; set; }
    public string BillingType { get; set; } = string.Empty;
}
