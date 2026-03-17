namespace TeamYellow.Services;

public interface IPayPalService
{
    Task<string> CreateOrder(decimal amount, string currency, string returnUrl, string cancelUrl, string customId);
    Task<(string CaptureId, string CustomId)> CaptureOrder(string token);
}
