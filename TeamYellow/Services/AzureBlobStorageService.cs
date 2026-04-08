using Azure.Storage.Blobs;

namespace TeamYellow.Services;

/// <summary>
/// Service for managing file uploads to Azure Blob Storage.
/// </summary>
public interface IAzureBlobStorageService
{
    /// <summary>
    /// Uploads a file to Azure Blob Storage.
    /// </summary>
    /// <param name="fileStream">The file stream to upload.</param>
    /// <param name="fileName">The name of the file in blob storage.</param>
    /// <returns>The URI of the uploaded blob.</returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Deletes a file from Azure Blob Storage.
    /// </summary>
    /// <param name="blobUri">The URI of the blob to delete.</param>
    /// <returns><c>true</c> if the deletion was successful; otherwise <c>false</c>.</returns>
    Task<bool> DeleteFileAsync(string blobUri);
}

/// <summary>
/// Implementation of Azure Blob Storage service for profile pictures.
/// </summary>
public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureBlobStorageService"/> class.
    /// </summary>
    /// <param name="blobContainerClient">The blob container client configured for the profile pictures container.</param>
    /// <param name="logger">Logs service operations.</param>
    public AzureBlobStorageService(BlobContainerClient blobContainerClient, ILogger<AzureBlobStorageService> logger)
    {
        _containerClient = blobContainerClient;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a file stream to the profile pictures blob storage container.
    /// </summary>
    /// <param name="fileStream">The stream containing the file data to upload.</param>
    /// <param name="fileName">The name to assign to the file in storage.</param>
    /// <returns>A task that represents the asynchronous operation, containing the URI of the uploaded blob.</returns>
    /// <exception cref="Exception">Thrown when the upload process fails. Errors are logged before re-throwing.</exception>
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);

            await blobClient.UploadAsync(fileStream, overwrite: true);

            _logger.LogInformation("File '{FileName}' uploaded successfully to blob storage.", fileName);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file '{FileName}' to blob storage.", fileName);
            throw;
        }
    }

    /// <summary>
    /// Attempts to delete a file from the configured profile pictures blob storage container using its URI.
    /// </summary>
    /// <param name="blobUri">The absolute URI of the blob file to delete.</param>
    /// <returns>
    /// A task representing the asynchronous operation. Returns <see langword="true"/> if the blob existed
    /// in the configured container and was deleted; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    /// <item><description>Returns <see langword="false"/> if <paramref name="blobUri"/> is null, empty, or whitespace.</description></item>
    /// <item><description>Returns <see langword="false"/> if <paramref name="blobUri"/> is not a valid absolute URI.</description></item>
    /// <item><description>Returns <see langword="false"/> if the URI does not belong to the configured Azure Blob Storage container.</description></item>
    /// <item><description>Extracts the file name from the URI path and attempts deletion using <c>DeleteIfExistsAsync()</c>.</description></item>
    /// <item><description>Logs and suppresses exceptions, returning <see langword="false"/> if an error occurs.</description></item>
    /// </list>
    /// </remarks>
    public async Task<bool> DeleteFileAsync(string blobUri)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(blobUri))
            {
                return false;
            }

            if (!Uri.TryCreate(blobUri, UriKind.Absolute, out Uri? uri))
            {
                _logger.LogWarning("Skipping blob delete because the URL is invalid: {BlobUrl}", blobUri);
                return false;
            }

            if (!string.Equals(uri.Host, _containerClient.Uri.Host, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Skipping blob delete because the file is not stored in the configured Azure container. URL: {BlobUrl}", blobUri);
                return false;
            }

            string containerPath = _containerClient.Uri.AbsolutePath.TrimEnd('/');
            string blobPath = uri.AbsolutePath;

            if (!blobPath.StartsWith(containerPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Skipping blob delete because the file is not stored under the configured Azure container path. URL: {BlobUrl}", blobUri);
                return false;
            }

            string relativeBlobPath = Uri.UnescapeDataString(blobPath.Substring(containerPath.Length).TrimStart('/'));

            if (string.IsNullOrWhiteSpace(relativeBlobPath))
            {
                _logger.LogWarning("Skipping blob delete because the resolved blob path is empty. URL: {BlobUrl}", blobUri);
                return false;
            }

            BlobClient blobClient = _containerClient.GetBlobClient(relativeBlobPath);
            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                _logger.LogInformation("Blob '{BlobPath}' deleted successfully from blob storage.", relativeBlobPath);
            }
            else
            {
                _logger.LogInformation("Blob delete skipped because blob '{BlobPath}' does not exist in blob storage.", relativeBlobPath);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from blob storage.");
            return false;
        }
    }
}