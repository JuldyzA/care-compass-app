using System.Text;
using System.Text.Json;
using TeamYellow.Models;

namespace TeamYellow.Services
{
    public class BrevoEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public BrevoEmailService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

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

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                response.Dispose();
                throw new InvalidOperationException($"Brevo email failed: {error}");
            }

            return response;
        }
    }
}