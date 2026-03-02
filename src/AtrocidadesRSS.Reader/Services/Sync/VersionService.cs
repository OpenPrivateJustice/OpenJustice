using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using AtrocidadesRSS.Reader.Configuration;

namespace AtrocidadesRSS.Reader.Services.Sync;

/// <summary>
/// Service for checking and comparing dataset versions.
/// </summary>
public class VersionService : IVersionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ReaderOptions _options;
    private readonly ILogger<VersionService> _logger;
    
    // In-memory storage for local version (persisted via localStorage in browser)
    private string _localVersion;
    
    // Response model for version endpoint
    private record VersionResponse(string Version, string? DownloadUrl, string? ReleaseNotes);

    public VersionService(
        IHttpClientFactory httpClientFactory,
        IOptions<ReaderOptions> options,
        ILogger<VersionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        
        // Initialize with configured local version (can be overridden from persistent storage)
        _localVersion = _options.Snapshot.LocalVersion ?? "0";
    }

    /// <inheritdoc/>
    public string GetLocalVersion()
    {
        return _localVersion;
    }

    /// <inheritdoc/>
    public async Task<VersionCheckResult> CheckVersionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking for version updates at {Endpoint}", _options.Snapshot.VersionEndpoint);

        try
        {
            // Fetch version info from remote endpoint using HttpClientFactory
            using var httpClient = _httpClientFactory.CreateClient("GeneratorHistoryApiClient");
            var response = await httpClient.GetAsync(
                _options.Snapshot.VersionEndpoint,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = $"Version check failed with status {response.StatusCode}";
                _logger.LogWarning(errorMsg);
                return new VersionCheckResult(
                    UpdateAvailable: false,
                    LocalVersion: _localVersion,
                    RemoteVersion: "unknown",
                    DownloadUrl: null,
                    ErrorMessage: errorMsg);
            }

            var versionInfo = await response.Content.ReadFromJsonAsync<VersionResponse>(
                cancellationToken: cancellationToken);

            if (versionInfo == null)
            {
                return new VersionCheckResult(
                    UpdateAvailable: false,
                    LocalVersion: _localVersion,
                    RemoteVersion: "unknown",
                    DownloadUrl: null,
                    ErrorMessage: "Invalid version response from server");
            }

            var remoteVersion = versionInfo.Version ?? "0";
            var updateAvailable = IsNewerVersion(remoteVersion, _localVersion);
            
            // Construct download URL if not provided in response
            var downloadUrl = versionInfo.DownloadUrl 
                ?? $"{_options.Snapshot.DownloadBaseUrl}/v{remoteVersion}.sql.gz";

            _logger.LogInformation(
                "Version check complete: local={LocalVersion}, remote={RemoteVersion}, update available={UpdateAvailable}",
                _localVersion, remoteVersion, updateAvailable);

            return new VersionCheckResult(
                UpdateAvailable: updateAvailable,
                LocalVersion: _localVersion,
                RemoteVersion: remoteVersion,
                DownloadUrl: downloadUrl,
                ErrorMessage: null);
        }
        catch (HttpRequestException ex)
        {
            var errorMsg = $"Network error checking version: {ex.Message}";
            _logger.LogWarning(ex, "Version check failed");
            return new VersionCheckResult(
                UpdateAvailable: false,
                LocalVersion: _localVersion,
                RemoteVersion: "unknown",
                DownloadUrl: null,
                ErrorMessage: errorMsg);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Version check was cancelled");
            return new VersionCheckResult(
                UpdateAvailable: false,
                LocalVersion: _localVersion,
                RemoteVersion: "unknown",
                DownloadUrl: null,
                ErrorMessage: "Version check cancelled");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error checking version: {ex.Message}";
            _logger.LogError(ex, "Version check failed");
            return new VersionCheckResult(
                UpdateAvailable: false,
                LocalVersion: _localVersion,
                RemoteVersion: "unknown",
                DownloadUrl: null,
                ErrorMessage: errorMsg);
        }
    }

    /// <inheritdoc/>
    public Task SaveLocalVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        _localVersion = version;
        _logger.LogInformation("Local version saved: {Version}", version);
        
        // In a full implementation, this would persist to localStorage
        // For now, we keep it in memory and could extend with JS interop
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Compares two version strings to determine if the remote version is newer.
    /// </summary>
    private static bool IsNewerVersion(string remoteVersion, string localVersion)
    {
        // Parse versions for comparison
        // Supports formats like "1", "1.0", "v1", "v1.0.0"
        var remote = ParseVersion(remoteVersion);
        var local = ParseVersion(localVersion);

        for (int i = 0; i < Math.Max(remote.Length, local.Length); i++)
        {
            var r = i < remote.Length ? remote[i] : 0;
            var l = i < local.Length ? local[i] : 0;

            if (r > l) return true;
            if (r < l) return false;
        }

        return false; // Versions are equal
    }

    /// <summary>
    /// Parses a version string into an array of integers.
    /// </summary>
    private static int[] ParseVersion(string version)
    {
        // Remove 'v' prefix if present
        var cleaned = version.TrimStart('v', 'V');
        
        return cleaned
            .Split('.')
            .Select(part => int.TryParse(part, out var num) ? num : 0)
            .ToArray();
    }
}
