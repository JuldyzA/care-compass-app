using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TeamYellow.Services;

/// <summary>
/// Service that communicates with the PayPal REST API to create and capture payment orders.
/// Supports both Sandbox and Live environments, configurable via <c>ApiKeys:PayPal:Mode</c>.
/// Access tokens are cached and refreshed automatically when they expire.
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
    /// Retrieves a cached OAuth 2.0 access token from PayPal, or requests a new one if
    /// the cached token is absent or has expired.
    /// </summary>
    /// <returns>A valid PayPal Bearer access token string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <c>ApiKeys:PayPal:ClientId</c> or <c>ApiKeys:PayPal:ClientSecret</c>
    /// are not configured in application secrets.
    /// </exception>
    /// <exception cref="Exception">Thrown if the PayPal token response does not contain an access token.</exception>
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
    /// The <paramref name="customId"/> is embedded in the order so it can be retrieved after capture.
    /// </summary>
    /// <param name="amount">The monetary amount to charge, in the specified currency.</param>
    /// <param name="currency">The ISO 4217 currency code (e.g., <c>CAD</c>, <c>USD</c>).</param>
    /// <param name="returnUrl">The URL PayPal redirects the buyer to after approval.</param>
    /// <param name="cancelUrl">The URL PayPal redirects the buyer to if they cancel.</param>
    /// <param name="customId">
    /// An application-defined identifier embedded in the order (e.g., the plan ID),
    /// returned in the capture response for reconciliation.
    /// </param>
    /// <returns>The PayPal buyer approval URL that the user should be redirected to.</returns>
    /// <exception cref="Exception">Thrown if the PayPal response does not include an approval link.</exception>
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

    /// <summary>
    /// Captures a previously approved PayPal order using its approval token.
    /// Returns the PayPal capture ID and the custom ID that was embedded when the order was created.
    /// </summary>
    /// <param name="token">The PayPal order approval token (returned by PayPal as the <c>token</c> query parameter).</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description><c>CaptureId</c> – the PayPal capture transaction identifier.</description></item>
    ///   <item><description><c>CustomId</c> – the application-defined value set when the order was created (e.g., plan ID).</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown if the PayPal response is missing <c>custom_id</c>, or if the payment capture status is not <c>COMPLETED</c>.
    /// </exception>
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

        var captureId = json?["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["id"]?.ToString()
            ?? json?["id"]?.ToString()
            ?? token;

        var customId = json?["purchase_units"]?[0]?["custom_id"]?.ToString()
            ?? json?["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["custom_id"]?.ToString()
            ?? throw new Exception("PayPal response missing custom_id");

        var capturedAmountText = json?["purchase_units"]?[0]?["payments"]?["captures"]?[0]?["amount"]?["value"]?.ToString()
            ?? json?["purchase_units"]?[0]?["amount"]?["value"]?.ToString()
            ?? throw new Exception("PayPal response missing captured amount.");

        if (!decimal.TryParse(capturedAmountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var capturedAmount))
            throw new Exception("PayPal response contained an invalid captured amount.");

        if (status == "COMPLETED")
            return (captureId, customId, capturedAmount);

        throw new Exception($"Payment capture failed. Status: {status}");
    }
}
