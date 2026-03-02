using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenJustice.Generator.Configuration;

/// <summary>
/// Root configuration options for the OpenJustice Generator.
/// Combines all configuration sections into a single strongly-typed options class.
/// </summary>
public class GeneratorOptions
{
    /// <summary>
    /// Database connection settings.
    /// </summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// File path configuration.
    /// </summary>
    public FilePathsOptions FilePaths { get; set; } = new();

    /// <summary>
    /// Discovery service configuration.
    /// </summary>
    public DiscoveryOptions Discovery { get; set; } = new();

    /// <summary>
    /// Export service configuration.
    /// </summary>
    public ExportOptions Export { get; set; } = new();

    /// <summary>
    /// Torrent client configuration.
    /// </summary>
    public TorrentOptions? Torrent { get; set; }
}

/// <summary>
/// Database connection configuration.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// PostgreSQL connection string.
    /// </summary>
    [Required(ErrorMessage = "Database:ConnectionString is required")]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// PostgreSQL host (alternative to connection string).
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// PostgreSQL port.
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Database name.
    /// </summary>
    public string Name { get; set; } = "openjustice";

    /// <summary>
    /// Database username.
    /// </summary>
    public string Username { get; set; } = "postgres";

    /// <summary>
    /// Database password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Maximum retry count for transient failures.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;
}

/// <summary>
/// File paths configuration.
/// </summary>
public class FilePathsOptions
{
    /// <summary>
    /// Directory for exported SQL snapshots.
    /// </summary>
    [Required(ErrorMessage = "FilePaths:SnapshotDirectory is required")]
    public string SnapshotDirectory { get; set; } = "./snapshots";

    /// <summary>
    /// Directory for database backups.
    /// </summary>
    [Required(ErrorMessage = "FilePaths:BackupDirectory is required")]
    public string BackupDirectory { get; set; } = "./backups";

    /// <summary>
    /// Directory for exported files.
    /// </summary>
    public string ExportDirectory { get; set; } = "./exports";

    /// <summary>
    /// Temporary directory for processing.
    /// </summary>
    public string TempDirectory { get; set; } = "./temp";
}

/// <summary>
/// Export service configuration.
/// </summary>
public class ExportOptions
{
    /// <summary>
    /// Prefix for snapshot files.
    /// </summary>
    public string FilePrefix { get; set; } = "snapshot";

    /// <summary>
    /// Path to pg_dump executable.
    /// </summary>
    public string? PgDumpPath { get; set; }

    /// <summary>
    /// Whether to clean (drop) objects before creating.
    /// </summary>
    public bool Clean { get; set; } = true;

    /// <summary>
    /// Include IF EXISTS in DROP statements.
    /// </summary>
    public bool IfExists { get; set; } = true;

    /// <summary>
    /// Export ownership commands.
    /// </summary>
    public bool NoOwner { get; set; } = true;

    /// <summary>
    /// Export privilege commands (GRANT/REVOKE).
    /// </summary>
    public bool NoPrivileges { get; set; } = true;

    /// <summary>
    /// Export table data only (no schema).
    /// </summary>
    public bool DataOnly { get; set; } = false;

    /// <summary>
    /// Maximum timeout for export in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Validates GeneratorOptions at startup.
/// </summary>
public class GeneratorOptionsValidator : IValidateOptions<GeneratorOptions>
{
    public ValidateOptionsResult Validate(string? name, GeneratorOptions options)
    {
        var errors = new List<string>();

        // Validate database configuration
        if (string.IsNullOrWhiteSpace(options.Database?.ConnectionString))
        {
            if (string.IsNullOrWhiteSpace(options.Database?.Host))
            {
                errors.Add("Either Database:ConnectionString or Database:Host must be configured");
            }
        }

        // Validate file paths
        if (string.IsNullOrWhiteSpace(options.FilePaths?.SnapshotDirectory))
        {
            errors.Add("FilePaths:SnapshotDirectory is required");
        }

        if (string.IsNullOrWhiteSpace(options.FilePaths?.BackupDirectory))
        {
            errors.Add("FilePaths:BackupDirectory is required");
        }

        // Validate export settings
        if (string.IsNullOrWhiteSpace(options.Export?.FilePrefix))
        {
            errors.Add("Export:FilePrefix is required");
        }

        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
}
