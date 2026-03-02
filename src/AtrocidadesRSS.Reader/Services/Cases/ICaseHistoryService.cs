using AtrocidadesRSS.Reader.Models.Cases;

namespace AtrocidadesRSS.Reader.Services.Cases;

/// <summary>
/// Service for retrieving and managing case field history.
/// Provides timeline and diff functionality for UI components.
/// </summary>
public interface ICaseHistoryService
{
    /// <summary>
    /// Gets all history entries for a case, ordered by ChangedAt descending (newest first).
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of history view models.</returns>
    Task<IReadOnlyList<CaseFieldHistoryViewModel>> GetFullTimelineAsync(
        int caseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets history entries grouped by field name.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of field groups with history entries.</returns>
    Task<IReadOnlyList<FieldHistoryGroup>> GetTimelineByFieldAsync(
        int caseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets history entries for a specific field of a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="fieldName">The field name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of history view models for the field.</returns>
    Task<IReadOnlyList<CaseFieldHistoryViewModel>> GetFieldTimelineAsync(
        int caseId,
        string fieldName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available field names that have history for a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of field names.</returns>
    Task<IReadOnlyList<string>> GetAvailableFieldsAsync(
        int caseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets two versions for comparison (A/B diff).
    /// Uses index-based selection for deterministic ordering (newest first).
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="fieldName">The field name.</param>
    /// <param name="indexA">Index for version A (older, e.g., 1 for 2nd newest).</param>
    /// <param name="indexB">Index for version B (newer, e.g., 0 for newest).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Diff selection with both versions, or null if indices invalid.</returns>
    Task<FieldDiffSelection?> GetDiffSelectionAsync(
        int caseId,
        string fieldName,
        int indexA = 1,
        int indexB = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a case has any history entries.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if history exists.</returns>
    Task<bool> HasHistoryAsync(
        int caseId,
        CancellationToken cancellationToken = default);
}
