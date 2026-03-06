using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TeamYellow.Services;

public class PayPalService : IPayPalService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;

    private static string? _cachedToken;
    private static DateTime _tokenExpiry = DateTime.MinValue;

    public PayPalService(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;

        // Configuration key: ApiKeys:PayPal:Mode
        // Expected values:
        //   - "Sandbox" (default if not set) -> uses https://api-m.sandbox.paypal.com
        //   - "Live"                        -> uses https://api-m.paypal.com
        var mode = _configuration["ApiKeys:PayPal:Mode"] ?? "Sandbox";
        _client.BaseAddress = new Uri(mode == "Live"
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com");
    }

    private async Task<string> GetAccessToken()
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        var clientId = _configuration["ApiKeys:PayPal:ClientId"];
        var clientSecret = _configuration["ApiKeys:PayPal:ClientSecret"];

        if (string.IsNullOrEmpty(clientId))
            throw new InvalidOperationException("PayPal ClientId is not configured. Set ApiKeys:PayPal:ClientId in secrets.json.");

        if (string.IsNullOrEmpty(clientSecret))
            throw new InvalidOperationException("PayPal ClientSecret is not configured. Set ApiKeys:PayPal:ClientSecret in secrets.json.");

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}")));
        request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);

        _cachedToken = json?["access_token"]?.ToString() ?? throw new Exception("Failed to get access token");
        var expiresIn = json?["expires_in"]?.GetValue<int>() ?? 32400;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

        return _cachedToken;
    }

    public async Task<string> CreateOrder(decimal amount, string currency, string returnUrl, string cancelUrl)
    {
        var accessToken = await GetAccessToken();

        var orderRequest = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = currency,
                        value = amount.ToString("F2")
                    }
                }
            },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);

        // Find the 'approve' link
        var links = json?["links"]?.AsArray();
        var approveLink = links?.FirstOrDefault(l => l?["rel"]?.ToString() == "approve")?["href"]?.ToString();

        return approveLink ?? throw new Exception("PayPal approval link not found");
    }

    public async Task<string> CaptureOrder(string token)
    {
        var accessToken = await GetAccessToken();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/v2/checkout/orders/{token}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);

        var status = json?["status"]?.ToString();
        var id = json?["id"]?.ToString(); // Capture ID or Order ID depending on response structure

        if (status == "COMPLETED")
        {
            return id ?? token;
        }

        throw new Exception($"Payment capture failed. Status: {status}");
    }
}
