namespace TeamYellow.DTOs;

/// <summary>
/// Data transfer object for profile image upload requests.
/// </summary>
public class ProfileImageUploadDto
{
    public string? ImageData { get; set; }

    public string? FileName { get; set; }
}