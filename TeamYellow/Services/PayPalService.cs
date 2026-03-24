using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TeamYellow.Services;

/// <summary>
/// Service that communicates with the PayPal REST API to create and capture payment orders.
/// </summary>
public class PayPalService : IPayPalService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;

    private static string? _cachedToken;
    private static DateTime _tokenExpiry = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of <see cref="PayPalService"/>.
    /// Sets the <see cref="HttpClient"/> base address based on the configured PayPal mode.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> used to call the PayPal API.</param>
    /// <param name="configuration">
    /// The application configuration. Reads <c>ApiKeys:PayPal:Mode</c> (defaults to <c>Sandbox</c>)
    /// to determine the PayPal API base URL.
    /// </param>
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

    /// <summary>
    /// Retrieves a cached OAuth access token from PayPal, or requests a new one if needed.
    /// </summary>
    /// <returns>A valid PayPal bearer access token.</returns>
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

    /// <summary>
    /// Creates a PayPal checkout order for a given amount and returns the buyer approval URL.
    /// </summary>
    /// <param name="amount">The monetary amount to charge.</param>
    /// <param name="currency">The ISO currency code.</param>
    /// <param name="returnUrl">The URL PayPal redirects to after approval.</param>
    /// <param name="cancelUrl">The URL PayPal redirects to if the buyer cancels.</param>
    /// <param name="customId">The application-defined identifier to embed in the order.</param>
    /// <returns>The PayPal buyer approval URL.</returns>
    public async Task<string> CreateOrder(decimal amount, string currency, string returnUrl, string cancelUrl, string customId)
    {
        var accessToken = await GetAccessToken();

        var orderRequest = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    custom_id = customId,
                    amount = new
                    {
                        currency_code = currency,
                        value = amount.ToString("F2", CultureInfo.InvariantCulture)
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

    /// <summary>
    /// Captures a previously approved PayPal order using its approval token.
    /// </summary>
    /// <param name="token">The PayPal order approval token.</param>
    /// <returns>
    /// A tuple containing the PayPal capture identifier, embedded custom identifier,
    /// and total captured amount.
    /// </returns>
    public async Task<(string CaptureId, string CustomId, decimal CapturedAmount)> CaptureOrder(string token)
    {
        var accessToken = await GetAccessToken();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/v2/checkout/orders/{token}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(content);

        var status = json?["status"]?.ToString();

        var purchaseUnitsNode = json?["purchase_units"];
        if (purchaseUnitsNode is not JsonArray purchaseUnitsArray || purchaseUnitsArray.Count != 1)
            throw new Exception("PayPal response must contain exactly one purchase unit.");

        var purchaseUnit = purchaseUnitsArray[0];

        var captureId = purchaseUnit?["payments"]?["captures"]?[0]?["id"]?.ToString()
            ?? json?["id"]?.ToString()
            ?? token;

        var customId = purchaseUnit?["custom_id"]?.ToString()
            ?? purchaseUnit?["payments"]?["captures"]?[0]?["custom_id"]?.ToString()
            ?? throw new Exception("PayPal response missing custom_id");

        var capturesNode = purchaseUnit?["payments"]?["captures"];
        if (capturesNode is not JsonArray capturesArray || capturesArray.Count == 0)
            throw new Exception("PayPal response missing captured amount.");

        decimal capturedAmount = 0m;

        foreach (var capture in capturesArray)
        {
            var amountText = capture?["amount"]?["value"]?.ToString();

            if (string.IsNullOrWhiteSpace(amountText) ||
                !decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                throw new Exception("PayPal response contained an invalid captured amount.");
            }

            capturedAmount += amount;
        }

        if (status == "COMPLETED")
            return (captureId, customId, capturedAmount);

        throw new Exception($"Payment capture failed. Status: {status}");
    }
}
