using System.Text;
using System.IO.Compression;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using AtrocidadesRSS.Reader.Configuration;
using AtrocidadesRSS.Reader.Services.Data;

namespace AtrocidadesRSS.Reader.Services.Sync;

/// <summary>
/// Service for downloading dataset snapshots via torrent or HTTP fallback.
/// In a Blazor WASM environment, this uses HTTP download with torrent abstraction for future implementation.
/// </summary>
public class TorrentSyncService : ITorrentSyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ReaderOptions _options;
    private readonly ILogger<TorrentSyncService> _logger;
    private readonly IVersionService _versionService;
    private readonly ILocalCaseStore _caseStore;

    private SyncStatus _currentStatus;
    
    // Store downloaded SQL content for import
    private byte[]? _downloadedSqlContent;

    public TorrentSyncService(
        IHttpClientFactory httpClientFactory,
        IOptions<ReaderOptions> options,
        ILogger<TorrentSyncService> logger,
        IVersionService versionService,
        ILocalCaseStore caseStore)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _versionService = versionService;
        _caseStore = caseStore;

        _currentStatus = new SyncStatus(
            State: SyncState.Idle,
            LocalVersion: _versionService.GetLocalVersion(),
            RemoteVersion: null,
            Progress: 0,
            ErrorMessage: null,
            LastAction: null);
    }

    /// <inheritdoc/>
    public SyncStatus GetStatus() => _currentStatus;

    /// <inheritdoc/>
    public void ResetStatus()
    {
        _currentStatus = _currentStatus with
        {
            State = SyncState.Idle,
            Progress = 0,
            ErrorMessage = null,
            LastAction = null
        };
        _downloadedSqlContent = null;
    }

    /// <summary>
    /// Gets the downloaded SQL content for import.
    /// </summary>
    public byte[]? GetDownloadedSqlContent() => _downloadedSqlContent;

    /// <inheritdoc/>
    public async Task<SyncResult> SyncAsync(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting sync operation");

        try
        {
            // Step 1: Check for updates
            UpdateStatus(SyncState.Checking, "Checking for updates...");
            
            var versionResult = await _versionService.CheckVersionAsync(cancellationToken);
            
            if (!string.IsNullOrEmpty(versionResult.ErrorMessage))
            {
                return new SyncResult(
                    Success: false,
                    Action: "version_check",
                    DownloadedVersion: null,
                    LocalFilePath: null,
                    ErrorMessage: versionResult.ErrorMessage);
            }

            _currentStatus = _currentStatus with { RemoteVersion = versionResult.RemoteVersion };

            // If no update available, we're done
            if (!versionResult.UpdateAvailable)
            {
                UpdateStatus(SyncState.Ready, "Already up to date");
                
                return new SyncResult(
                    Success: true,
                    Action: "version_check",
                    DownloadedVersion: _versionService.GetLocalVersion(),
                    LocalFilePath: null,
                    ErrorMessage: null);
            }

            // Step 2: Download the new version
            if (string.IsNullOrEmpty(versionResult.DownloadUrl))
            {
                var error = "No download URL available";
                UpdateStatus(SyncState.Error, error);
                
                return new SyncResult(
                    Success: false,
                    Action: "download",
                    DownloadedVersion: null,
                    LocalFilePath: null,
                    ErrorMessage: error);
            }

            UpdateStatus(SyncState.Downloading, $"Downloading v{versionResult.RemoteVersion}...");
            
            var downloadResult = await DownloadSnapshotAsync(
                versionResult.DownloadUrl, 
                progress, 
                cancellationToken);

            if (!downloadResult.Success)
            {
                return new SyncResult(
                    Success: false,
                    Action: "download",
                    DownloadedVersion: null,
                    LocalFilePath: null,
                    ErrorMessage: downloadResult.ErrorMessage);
            }

            // Step 3: Import the downloaded SQL into local store
            if (_downloadedSqlContent != null && _downloadedSqlContent.Length > 0)
            {
                UpdateStatus(SyncState.Downloading, "Importing snapshot...");
                
                var sqlContent = Encoding.UTF8.GetString(_downloadedSqlContent);
                var importResult = await _caseStore.ImportSnapshotAsync(sqlContent, cancellationToken: cancellationToken);
                
                if (!importResult.Success)
                {
                    var importError = importResult.ErrorMessage ?? "Unknown import error";
                    UpdateStatus(SyncState.Error, $"Import failed: {importError}");
                    
                    return new SyncResult(
                        Success: false,
                        Action: "import",
                        DownloadedVersion: versionResult.RemoteVersion,
                        LocalFilePath: downloadResult.LocalFilePath,
                        ErrorMessage: $"Download succeeded but import failed: {importError}");
                }
                
                _logger.LogInformation(
                    "Import complete: {Records} records imported",
                    importResult.RecordsImported);
            }
            else
            {
                _logger.LogWarning("No SQL content available for import");
            }

            // Step 4: Save the new local version only after successful import
            await _versionService.SaveLocalVersionAsync(versionResult.RemoteVersion, cancellationToken);
            
            _currentStatus = _currentStatus with { LocalVersion = versionResult.RemoteVersion };
            
            UpdateStatus(SyncState.Ready, "Sync complete");
            
            _logger.LogInformation(
                "Sync complete: version {Version}, {Bytes} bytes",
                versionResult.RemoteVersion,
                downloadResult.BytesDownloaded);

            return new SyncResult(
                Success: true,
                Action: "sync",
                DownloadedVersion: versionResult.RemoteVersion,
                LocalFilePath: downloadResult.LocalFilePath,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync operation failed");
            UpdateStatus(SyncState.Error, ex.Message);
            
            return new SyncResult(
                Success: false,
                Action: "sync",
                DownloadedVersion: null,
                LocalFilePath: null,
                ErrorMessage: ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> DownloadSnapshotAsync(
        string downloadUrl, 
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading snapshot from {Url}", downloadUrl);

        try
        {
            // For Blazor WASM, we download via HTTP
            // In a full implementation, this could integrate with browser torrent clients
            // like WebTorrent.js via JS interop

            using var httpClient = _httpClientFactory.CreateClient("GeneratorHistoryApiClient");
            using var response = await httpClient.GetAsync(
                downloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new DownloadResult(
                    Success: false,
                    LocalFilePath: null,
                    BytesDownloaded: 0,
                    ErrorMessage: $"Download failed with status {response.StatusCode}");
            }

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes > 0;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            
            // For browser environment, we'd use JS interop to save to IndexedDB
            // Here we simulate with in-memory storage
            using var memoryStream = new MemoryStream();
            
            var buffer = new byte[8192];
            var totalRead = 0L;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (canReportProgress)
                {
                    var progressPercent = (double)totalRead / totalBytes * 100;
                    progress?.Report(progressPercent);
                    
                    UpdateStatus(SyncState.Downloading, 
                        $"Downloading... {FormatBytes(totalRead)} / {FormatBytes(totalBytes)}");
                }
            }

            // Decompress if gzipped
            var finalData = await TryDecompressGzipAsync(memoryStream.ToArray());
            
            // Store for import (in production, would persist to IndexedDB via JS interop)
            _downloadedSqlContent = finalData;
            
            var localPath = $"snapshots/v{_versionService.GetLocalVersion()}.sql";
            
            _logger.LogInformation(
                "Download complete: {Bytes} bytes saved to {Path}",
                finalData.Length,
                localPath);

            return new DownloadResult(
                Success: true,
                LocalFilePath: localPath,
                BytesDownloaded: finalData.Length,
                ErrorMessage: null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Download failed");
            return new DownloadResult(
                Success: false,
                LocalFilePath: null,
                BytesDownloaded: 0,
                ErrorMessage: $"Network error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Download was cancelled");
            return new DownloadResult(
                Success: false,
                LocalFilePath: null,
                BytesDownloaded: 0,
                ErrorMessage: "Download cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected download error");
            return new DownloadResult(
                Success: false,
                LocalFilePath: null,
                BytesDownloaded: 0,
                ErrorMessage: $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to decompress gzip data if present.
    /// </summary>
    private static async Task<byte[]> TryDecompressGzipAsync(byte[] data)
    {
        // Check for gzip magic number (0x1f 0x8b)
        if (data.Length < 2 || data[0] != 0x1f || data[1] != 0x8b)
        {
            return data;
        }

        try
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            
            await gzip.CopyToAsync(output);
            return output.ToArray();
        }
        catch
        {
            // Not valid gzip, return original
            return data;
        }
    }

    private void UpdateStatus(SyncState state, string? message = null, double? progress = null)
    {
        _currentStatus = _currentStatus with
        {
            State = state,
            Progress = progress ?? _currentStatus.Progress,
            ErrorMessage = state == SyncState.Error ? message : null,
            LastAction = message
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}
