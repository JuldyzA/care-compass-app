using Newtonsoft.Json;

namespace TeamYellow.Services
{
    /// <summary>
    /// Contains types used to verify Google reCAPTCHA responses.
    /// </summary>
    public class ReCAPTCHA
    {
        /// <summary>
        /// Represents the response returned by Google's reCAPTCHA verification API.
        /// </summary>
        public class ReCaptchaValidationResult
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("hostname")]
            public string? HostName { get; set; }

            [JsonProperty("challenge_ts")]
            public string? TimeStamp { get; set; }

            [JsonProperty("error-codes")]
            public List<string> ErrorCodes { get; set; } = new();
        }

        /// <summary>
        /// Validates submitted reCAPTCHA tokens against Google's verification endpoint.
        /// </summary>
        public class ReCaptchaValidator
        {
            private readonly HttpClient _httpClient;

            /// <summary>
            /// Initializes a new instance of <see cref="ReCaptchaValidator"/>.
            /// </summary>
            /// <param name="httpClient">The HTTP client used to call the Google reCAPTCHA API.</param>
            public ReCaptchaValidator(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            /// <summary>
            /// Verifies the submitted reCAPTCHA token with Google and returns the parsed validation result.
            /// </summary>
            /// <param name="secret">The server-side reCAPTCHA secret key.</param>
            /// <param name="captchaResponse">The reCAPTCHA token submitted by the client.</param>
            /// <returns>
            /// A <see cref="ReCaptchaValidationResult"/> containing the verification outcome.
            /// Returns an unsuccessful result when the token is missing or the API response cannot be parsed.
            /// </returns>
            public async Task<ReCaptchaValidationResult> IsValidAsync(string secret, string captchaResponse)
            {
                if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(captchaResponse))
                {
                    return new ReCaptchaValidationResult
                    {
                        Success = false,
                        ErrorCodes = new List<string> { "missing-input" }
                    };
                }

                var values = new List<KeyValuePair<string, string>>
                {
                    new("secret", secret),
                    new("response", captchaResponse)
                };

                try
                {
                    using var content = new FormUrlEncodedContent(values);
                    using var response = await _httpClient.PostAsync("/recaptcha/api/siteverify", content);
                    var verificationResponse = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return new ReCaptchaValidationResult
                        {
                            Success = false,
                            ErrorCodes = new List<string> { $"http-{(int)response.StatusCode}" }
                        };
                    }

                    try
                    {
                        var result = JsonConvert.DeserializeObject<ReCaptchaValidationResult>(verificationResponse);

                        return result ?? new ReCaptchaValidationResult
                        {
                            Success = false,
                            ErrorCodes = new List<string> { "empty-response" }
                        };
                    }
                    catch (JsonException)
                    {
                        return new ReCaptchaValidationResult
                        {
                            Success = false,
                            ErrorCodes = new List<string> { "invalid-json" }
                        };
                    }
                }
                catch (HttpRequestException)
                {
                    return new ReCaptchaValidationResult
                    {
                        Success = false,
                        ErrorCodes = new List<string> { "http-request-failed" }
                    };
                }
                catch (TaskCanceledException)
                {
                    return new ReCaptchaValidationResult
                    {
                        Success = false,
                        ErrorCodes = new List<string> { "request-timeout" }
                    };
                }
            }
        }
    }
}
