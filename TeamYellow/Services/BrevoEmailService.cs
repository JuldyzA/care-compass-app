using System.Text;
using System.Text.Json;
using TeamYellow.Models;

namespace TeamYellow.Services
{
    /// <summary>
    /// Service that sends transactional emails through the Brevo email API.
    /// </summary>
    public class BrevoEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<BrevoEmailService> _logger;

        public BrevoEmailService(IConfiguration configuration, HttpClient httpClient, ILogger<BrevoEmailService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Service that sends transactional emails through the Brevo email API.
        /// </summary>
        public async Task<HttpResponseMessage> SendEmailAsync(ComposeEmailModel payload)
        {
            var apiKey = _configuration["Brevo:ApiKey"];

            var requestBody = new
            {
                sender = new
                {
                    name = _configuration["Brevo:Name"],
                    email = _configuration["Brevo:Email"]
                },
                to = new[]
                {
                new { email = payload.Email }
            },
                subject = payload.Subject,
                htmlContent = payload.Body
            };

            var json = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("api-key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending Brevo email to {Email} with subject {Subject}.", payload.Email, payload.Subject);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Brevo email failed for recipient {Email}. Response: {Error}", payload.Email, error);
                response.Dispose();
                throw new InvalidOperationException($"Brevo email failed: {error}");
            }

            _logger.LogInformation("Brevo email sent successfully to {Email}.", payload.Email);
            return response;
        }
    }
}