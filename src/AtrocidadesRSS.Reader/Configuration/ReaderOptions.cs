using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace AtrocidadesRSS.Reader.Configuration;

/// <summary>
/// Root configuration options for the AtrocidadesRSS Reader.
/// Contains all runtime settings for the client-side reader application.
/// </summary>
public class ReaderOptions
{
    /// <summary>
    /// Torrent client configuration for decentralized distribution.
    /// </summary>
    public TorrentOptions Torrent { get; set; } = new();

    /// <summary>
    /// Snapshot metadata for version tracking.
    /// </summary>
    public SnapshotOptions Snapshot { get; set; } = new();

    /// <summary>
    /// Local database file settings for offline access.
    /// </summary>
    public LocalDbOptions LocalDb { get; set; } = new();
}

/// <summary>
/// Torrent client configuration.
/// </summary>
public class TorrentOptions
{
    /// <summary>
    /// List of torrent tracker URLs for announce.
    /// </summary>
    [Required(ErrorMessage = "At least one torrent tracker URL is required")]
    public List<string> TrackerUrls { get; set; } = new()
    {
        "udp://tracker.opentrackr.org:1337/announce"
    };

    /// <summary>
    /// DHT (Distributed Hash Table) enabled.
    /// </summary>
    public bool DhtEnabled { get; set; } = true;

    /// <summary>
    /// Peer exchange enabled.
    /// </summary>
    public bool PeerExchangeEnabled { get; set; } = true;

    /// <summary>
    /// Maximum connections to maintain.
    /// </summary>
    [Range(10, 500, ErrorMessage = "MaxConnections must be between 10 and 500")]
    public int MaxConnections { get; set; } = 100;
}

/// <summary>
/// Snapshot version metadata configuration.
/// </summary>
public class SnapshotOptions
{
    /// <summary>
    /// Base URL for fetching snapshot versions.
    /// </summary>
    [Required(ErrorMessage = "Snapshot:VersionEndpoint is required")]
    public string VersionEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for downloading snapshot files.
    /// </summary>
    [Required(ErrorMessage = "Snapshot:DownloadBaseUrl is required")]
    public string DownloadBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Current local snapshot version.
    /// </summary>
    public string LocalVersion { get; set; } = "0";

    /// <summary>
    /// Auto-check for updates on startup.
    /// </summary>
    public bool AutoCheckUpdates { get; set; } = true;
}

/// <summary>
/// Local SQLite database file configuration.
/// </summary>
public class LocalDbOptions
{
    /// <summary>
    /// Path to the local SQLite database file.
    /// </summary>
    [Required(ErrorMessage = "LocalDb:DatabasePath is required")]
    public string DatabasePath { get; set; } = "atrocidadesrss.db";

    /// <summary>
    /// Enable WAL mode for better concurrent access.
    /// </summary>
    public bool WalMode { get; set; } = true;

    /// <summary>
    /// Cache size in pages.
    /// </summary>
    public int CacheSize { get; set; } = -2000;

    /// <summary>
    /// Synchronous mode (OFF, NORMAL, FULL).
    /// </summary>
    public string Synchronous { get; set; } = "NORMAL";

    /// <summary>
    /// Journal mode (DELETE, TRUNCATE, PERSIST, MEMORY, WAL).
    /// </summary>
    public string JournalMode { get; set; } = "WAL";
}

/// <summary>
/// Validates ReaderOptions at startup.
/// </summary>
public class ReaderOptionsValidator : IValidateOptions<ReaderOptions>
{
    public ValidateOptionsResult Validate(string? name, ReaderOptions options)
    {
        var errors = new List<string>();

        // Validate torrent configuration
        if (options.Torrent == null)
        {
            errors.Add("Torrent configuration is required");
        }
        else if (options.Torrent.TrackerUrls == null || options.Torrent.TrackerUrls.Count == 0)
        {
            errors.Add("At least one torrent tracker URL is required");
        }

        // Validate snapshot configuration
        if (options.Snapshot == null)
        {
            errors.Add("Snapshot configuration is required");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(options.Snapshot.VersionEndpoint))
            {
                errors.Add("Snapshot:VersionEndpoint is required");
            }

            if (string.IsNullOrWhiteSpace(options.Snapshot.DownloadBaseUrl))
            {
                errors.Add("Snapshot:DownloadBaseUrl is required");
            }
        }

        // Validate local database configuration
        if (options.LocalDb == null)
        {
            errors.Add("LocalDb configuration is required");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(options.LocalDb.DatabasePath))
            {
                errors.Add("LocalDb:DatabasePath is required");
            }
        }

        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
}
