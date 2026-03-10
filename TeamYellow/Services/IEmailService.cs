using TeamYellow.Models;

namespace TeamYellow.Services
{
    public interface IEmailService
    {
        Task<HttpResponseMessage> SendEmailAsync(ComposeEmailModel payload);
    }
}
