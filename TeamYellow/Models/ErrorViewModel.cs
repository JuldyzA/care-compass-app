namespace TeamYellow.Models
{
    /// <summary>
    /// Represents error information displayed by the application error page.
    /// </summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
