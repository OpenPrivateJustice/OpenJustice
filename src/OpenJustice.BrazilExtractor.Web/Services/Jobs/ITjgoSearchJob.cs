using Microsoft.Extensions.Logging;
using OpenJustice.BrazilExtractor.Models;
using OpenJustice.BrazilExtractor.Services.Tjgo;

namespace OpenJustice.BrazilExtractor.Services.Jobs;

/// <summary>
/// Interface for TJGO search job execution.
/// </summary>
public interface ITjgoSearchJob
{
    /// <summary>
    /// Executes a single TJGO search job iteration.
    /// </summary>
    /// <param name="queryDate">Optional query date override (single-day query).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search result.</returns>
    Task<TjgoSearchResult> ExecuteAsync(DateTime? queryDate = null, CancellationToken cancellationToken = default);
}
