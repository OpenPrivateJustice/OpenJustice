using AtrocidadesRSS.Reader.Models.Cases;

namespace AtrocidadesRSS.Reader.Services.Cases;

/// <summary>
/// Service for loading complete case details including all related data.
/// </summary>
public interface ICaseDetailsService
{
    /// <summary>
    /// Gets the full case details by ID, including sources, evidence, judicial info, and metadata.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The case detail view model, or null if not found.</returns>
    Task<CaseDetailViewModel?> GetCaseDetailsAsync(int caseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full case details by reference code.
    /// </summary>
    /// <param name="referenceCode">The reference code (e.g., ATRO-2024-0001).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The case detail view model, or null if not found.</returns>
    Task<CaseDetailViewModel?> GetCaseDetailsByReferenceAsync(string referenceCode, CancellationToken cancellationToken = default);
}
