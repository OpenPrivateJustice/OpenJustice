namespace AtrocidadesRSS.Generator.Services.Export;

/// <summary>
/// Service for managing snapshot file versioning.
/// </summary>
public interface ISnapshotVersionService
{
    /// <summary>
    /// Gets the next available version number for a snapshot.
    /// </summary>
    /// <param name="snapshotDirectory">Directory where snapshots are stored.</param>
    /// <param name="prefix">File prefix (default: "snapshot").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next version number.</returns>
    Task<int> GetNextVersionAsync(string snapshotDirectory, string prefix = "snapshot", CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a versioned file name.
    /// </summary>
    /// <param name="prefix">File prefix.</param>
    /// <param name="version">Version number.</param>
    /// <param name="extension">File extension (default: ".sql").</param>
    /// <returns>Versioned file name (e.g., "snapshot-v1.sql").</returns>
    string GenerateFileName(string prefix, int version, string extension = ".sql");

    /// <summary>
    /// Gets all existing versions in a directory.
    /// </summary>
    /// <param name="snapshotDirectory">Directory to scan.</param>
    /// <param name="prefix">File prefix to filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of version numbers found.</returns>
    Task<List<int>> GetExistingVersionsAsync(string snapshotDirectory, string prefix = "snapshot", CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of snapshot version management.
/// </summary>
public class SnapshotVersionService : ISnapshotVersionService
{
    private static readonly System.Text.RegularExpressions.Regex VersionPattern = new(
        @"^(?<prefix>[a-zA-Z_][a-zA-Z0-9_]*)-v(?<version>\d+)\.sql$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <inheritdoc/>
    public Task<int> GetNextVersionAsync(string snapshotDirectory, string prefix = "snapshot", CancellationToken cancellationToken = default)
    {
        var versions = GetExistingVersionsAsync(snapshotDirectory, prefix, cancellationToken).GetAwaiter().GetResult();
        
        var nextVersion = versions.Count > 0 ? versions.Max() + 1 : 1;
        return Task.FromResult(nextVersion);
    }

    /// <inheritdoc/>
    public string GenerateFileName(string prefix, int version, string extension = ".sql")
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix cannot be empty.", nameof(prefix));
        
        if (version < 1)
            throw new ArgumentException("Version must be greater than 0.", nameof(version));

        return $"{prefix}-v{version}{extension}";
    }

    /// <inheritdoc/>
    public Task<List<int>> GetExistingVersionsAsync(string snapshotDirectory, string prefix = "snapshot", CancellationToken cancellationToken = default)
    {
        var versions = new List<int>();

        if (!Directory.Exists(snapshotDirectory))
        {
            return Task.FromResult(versions);
        }

        var files = Directory.GetFiles(snapshotDirectory, "*.sql", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var match = VersionPattern.Match(fileName);

            if (match.Success)
            {
                var filePrefix = match.Groups["prefix"].Value;
                if (filePrefix.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(match.Groups["version"].Value, out var version))
                    {
                        versions.Add(version);
                    }
                }
            }
        }

        return Task.FromResult(versions.OrderBy(v => v).ToList());
    }
}
