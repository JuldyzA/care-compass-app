using TeamYellow.Models;

namespace TeamYellow.Services
{
    /// <summary>
    /// Defines email delivery operations for the application.
    /// </summary>
    public interface IEmailService
    {
        Task<HttpResponseMessage> SendEmailAsync(ComposeEmailModel payload);
    }
}
