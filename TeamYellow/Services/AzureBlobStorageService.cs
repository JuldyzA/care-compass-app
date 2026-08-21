using Azure.Storage.Blobs;
using Azure.Storage.Sas;

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
    /// Gets a read-only SAS URI for a blob that expires after the specified time.
    /// </summary>
    /// <param name="blobUri">The URI of the blob.</param>
    /// <param name="expiry">The time span for which the URI should be valid.</param>
    /// <returns>A temporary URI that can be used to access the blob.</returns>
    Task<string> GetReadUrlAsync(string blobUri, TimeSpan expiry);

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
    /// 
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
    /// Generates a short-lived, read-only SAS URI for a blob in the profile pictures container.
    /// </summary>
    /// <param name="blobUri">The absolute URI of the blob.</param>
    /// <param name="expiry">The duration for which the SAS URI should be valid.</param>
    /// <returns>
    /// A task representing the asynchronous operation, which returns the SAS URI as a string.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="blobUri"/> is null, empty, or not an absolute URI.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the blob URI does not belong to the configured Azure storage container
    /// or if the <c>BlobClient</c> is not configured with credentials capable of generating a SAS URI.
    /// </exception>
    public Task<string> GetReadUrlAsync(string blobUri, TimeSpan expiry)
{
    if (string.IsNullOrWhiteSpace(blobUri))
    {
        throw new ArgumentException("Blob URI cannot be empty.", nameof(blobUri));
    }

    if (!Uri.TryCreate(blobUri, UriKind.Absolute, out Uri? uri))
    {
        throw new ArgumentException("Invalid blob URI.", nameof(blobUri));
    }

    string containerPath = _containerClient.Uri.AbsolutePath.TrimEnd('/');
    string blobPath = uri.AbsolutePath;
_logger.LogInformation(
    "Configured container URI: {ContainerUri}",
    _containerClient.Uri);

_logger.LogInformation(
    "Blob URI being read: {BlobUri}",
    blobUri);
    if (!blobPath.StartsWith(containerPath + "/", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "The blob URI does not belong to the configured Azure container.");
    }

    string relativeBlobPath =
        Uri.UnescapeDataString(
            blobPath.Substring(containerPath.Length).TrimStart('/'));

    BlobClient blobClient =
        _containerClient.GetBlobClient(relativeBlobPath);

    if (!blobClient.CanGenerateSasUri)
    {
        throw new InvalidOperationException(
            "The BlobClient is not configured with credentials that can generate a SAS URI.");
    }

    Uri sasUri = blobClient.GenerateSasUri(
        Azure.Storage.Sas.BlobSasPermissions.Read,
        DateTimeOffset.UtcNow.Add(expiry));

    return Task.FromResult(sasUri.ToString());
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