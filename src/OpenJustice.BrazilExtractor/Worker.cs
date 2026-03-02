using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Services.Jobs;

namespace OpenJustice.BrazilExtractor;

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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting TJGO search iteration");

                // Create scope for scoped services (TJGO job and services)
                using var scope = _scopeFactory.CreateScope();
                var searchJob = scope.ServiceProvider.GetRequiredService<ITjgoSearchJob>();

                // Execute the search job with cancellation token
                var result = await searchJob.ExecuteAsync(stoppingToken);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "TJGO search iteration completed successfully - Records: {Records}",
                        result.RecordCount);
                }
                else
                {
                    _logger.LogWarning(
                        "TJGO search iteration completed with errors - Error: {Error}",
                        result.ErrorMessage);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker cancellation requested, stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TJGO search iteration");
            }

            // Wait for the configured interval before next iteration
            _logger.LogDebug("Waiting {Interval} seconds before next iteration", _options.QueryIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.QueryIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("BrazilExtractor worker stopped");
    }
}
