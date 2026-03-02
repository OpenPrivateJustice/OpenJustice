namespace OpenJustice.Reader.Services.Sync;

/// <summary>
/// Result of a version check operation.
/// </summary>
public record VersionCheckResult(
    bool UpdateAvailable,
    string LocalVersion,
    string RemoteVersion,
    string? DownloadUrl,
    string? ErrorMessage
);

/// <summary>
/// Result of a download operation.
/// </summary>
public record DownloadResult(
    bool Success,
    string? LocalFilePath,
    long BytesDownloaded,
    string? ErrorMessage
);

/// <summary>
/// Result of a sync operation combining version check and download.
/// </summary>
public record SyncResult(
    bool Success,
    string Action,
    string? DownloadedVersion,
    string? LocalFilePath,
    string? ErrorMessage
);

/// <summary>
/// Service interface for checking available dataset versions.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Checks the remote version against local version.
    /// </summary>
    Task<VersionCheckResult> CheckVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current local version.
    /// </summary>
    string GetLocalVersion();

    /// <summary>
    /// Saves the local version after successful sync.
    /// </summary>
    Task SaveLocalVersionAsync(string version, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service interface for downloading dataset snapshots.
/// </summary>
public interface ITorrentSyncService
{
    /// <summary>
    /// Downloads the latest snapshot file.
    /// </summary>
    Task<DownloadResult> DownloadSnapshotAsync(string downloadUrl, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a complete sync: check version, download if needed.
    /// </summary>
    Task<SyncResult> SyncAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current sync status.
    /// </summary>
    SyncStatus GetStatus();

    /// <summary>
    /// Resets the sync status to idle.
    /// </summary>
    void ResetStatus();
    
    /// <summary>
    /// Gets the downloaded SQL content for import.
    /// </summary>
    byte[]? GetDownloadedSqlContent();
}

/// <summary>
/// Enumeration of possible sync states.
/// </summary>
public enum SyncState
{
    Idle,
    Checking,
    Downloading,
    Ready,
    Error
}

/// <summary>
/// Represents the current sync status.
/// </summary>
public record SyncStatus(
    SyncState State,
    string? LocalVersion,
    string? RemoteVersion,
    double Progress,
    string? ErrorMessage,
    string? LastAction
);
