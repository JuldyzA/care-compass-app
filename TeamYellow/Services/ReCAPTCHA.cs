using Newtonsoft.Json;

namespace TeamYellow.Services
{
    public class ReCAPTCHA
    {
        public class ReCaptchaValidationResult
        {
            public bool Success { get; set; }
            public string HostName { get; set; }

            [JsonProperty("challenge_ts")]
            public string TimeStamp { get; set; }

            [JsonProperty("error-codes")]
            public List<string> ErrorCodes { get; set; }
        }

        public class ReCaptchaValidator
        {
            public static ReCaptchaValidationResult
                   IsValid(string secret, string captchaResponse)
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
