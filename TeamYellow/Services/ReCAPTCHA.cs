using Newtonsoft.Json;

namespace TeamYellow.Services
{
    /// <summary>
    /// Contains helper types for validating Google reCAPTCHA responses.
    /// </summary>
    public class ReCAPTCHA
    {
        /// <summary>
        /// Represents the result returned by Google reCAPTCHA verification.
        /// </summary>
        public class ReCaptchaValidationResult
        {
            public bool Success { get; set; }
            public string HostName { get; set; }

            [JsonProperty("challenge_ts")]
            public string TimeStamp { get; set; }

            [JsonProperty("error-codes")]
            public List<string> ErrorCodes { get; set; }
        }

        /// <summary>
        /// Provides methods for validating Google reCAPTCHA responses.
        /// </summary>
        public class ReCaptchaValidator
        {
            /// <summary>
            /// Validates the submitted reCAPTCHA response against the Google verification endpoint.
            /// </summary>
            /// <param name="secret">The server-side reCAPTCHA secret key.</param>
            /// <param name="captchaResponse">The reCAPTCHA response token submitted by the client.</param>
            /// <returns>The parsed reCAPTCHA validation result.</returns>
            public static ReCaptchaValidationResult IsValid(string secret, string captchaResponse)
            {
                if (string.IsNullOrWhiteSpace(captchaResponse))
                {
                    return new ReCaptchaValidationResult { Success = false };
                }

                HttpClient client = new HttpClient
                {
                    BaseAddress = new Uri("https://www.google.com")
                };

                var values = new List<KeyValuePair<string, string>>
                {
                    new("secret", secret),
                    new("response", captchaResponse)
                };

                var content = new FormUrlEncodedContent(values);

                var response =
                client.PostAsync("/recaptcha/api/siteverify", content).Result;

                string verificationResponse =
                    response.Content.ReadAsStringAsync().Result;

                return
JsonConvert.DeserializeObject<ReCaptchaValidationResult>(verificationResponse);
            }
        }
    }
}
