using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Services.Jobs;

namespace OpenJustice.BrazilExtractor;

/// <summary>
/// Background worker that executes TJGO search iterations.
/// Provides acquisition telemetry for operations monitoring.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly BrazilExtractorOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        ILogger<Worker> logger,
        IOptions<BrazilExtractorOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _options = options.Value;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BrazilExtractor worker starting");

        // Configuration is validated at startup - options are guaranteed to be valid here
        _logger.LogInformation("TJGO URL: {Url}", _options.TjgoUrl);
        _logger.LogInformation("ConsultaPublicacao URL: {Url}", _options.ConsultaPublicacaoUrl);
        _logger.LogInformation("Criminal mode: {CriminalMode}", _options.CriminalMode);
        _logger.LogInformation("Download path: {DownloadPath}", _options.DownloadPath);
        _logger.LogInformation("Query interval: {Interval} seconds", _options.QueryIntervalSeconds);
        _logger.LogInformation("Max results per query: {MaxResults}", _options.MaxResultsPerQuery);

        // Track iteration count for telemetry
        int iterationCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            iterationCount++;
            var iterationStartTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("=== Starting TJGO search iteration {Iteration} ===", iterationCount);

                // Create scope for scoped services (TJGO job and services)
                using var scope = _scopeFactory.CreateScope();
                var searchJob = scope.ServiceProvider.GetRequiredService<ITjgoSearchJob>();

                // Execute the search job with cancellation token
                var result = await searchJob.ExecuteAsync(stoppingToken);

                // Surface key acquisition telemetry at worker level
                if (result.Success)
                {
                    _logger.LogInformation(
                        "=== TJGO iteration {Iteration} completed: QueryDate: {DateWindow}, Filter: {Filter}, Records: {Records}, PDFLinks: {PdfLinks} ===",
                        iterationCount,
                        result.DateWindow ?? "N/A",
                        result.AppliedFilterProfile ?? "None",
                        result.RecordCount,
                        result.PdfLinks.Count);

                    // Log download summary if applicable
                    if (result.DownloadResult != null)
                    {
                        var download = result.DownloadResult;
                        if (download.SucceededCount > 0)
                        {
                            _logger.LogInformation(
                                "=== Download summary: {Succeeded}/{Attempted} PDFs downloaded in {Duration:F2}s, files saved to: {Path} ===",
                                download.SucceededCount,
                                download.AttemptedCount,
                                download.Duration.TotalSeconds,
                                _options.DownloadPath);

                            // Log first few file paths for verification
                            var sampleFiles = download.SucceededFiles.Take(3).ToList();
                            foreach (var file in sampleFiles)
                            {
                                _logger.LogDebug("  Downloaded: {File}", Path.GetFileName(file));
                            }

                            if (download.SucceededFiles.Count > 3)
                            {
                                _logger.LogDebug("  ... and {Count} more files", download.SucceededFiles.Count - 3);
                            }
                        }

                        if (download.FailedCount > 0)
                        {
                            _logger.LogWarning(
                                "=== Download failures: {Failed}/{Attempted} PDFs failed ===",
                                download.FailedCount,
                                download.AttemptedCount);
                        }
                    }

                    // Log query timing for cadence verification
                    _logger.LogInformation(
                        "=== Query timing: Start: {Start:HH:mm:ss}Z, End: {End:HH:mm:ss}Z, Duration: {Duration:F2}s ===",
                        result.QueryExecutionStartUtc,
                        result.QueryExecutionEndUtc,
                        (result.QueryExecutionEndUtc - result.QueryExecutionStartUtc).TotalSeconds);
                }
                else
                {
                    _logger.LogWarning(
                        "=== TJGO iteration {Iteration} failed: {Error} ===",
                        iterationCount,
                        result.ErrorMessage ?? "Unknown error");
                }

                var iterationDuration = DateTime.UtcNow - iterationStartTime;
                _logger.LogInformation(
                    "=== Iteration {Iteration} total duration: {Duration:F2}s ===",
                    iterationCount,
                    iterationDuration.TotalSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker cancellation requested, stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TJGO search iteration {Iteration}", iterationCount);
            }

            // Wait for the configured interval before next iteration
            _logger.LogDebug("Waiting {Interval} seconds before next iteration", _options.QueryIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.QueryIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("BrazilExtractor worker stopped after {Iterations} iterations", iterationCount);
    }
}
