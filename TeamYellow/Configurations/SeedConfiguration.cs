namespace TeamYellow.Configurations;

/// <summary>
/// Provides centralized access to data seeding configuration settings.
/// </summary>
public class SeedConfiguration
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedConfiguration"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public SeedConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the default password for seeding test users.
    /// </summary>
    public string DefaultPassword =>
        _configuration["Seed:DefaultPassword"] ?? throw new InvalidOperationException("Seed password not configured");

    /// <summary>
    /// Gets the password for the demo counsellor account.
    /// </summary>
    public string DemoPassword =>
        _configuration["Seed:DemoPassword"] ?? throw new InvalidOperationException("Seed demo password not configured");
}