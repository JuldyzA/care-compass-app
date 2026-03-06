namespace TeamYellow.Services;

public interface IPayPalService
{
    Task<string> CreateOrder(decimal amount, string currency, string returnUrl, string cancelUrl);
    Task<string> CaptureOrder(string token);
}
