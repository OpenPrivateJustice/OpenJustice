using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AtrocidadesRSS.Generator.Services.Export;

/// <summary>
/// Configuration options for snapshot export.
/// </summary>
public class SnapshotExportOptions
{
    /// <summary>
    /// Directory to save snapshot files.
    /// </summary>
    public string SnapshotDirectory { get; set; } = "./snapshots";

    /// <summary>
    /// Database connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// PostgreSQL host (overrides connection string if provided).
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// PostgreSQL port.
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// PostgreSQL database name.
    /// </summary>
    public string Database { get; set; } = "atrocidadesrss";

    /// <summary>
    /// PostgreSQL username.
    /// </summary>
    public string Username { get; set; } = "postgres";

    /// <summary>
    /// PostgreSQL password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Prefix for snapshot files.
    /// </summary>
    public string FilePrefix { get; set; } = "snapshot";

    /// <summary>
    /// pg_dump executable path. If empty, uses system PATH.
    /// </summary>
    public string? PgDumpPath { get; set; }
}

/// <summary>
/// Result of a snapshot export operation.
/// </summary>
public class SnapshotExportResult
{
    /// <summary>
    /// Whether the export was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Path to the created snapshot file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Version number of the exported snapshot.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Error message if export failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Size of the exported file in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Duration of the export operation.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Service for exporting PostgreSQL database snapshots.
/// </summary>
public interface ISnapshotExportService
{
    /// <summary>
    /// Exports the database to a SQL snapshot file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Export result with file path and version.</returns>
    Task<SnapshotExportResult> ExportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if pg_dump is available on the system.
    /// </summary>
    /// <returns>True if pg_dump is available.</returns>
    bool IsPgDumpAvailable();
}

/// <summary>
/// Implementation of PostgreSQL snapshot export using pg_dump.
/// </summary>
public class SnapshotExportService : ISnapshotExportService
{
    private readonly SnapshotExportOptions _options;
    private readonly ISnapshotVersionService _versionService;
    private readonly ILogger<SnapshotExportService> _logger;

    public SnapshotExportService(
        IOptions<SnapshotExportOptions> options,
        ISnapshotVersionService versionService,
        ILogger<SnapshotExportService> logger)
    {
        _options = options.Value;
        _versionService = versionService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SnapshotExportResult> ExportAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new SnapshotExportResult();

        try
        {
            // Validate pg_dump is available
            if (!IsPgDumpAvailable())
            {
                result.Success = false;
                result.ErrorMessage = "pg_dump is not available. Please ensure PostgreSQL client tools are installed.";
                _logger.LogError("pg_dump not found in system PATH");
                return result;
            }

            // Ensure snapshot directory exists
            if (!Directory.Exists(_options.SnapshotDirectory))
            {
                Directory.CreateDirectory(_options.SnapshotDirectory);
                _logger.LogInformation("Created snapshot directory: {Directory}", _options.SnapshotDirectory);
            }

            // Get next version number
            var version = await _versionService.GetNextVersionAsync(_options.SnapshotDirectory, _options.FilePrefix, cancellationToken);
            var fileName = _versionService.GenerateFileName(_options.FilePrefix, version);
            var filePath = Path.Combine(_options.SnapshotDirectory, fileName);

            // Build pg_dump arguments
            var args = BuildPgDumpArguments();

            _logger.LogInformation("Starting database export to {FilePath}", filePath);

            // Execute pg_dump
            var pgDumpResult = await ExecutePgDumpAsync(args, filePath, cancellationToken);

            if (!pgDumpResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = pgDumpResult.ErrorMessage;
                result.Version = version;
                
                // Clean up partial file if it exists
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }
                
                return result;
            }

            // Verify file was created
            if (!File.Exists(filePath))
            {
                result.Success = false;
                result.ErrorMessage = "Snapshot file was not created.";
                return result;
            }

            // Get file info
            var fileInfo = new FileInfo(filePath);
            result.Success = true;
            result.FilePath = filePath;
            result.Version = version;
            result.FileSizeBytes = fileInfo.Length;
            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Export completed: {FilePath}, Version: {Version}, Size: {Size} bytes",
                filePath, version, result.FileSizeBytes);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Export failed: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Snapshot export failed");
            return result;
        }
    }

    /// <inheritdoc/>
    public bool IsPgDumpAvailable()
    {
        var pgDumpPath = _options.PgDumpPath;
        
        if (!string.IsNullOrEmpty(pgDumpPath))
        {
            return File.Exists(pgDumpPath) || ExecutableInPath(pgDumpPath);
        }

        // Check common Windows locations first
        if (OperatingSystem.IsWindows())
        {
            var commonPaths = new[]
            {
                @"C:\Program Files\PostgreSQL\17\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path)) return true;
            }
        }

        // Try to find in PATH
        return ExecutableInPath("pg_dump") || 
               (OperatingSystem.IsWindows() && ExecutableInPath("pg_dump.exe"));
    }

    private static bool ExecutableInPath(string executable)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var paths = pathEnv.Split(Path.PathSeparator);

        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, executable);
            if (File.Exists(fullPath)) return true;
        }

        return false;
    }

    private string BuildPgDumpArguments()
    {
        // Build connection string from options or use direct connection string
        string connectionString;

        if (!string.IsNullOrEmpty(_options.Host))
        {
            var password = _options.Password ?? Environment.GetEnvironmentVariable("PGPASSWORD") ?? "";
            connectionString = $"host={_options.Host} port={_options.Port} dbname={_options.Database} user={_options.Username} password={password}";
        }
        else if (!string.IsNullOrEmpty(_options.ConnectionString))
        {
            connectionString = _options.ConnectionString;
        }
        else
        {
            throw new InvalidOperationException("Either Host or ConnectionString must be configured for snapshot export.");
        }

        // Build pg_dump command arguments
        var sb = new StringBuilder();
        
        // Add connection parameters
        sb.Append($"-h {GetConnectionHost(connectionString)} ");
        sb.Append($"-p {GetConnectionPort(connectionString)} ");
        sb.Append($"-U {GetConnectionUser(connectionString)} ");
        sb.Append($"-d {GetConnectionDatabase(connectionString)} ");
        
        // Add export options
        sb.Append("--clean ");
        sb.Append("--if-exists ");
        sb.Append("--no-owner ");
        sb.Append("--no-privileges ");
        
        // Output format (plain SQL)
        sb.Append("-f "); // file output follows

        return sb.ToString().Trim();
    }

    private async Task<SnapshotExportResult> ExecutePgDumpArgumentsAndFileAsync(string argsWithoutFile, string filePath, CancellationToken cancellationToken)
    {
        var result = new SnapshotExportResult();

        try
        {
            var pgDumpPath = _options.PgDumpPath ?? (OperatingSystem.IsWindows() ? "pg_dump.exe" : "pg_dump");
            var fullArgs = $"{argsWithoutFile} \"{filePath}\"";

            _logger.LogDebug("Executing: {Tool} {Args}", pgDumpPath, fullArgs);

            var startTime = DateTime.UtcNow;
            var processStartInfo = new ProcessStartInfo
            {
                FileName = pgDumpPath,
                Arguments = fullArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _options.SnapshotDirectory
            };

            // Set environment variable for password
            var connectionString = !string.IsNullOrEmpty(_options.Host) 
                ? $"host={_options.Host} port={_options.Port} dbname={_options.Database} user={_options.Username} password={_options.Password}"
                : _options.ConnectionString;
            
            var password = ExtractPassword(connectionString);
            if (!string.IsNullOrEmpty(password))
            {
                processStartInfo.Environment["PGPASSWORD"] = password;
            }

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            var timedOut = !process.WaitForExit(300000); // 5 minute timeout

            if (timedOut)
            {
                process.Kill(true);
                result.Success = false;
                result.ErrorMessage = "pg_dump timed out after 5 minutes.";
                _logger.LogError("pg_dump timed out");
                return result;
            }

            if (process.ExitCode != 0)
            {
                result.Success = false;
                result.ErrorMessage = $"pg_dump failed with exit code {process.ExitCode}: {error}";
                _logger.LogError("pg_dump failed: {Error}", error);
                return result;
            }

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to execute pg_dump: {ex.Message}";
            _logger.LogError(ex, "Failed to execute pg_dump");
            return result;
        }
    }

    private async Task<SnapshotExportResult> ExecutePgDumpAsync(string argsWithoutFile, string filePath, CancellationToken cancellationToken)
    {
        var result = new SnapshotExportResult();

        try
        {
            var pgDumpPath = _options.PgDumpPath ?? (OperatingSystem.IsWindows() ? "pg_dump.exe" : "pg_dump");
            var fullArgs = $"{argsWithoutFile} \"{filePath}\"";

            _logger.LogDebug("Executing: {Tool} {Args}", pgDumpPath, fullArgs);

            var startTime = DateTime.UtcNow;
            var processStartInfo = new ProcessStartInfo
            {
                FileName = pgDumpPath,
                Arguments = fullArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _options.SnapshotDirectory
            };

            // Set environment variable for password
            var connectionString = !string.IsNullOrEmpty(_options.Host) 
                ? $"host={_options.Host} port={_options.Port} dbname={_options.Database} user={_options.Username} password={_options.Password}"
                : _options.ConnectionString;
            
            var password = ExtractPassword(connectionString);
            if (!string.IsNullOrEmpty(password))
            {
                processStartInfo.Environment["PGPASSWORD"] = password;
            }

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            var timedOut = !process.WaitForExit(300000); // 5 minute timeout

            if (timedOut)
            {
                process.Kill(true);
                result.Success = false;
                result.ErrorMessage = "pg_dump timed out after 5 minutes.";
                _logger.LogError("pg_dump timed out");
                return result;
            }

            if (process.ExitCode != 0)
            {
                result.Success = false;
                result.ErrorMessage = $"pg_dump failed with exit code {process.ExitCode}: {error}";
                _logger.LogError("pg_dump failed: {Error}", error);
                return result;
            }

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to execute pg_dump: {ex.Message}";
            _logger.LogError(ex, "Failed to execute pg_dump");
            return result;
        }
    }

    private static string GetConnectionHost(string connectionString)
    {
        return ExtractConnectionPart(connectionString, "host") ?? "localhost";
    }

    private static string GetConnectionPort(string connectionString)
    {
        return ExtractConnectionPart(connectionString, "port") ?? "5432";
    }

    private static string GetConnectionUser(string connectionString)
    {
        return ExtractConnectionPart(connectionString, "user") ?? "postgres";
    }

    private static string GetConnectionDatabase(string connectionString)
    {
        return ExtractConnectionPart(connectionString, "dbname") ?? "atrocidadesrss";
    }

    private static string? ExtractPassword(string connectionString)
    {
        return ExtractConnectionPart(connectionString, "password");
    }

    private static string? ExtractConnectionPart(string connectionString, string key)
    {
        if (string.IsNullOrEmpty(connectionString)) return null;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2 && keyValue[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return keyValue[1].Trim();
            }
        }

        // Also try space-separated format
        parts = connectionString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2 && keyValue[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return keyValue[1].Trim();
            }
        }

        return null;
    }
}
