namespace TeamYellow.Configurations;

/// <summary>
/// Provides centralized access to Azure Storage configuration settings.
/// </summary>
public class AzureStorageConfiguration
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageConfiguration"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public AzureStorageConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the Azure Storage connection string.
    /// </summary>
    public string ConnectionString =>
        _configuration["AzureStorage:ConnectionString"] ?? throw new InvalidOperationException("Azure Storage connection string not found.");

    /// <summary>
    /// Gets the Azure Storage account name.
    /// </summary>
    public string AccountName =>
        _configuration["AzureStorage:AccountName"] ?? throw new InvalidOperationException("Azure Storage account name not found.");

    /// <summary>
    /// Gets the Azure Storage account key.
    /// </summary>
    public string AccountKey =>
        _configuration["AzureStorage:AccountKey"] ?? throw new InvalidOperationException("Azure Storage account key not found.");

    /// <summary>
    /// Gets the profile pictures container name.
    /// </summary>
    public string ProfilePicturesContainer =>
        _configuration["AzureStorage:ProfilePicturesContainer"] ?? throw new InvalidOperationException("Azure Storage container not found.");
}