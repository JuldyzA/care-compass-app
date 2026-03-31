namespace TeamYellow.Configurations;

/// <summary>
/// Standardized error and success messages for image upload operations.
/// </summary>
public static class ImageUploadMessages
{
    /// <summary>
    /// Error message when no image data is provided.
    /// </summary>
    public const string NoImageDataProvided = "No image data provided.";

    /// <summary>
    /// Error message when image data format is invalid.
    /// </summary>
    public const string InvalidImageDataFormat = "Invalid image data format.";

    /// <summary>
    /// Error message when image data is not valid base64.
    /// </summary>
    public const string InvalidBase64Data = "Image data is not valid base64.";

    /// <summary>
    /// Error message prefix when file size exceeds the limit.
    /// </summary>
    public const string FileSizeExceedsPrefix = "File size exceeds";

    /// <summary>
    /// Error message when user cannot be identified.
    /// </summary>
    public const string UserNotIdentified = "User not identified.";

    /// <summary>
    /// Generic error message for upload failures.
    /// </summary>
    public const string UploadError = "An error occurred while uploading the image.";

    /// <summary>
    /// Success message when profile image is updated.
    /// </summary>
    public const string UploadSuccess = "Your profile image has been updated successfully.";
}