using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Models;
using OpenJustice.BrazilExtractor.Services.Tjgo;

namespace OpenJustice.BrazilExtractor.Services.Jobs;

/// <summary>
/// TJGO search job that executes the search workflow.
/// Orchestrates from worker loop to TJGO service.
/// </summary>
public class TjgoSearchJob : ITjgoSearchJob
{
    private readonly ITjgoSearchService _searchService;
    private readonly BrazilExtractorOptions _options;
    private readonly ILogger<TjgoSearchJob> _logger;

    public TjgoSearchJob(
        ITjgoSearchService searchService,
        IOptions<BrazilExtractorOptions> options,
        ILogger<TjgoSearchJob> logger)
    {
        _searchService = searchService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TjgoSearchResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting TJGO search job execution");

        // Determine the query date - use configured date window or default to today
        var queryDate = _options.QueryDateWindowStartDate ?? DateTime.Today;

        // Create single-day query - both DataInicial and DataFinal will be set to same value
        var query = TjgoSearchQuery.ForSingleDay(queryDate, _options.CriminalMode);

        _logger.LogInformation(
            "Executing search for date {Date} (formatted: {FormattedDate}), Criminal mode: {CriminalMode}",
            queryDate.ToString("yyyy-MM-dd"),
            query.FormattedDate,
            query.CriminalMode);

        // Execute the search
        var result = await _searchService.ExecuteSearchAsync(query, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation(
                "TJGO search job completed successfully - URL: {Url}, Records: {Records}",
                result.ResultUrl,
                result.RecordCount);
        }
        else
        {
            _logger.LogWarning(
                "TJGO search job completed with errors - Error: {Error}",
                result.ErrorMessage);
        }

        return result;
    }
}
