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
    /// <param name="containerName">The container name in blob storage.</param>
    /// <returns>The URI of the uploaded blob.</returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName);

    /// <summary>
    /// Deletes a file from Azure Blob Storage.
    /// </summary>
    /// <param name="blobUri">The URI of the blob to delete.</param>
    /// <param name="containerName">The container name in blob storage.</param>
    /// <returns><c>true</c> if the deletion was successful; otherwise <c>false</c>.</returns>
    Task<bool> DeleteFileAsync(string blobUri, string containerName);
}

/// <summary>
/// Implementation of Azure Blob Storage service.
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
    /// Uploads a file stream to the specified blob storage container.
    /// </summary>
    /// <param name="fileStream">The stream containing the file data to upload.</param>
    /// <param name="fileName">The name to assign to the file in storage.</param>
    /// <param name="containerName">The name of the target container.</param>
    /// <returns>A task that represents the asynchronous operation, containing the URI of the uploaded blob.</returns>
    /// <exception cref="Exception">Thrown when the upload process fails. Errors are logged before re-throwing.</exception>

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName)
    {
        try
        {
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);

            await blobClient.UploadAsync(fileStream, overwrite: true);

            _logger.LogInformation("File '{FileName}' uploaded successfully to container '{ContainerName}'", fileName, containerName);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file '{FileName}' to container '{ContainerName}'", fileName, containerName);
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from the blob storage container using its URI.
    /// </summary>
    /// <param name="blobUri">The full URI of the blob to be deleted.</param>
    /// <param name="containerName">The name of the container where the file is stored.</param>
    /// <returns>A task representing the asynchronous operation, returning <see langword="true"/> if the deletion was successful; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This method extracts the filename from the URI and attempts to delete it. 
    /// Any exceptions encountered during the process are caught and logged.
    /// </remarks>
    public async Task<bool> DeleteFileAsync(string blobUri, string containerName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(blobUri))
            {
                return false;
            }

            string fileName = Path.GetFileName(new Uri(blobUri).AbsolutePath);
            BlobClient blobClient = _containerClient.GetBlobClient(fileName);

            await blobClient.DeleteAsync();

            _logger.LogInformation("File '{FileName}' deleted successfully from container '{ContainerName}'", fileName, containerName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from container '{ContainerName}'", containerName);
            return false;
        }
    }
}