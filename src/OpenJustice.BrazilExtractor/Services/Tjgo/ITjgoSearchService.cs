using OpenJustice.BrazilExtractor.Models;

namespace OpenJustice.BrazilExtractor.Services.Tjgo;

/// <summary>
/// Service for executing TJGO portal search operations.
/// </summary>
public interface ITjgoSearchService
{
    /// <summary>
    /// Executes a TJGO search query with single-day date semantics.
    /// </summary>
    /// <param name="query">The search query with date and mode settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search result with success status and record count.</returns>
    Task<TjgoSearchResult> ExecuteSearchAsync(TjgoSearchQuery query, CancellationToken cancellationToken = default);
}
