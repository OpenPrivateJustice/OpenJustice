using OpenJustice.Reader.Models.Search;
using OpenJustice.Reader.Services.Data;

namespace OpenJustice.Reader.Services.Search;

/// <summary>
/// Interface for case search operations.
/// </summary>
public interface ICaseSearchService
{
    /// <summary>
    /// Executes a search query with fuzzy matching, filters, sorting, and pagination.
    /// </summary>
    Task<PagedResult<LocalCase>> SearchAsync(
        CaseSearchQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available crime types for filtering.
    /// </summary>
    Task<IReadOnlyList<string>> GetCrimeTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available judicial statuses for filtering.
    /// </summary>
    Task<IReadOnlyList<string>> GetJudicialStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available states for filtering.
    /// </summary>
    Task<IReadOnlyList<string>> GetStatesAsync(CancellationToken cancellationToken = default);
}
