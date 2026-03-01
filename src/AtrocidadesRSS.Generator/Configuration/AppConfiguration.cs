using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace AtrocidadesRSS.Generator.Configuration;

/// <summary>
/// Application configuration options bound from appsettings.json
/// </summary>
public class AppOptions
{
    /// <summary>
    /// Database connection strings
    /// </summary>
    public ConnectionStrings ConnectionStrings { get; set; } = new();

    /// <summary>
    /// File path configuration
    /// </summary>
    public FilePaths FilePaths { get; set; } = new();

    /// <summary>
    /// Torrent client configuration
    /// </summary>
    public TorrentOptions Torrent { get; set; } = new();
}

/// <summary>
/// Database connection string configuration
/// </summary>
public class ConnectionStrings
{
    /// <summary>
    /// Default PostgreSQL connection string
    /// </summary>
    [Required(ErrorMessage = "ConnectionStrings:DefaultConnection is required")]
    public string DefaultConnection { get; set; } = string.Empty;
}

/// <summary>
/// File path configuration for exports, backups, etc.
/// </summary>
public class FilePaths
{
    /// <summary>
    /// Directory for exported SQL snapshots
    /// </summary>
    [Required]
    public string ExportDirectory { get; set; } = "./exports";

    /// <summary>
    /// Directory for database backups
    /// </summary>
    [Required]
    public string BackupDirectory { get; set; } = "./backups";

    /// <summary>
    /// Temporary directory for processing
    /// </summary>
    public string TempDirectory { get; set; } = "./temp";

    /// <summary>
    /// Directory for SQL snapshot files
    /// </summary>
    public string SnapshotDirectory { get; set; } = "./snapshots";
}

/// <summary>
/// Torrent client configuration
/// </summary>
public class TorrentOptions
{
    /// <summary>
    /// List of torrent tracker URLs
    /// </summary>
    [Required]
    public List<string> TrackerUrls { get; set; } = new();

    /// <summary>
    /// Port for torrent client to listen on
    /// </summary>
    [Range(1024, 65535, ErrorMessage = "ListenPort must be between 1024 and 65535")]
    public int ListenPort { get; set; } = 6881;

    /// <summary>
    /// Maximum download speed in bytes per second (0 = unlimited)
    /// </summary>
    public long MaxDownloadSpeed { get; set; }

    /// <summary>
    /// Maximum upload speed in bytes per second (0 = unlimited)
    /// </summary>
    public long MaxUploadSpeed { get; set; }

    /// <summary>
    /// Maximum number of connections per torrent
    /// </summary>
    [Range(1, 1000)]
    public int MaxConnections { get; set; } = 100;
}

/// <summary>
/// Validates configuration on startup
/// </summary>
public class AppConfigurationValidator : IValidateOptions<AppOptions>
{
    public ValidateOptionsResult Validate(string? name, AppOptions options)
    {
        var errors = new List<string>();

        // Validate connection string
        if (string.IsNullOrWhiteSpace(options.ConnectionStrings?.DefaultConnection))
        {
            errors.Add("ConnectionStrings:DefaultConnection is required");
        }

        // Validate file paths
        if (string.IsNullOrWhiteSpace(options.FilePaths?.ExportDirectory))
        {
            errors.Add("FilePaths:ExportDirectory is required");
        }

        if (string.IsNullOrWhiteSpace(options.FilePaths?.BackupDirectory))
        {
            errors.Add("FilePaths:BackupDirectory is required");
        }

        // Validate torrent settings
        if (options.Torrent?.TrackerUrls == null || options.Torrent.TrackerUrls.Count == 0)
        {
            errors.Add("Torrent:TrackerUrls is required");
        }

        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
}
