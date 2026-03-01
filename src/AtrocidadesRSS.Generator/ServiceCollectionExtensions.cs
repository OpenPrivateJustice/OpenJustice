using AtrocidadesRSS.Generator.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AtrocidadesRSS.Generator;

/// <summary>
/// Extension methods for configuring AtrocidadesRSS services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add AtrocidadesRSS configuration to the service collection
    /// </summary>
    public static IServiceCollection AddAtrocidadesRssConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get sections
        var connectionStrings = configuration.GetSection("ConnectionStrings");
        var filePaths = configuration.GetSection("FilePaths");
        var torrent = configuration.GetSection("Torrent");

        // Register configuration options using PostConfigure
        services.PostConfigure<AppOptions>(options =>
        {
            options.ConnectionStrings = connectionStrings.Get<ConnectionStrings>() ?? new ConnectionStrings();
            options.FilePaths = filePaths.Get<FilePaths>() ?? new FilePaths();
            options.Torrent = torrent.Get<TorrentOptions>() ?? new TorrentOptions();
        });

        // Add configuration validation
        services.AddSingleton<IValidateOptions<AppOptions>, AppConfigurationValidator>();

        // Ensure directories exist at startup
        EnsureDirectoriesExist(filePaths);

        return services;
    }

    /// <summary>
    /// Validate configuration at startup - call after AddAtrocidadesRssConfiguration
    /// </summary>
    public static IServiceCollection ValidateAtrocidadesRssConfiguration(this IServiceCollection services)
    {
        // This triggers validation via IValidateOptions
        return services;
    }

    private static void EnsureDirectoriesExist(IConfigurationSection filePathsSection)
    {
        var filePaths = new FilePaths();
        filePathsSection.Bind(filePaths);
        
        EnsureDirectoryExists(filePaths.ExportDirectory);
        EnsureDirectoryExists(filePaths.BackupDirectory);
        EnsureDirectoryExists(filePaths.TempDirectory);
        EnsureDirectoryExists(filePaths.SnapshotDirectory);
    }

    private static void EnsureDirectoryExists(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch
            {
                // Ignore directory creation errors - may not have permissions at design time
            }
        }
    }
}
