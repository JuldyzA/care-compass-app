namespace TeamYellow.Models
{
    /// <summary>
    /// Represents the data required to compose and send an email message.
    /// </summary>
    public class ComposeEmailModel
    {
        public string Subject { get; set; }
        public string Email { get; set; }
        public string Body { get; set; }
    }
}
