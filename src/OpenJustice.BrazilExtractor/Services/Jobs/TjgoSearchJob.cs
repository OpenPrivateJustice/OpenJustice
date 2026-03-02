using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Models;
using OpenJustice.BrazilExtractor.Services.Downloads;
using OpenJustice.BrazilExtractor.Services.Tjgo;

namespace OpenJustice.BrazilExtractor.Services.Jobs;

/// <summary>
/// TJGO search job that executes the search workflow.
/// Orchestrates from worker loop to TJGO service, enforces query-level cadence,
/// and integrates PDF downloads.
/// </summary>
public class TjgoSearchJob : ITjgoSearchJob
{
    private readonly ITjgoSearchService _searchService;
    private readonly IPdfDownloadService _downloadService;
    private readonly BrazilExtractorOptions _options;
    private readonly ILogger<TjgoSearchJob> _logger;

    // Track last query execution time for cadence enforcement (EXTR-07)
    // Static to persist across job instances within the same worker process
    private static DateTime? _lastQueryExecutionTimeUtc;

    public TjgoSearchJob(
        ITjgoSearchService searchService,
        IPdfDownloadService downloadService,
        IOptions<BrazilExtractorOptions> options,
        ILogger<TjgoSearchJob> logger)
    {
        _searchService = searchService;
        _downloadService = downloadService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TjgoSearchResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting TJGO search job execution");

        // Determine the query date - use configured date window or default to today
        var queryDate = _options.QueryDateWindowStartDate ?? DateTime.Today;

        // Enforce query-level cadence (EXTR-07)
        // Delay applies to query/pagination actions, NOT to individual PDF downloads
        await EnforceQueryCadenceAsync();

        // Create single-day query - both DataInicial and DataFinal will be set to same value
        var query = TjgoSearchQuery.ForSingleDay(queryDate, _options.CriminalMode);

        var queryStartTime = DateTime.UtcNow;
        
        _logger.LogInformation(
            "Executing search for date {Date} (formatted: {FormattedDate}), Criminal mode: {CriminalMode}",
            queryDate.ToString("yyyy-MM-dd"),
            query.FormattedDate,
            query.CriminalMode);

        // Execute the search
        var result = await _searchService.ExecuteSearchAsync(query, cancellationToken);

        // Update last query execution time
        _lastQueryExecutionTimeUtc = DateTime.UtcNow;

        if (result.Success)
        {
            // Log query execution timing for cadence verification
            var queryDuration = result.QueryExecutionEndUtc - result.QueryExecutionStartUtc;
            _logger.LogInformation(
                "TJGO search completed - DateWindow: {DateWindow}, FilterProfile: {Profile}, Records: {Records}, Pages: {PageIndex}, QueryDuration: {Duration:F2}s",
                result.DateWindow ?? "N/A",
                result.AppliedFilterProfile ?? "None",
                result.RecordCount,
                result.PageIndex,
                queryDuration.TotalSeconds);

            // Download captured PDF links (EXTR-08)
            if (result.PdfLinks.Count > 0)
            {
                _logger.LogInformation(
                    "Starting PDF download for {Count} harvested links from query date {Date}",
                    result.PdfLinks.Count, queryDate.ToString("yyyy-MM-dd"));

                var downloadResult = await _downloadService.DownloadBatchAsync(
                    result.PdfLinks,
                    queryDate,
                    cancellationToken);

                // Attach download result to search result
                result.DownloadResult = downloadResult;

                // Log download telemetry
                _logger.LogInformation(
                    "PDF download completed - Attempted: {Attempted}, Succeeded: {Succeeded}, Failed: {Failed}, Duration: {Duration:F2}s",
                    downloadResult.AttemptedCount,
                    downloadResult.SucceededCount,
                    downloadResult.FailedCount,
                    downloadResult.Duration.TotalSeconds);

                if (downloadResult.SucceededCount > 0)
                {
                    _logger.LogInformation(
                        "Downloaded files saved to: {DownloadPath}",
                        _downloadService.DownloadPath);
                }

                if (downloadResult.FailedCount > 0)
                {
                    foreach (var failure in downloadResult.Failures)
                    {
                        _logger.LogWarning(
                            "PDF download failed - URL: {Url}, Reason: {Reason}, HTTP: {HttpStatus}",
                            failure.Url,
                            failure.Reason,
                            failure.HttpStatusCode?.ToString() ?? "N/A");
                    }
                }
            }
            else
            {
                _logger.LogInformation("No PDF links harvested, skipping download phase");
            }

            // Log final acquisition telemetry
            LogAcquisitionTelemetry(result);
        }
        else
        {
            _logger.LogWarning(
                "TJGO search job completed with errors - Error: {Error}, FilterProfile: {Profile}",
                result.ErrorMessage,
                result.AppliedFilterProfile ?? "None");
        }

        return result;
    }

    /// <summary>
    /// Enforces query-level cadence by waiting if needed.
    /// EXTR-07: Interval applies to query/pagination executions, NOT individual PDF downloads.
    /// </summary>
    private async Task EnforceQueryCadenceAsync()
    {
        var now = DateTime.UtcNow;
        
        if (_lastQueryExecutionTimeUtc.HasValue)
        {
            var elapsed = now - _lastQueryExecutionTimeUtc.Value;
            var intervalSeconds = _options.QueryIntervalSeconds;
            
            if (elapsed.TotalSeconds < intervalSeconds)
            {
                var waitTime = intervalSeconds - elapsed.TotalSeconds;
                _logger.LogInformation(
                    "Enforcing query cadence: waited {WaitTime:F1}s (interval: {Interval}s, elapsed: {Elapsed:F1}s)",
                    waitTime, intervalSeconds, elapsed.TotalSeconds);
                
                await Task.Delay(TimeSpan.FromSeconds(waitTime));
            }
            else
            {
                _logger.LogDebug(
                    "Query cadence satisfied: elapsed {Elapsed:F1}s >= interval {Interval}s",
                    elapsed.TotalSeconds, intervalSeconds);
            }
        }
        else
        {
            _logger.LogDebug("First query execution, no cadence delay needed");
        }
    }

    /// <summary>
    /// Logs comprehensive acquisition telemetry for operations.
    /// </summary>
    private void LogAcquisitionTelemetry(TjgoSearchResult result)
    {
        var dateWindow = result.Query?.FormattedDate ?? "N/A";
        var downloadResult = result.DownloadResult;

        _logger.LogInformation(
            "=== Acquisition Telemetry ===");
        
        _logger.LogInformation(
            "Query Date: {DateWindow}, Filter: {Filter}, Records: {Records}",
            dateWindow,
            result.AppliedFilterProfile ?? "None",
            result.RecordCount);

        _logger.LogInformation(
            "Query Executions: 1, Pages Traversed: {Pages}, QueryStart: {QueryStart:HH:mm:ss}Z, QueryEnd: {QueryEnd:HH:mm:ss}Z",
            result.PageIndex + 1,
            result.QueryExecutionStartUtc.ToString("HH:mm:ss"),
            result.QueryExecutionEndUtc.ToString("HH:mm:ss"));

        _logger.LogInformation(
            "Links Captured: {LinksCaptured} (Total seen: {TotalSeen}, Unique: {Unique}, Capped: {Capped})",
            result.PdfLinks.Count,
            result.TotalLinksSeen,
            result.UniqueLinksRetained,
            result.WasCapped);

        if (downloadResult != null)
        {
            _logger.LogInformation(
                "Downloads: Attempted: {Attempted}, Succeeded: {Succeeded}, Failed: {Failed}, Duration: {Duration:F2}s",
                downloadResult.AttemptedCount,
                downloadResult.SucceededCount,
                downloadResult.FailedCount,
                downloadResult.Duration.TotalSeconds);

            if (downloadResult.SucceededFiles.Count > 0)
            {
                _logger.LogInformation("Downloaded files: {Count} files in {Path}",
                    downloadResult.SucceededFiles.Count,
                    _downloadService.DownloadPath);
            }
        }
        else
        {
            _logger.LogInformation("Downloads: Skipped (no links captured)");
        }

        _logger.LogInformation("===========================");
    }
}
