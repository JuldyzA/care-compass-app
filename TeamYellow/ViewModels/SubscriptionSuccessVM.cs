namespace TeamYellow.ViewModels;

public class SubscriptionSuccessVM
{
    public string Message { get; set; } = string.Empty;

    public int SubscriptionId { get; set; }

    public string PlanName { get; set; } = string.Empty;
    
    public DateTime CycleStart { get; set; }
    public DateTime CycleEnd { get; set; }
}
