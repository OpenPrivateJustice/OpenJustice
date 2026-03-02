using OpenJustice.Generator.Infrastructure.Persistence.Entities;

namespace OpenJustice.Generator.Services.History;

/// <summary>
/// Interface for immutable case field history tracking service.
/// </summary>
public interface ICaseFieldHistoryService
{
    /// <summary>
    /// Appends history entries for all changed fields between the old and new case values.
    /// This is an append-only operation - no updates or deletes are performed.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="oldCase">The case entity before the update (null for new cases).</param>
    /// <param name="newCase">The case entity after the update.</param>
    /// <param name="curatorId">The curator who made the changes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of created history entries.</returns>
    Task<List<CaseFieldHistory>> AppendChangesAsync(
        int caseId,
        Case? oldCase,
        Case newCase,
        string? curatorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all field history entries for a case, ordered by ChangedAt descending (most recent first).
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of history entries.</returns>
    Task<List<CaseFieldHistory>> GetHistoryForCaseAsync(int caseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets field history entries for a specific field on a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="fieldName">The field name to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of history entries for the field.</returns>
    Task<List<CaseFieldHistory>> GetFieldHistoryAsync(int caseId, string fieldName, CancellationToken cancellationToken = default);
}
