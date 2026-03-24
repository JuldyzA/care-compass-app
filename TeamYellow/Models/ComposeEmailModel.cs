namespace TeamYellow.Models
{
    /// <summary>
    /// Represents the data required to compose and send an email message.
    /// </summary>
    public class ComposeEmailModel
    {
        public string Subject { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
