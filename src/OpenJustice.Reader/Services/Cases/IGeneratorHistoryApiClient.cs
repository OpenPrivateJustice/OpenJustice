using System.Net;
using OpenJustice.Reader.Models.Cases;

namespace OpenJustice.Reader.Services.Cases;

/// <summary>
/// Contract for authenticated history API calls to Generator service.
/// </summary>
public interface IGeneratorHistoryApiClient
{
    /// <summary>
    /// Gets all field history for a case from the Generator API.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of field history entries, or empty list if not found.</returns>
    /// <exception cref="GeneratorHistoryApiUnauthorizedException">
    /// Thrown when the API returns 401 Unauthorized.
    /// </exception>
    Task<List<CaseFieldHistoryViewModel>> GetCaseHistoryAsync(
        int caseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets history for a specific field on a case from the Generator API.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="fieldName">The field name to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of field history entries for the specified field, or empty list if not found.</returns>
    /// <exception cref="GeneratorHistoryApiUnauthorizedException">
    /// Thrown when the API returns 401 Unauthorized.
    /// </exception>
    Task<List<CaseFieldHistoryViewModel>> GetFieldHistoryAsync(
        int caseId,
        string fieldName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown when the Generator history API returns 401 Unauthorized.
/// </summary>
public class GeneratorHistoryApiUnauthorizedException : Exception
{
    /// <summary>
    /// The login URL to redirect the user to for authentication.
    /// </summary>
    public string LoginUrl { get; }

    /// <summary>
    /// Creates a new instance of GeneratorHistoryApiUnauthorizedException.
    /// </summary>
    /// <param name="loginUrl">The login URL for re-authentication.</param>
    public GeneratorHistoryApiUnauthorizedException(string loginUrl)
        : base("Authentication failed. Please log in to access Generator history data.")
    {
        LoginUrl = loginUrl;
    }
}
