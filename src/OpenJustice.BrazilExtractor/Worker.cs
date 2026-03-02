using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenJustice.BrazilExtractor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly BrazilExtractorOptions _options;

    public Worker(ILogger<Worker> logger, IOptions<BrazilExtractorOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BrazilExtractor worker starting");

        // Configuration is validated at startup - options are guaranteed to be valid here
        _logger.LogInformation("TJGO URL: {Url}", _options.TjgoUrl);
        _logger.LogInformation("Criminal mode: {CriminalMode}", _options.CriminalMode);
        _logger.LogInformation("Download path: {DownloadPath}", _options.DownloadPath);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("BrazilExtractor running at: {Time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
