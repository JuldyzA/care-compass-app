namespace TeamYellow.ViewModels;

/// <summary>
/// View model for displaying user account credentials.
/// </summary>
public class UserAccountVM
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the masked password for display purposes.
    /// </summary>
    public string MaskedPassword => "***********";
}