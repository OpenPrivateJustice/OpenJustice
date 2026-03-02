using AtrocidadesRSS.Reader.Models.Cases;
using AtrocidadesRSS.Reader.Services.Data;
using Microsoft.Extensions.Logging;

namespace AtrocidadesRSS.Reader.Services.Cases;

/// <summary>
/// Service for retrieving and managing case field history.
/// Provides timeline and diff functionality for UI components.
/// Read-only and resilient: returns empty collections if no history exists.
/// </summary>
public class CaseHistoryService : ICaseHistoryService
{
    private readonly ILocalCaseStore _caseStore;
    private readonly ILogger<CaseHistoryService> _logger;

    public CaseHistoryService(
        ILocalCaseStore caseStore,
        ILogger<CaseHistoryService> logger)
    {
        _caseStore = caseStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CaseFieldHistoryViewModel>> GetFullTimelineAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading full timeline for case ID: {CaseId}", caseId);

        var history = await _caseStore.GetCaseHistoryAsync(caseId, cancellationToken);
        
        if (history.Count == 0)
        {
            _logger.LogDebug("No history found for case ID: {CaseId}", caseId);
            return Array.Empty<CaseFieldHistoryViewModel>();
        }

        // Order by ChangedAt descending (newest first) for deterministic output
        var orderedHistory = history.OrderByDescending(h => h.ChangedAt).ToList();
        return CaseFieldHistoryViewModel.FromEntityList(orderedHistory);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FieldHistoryGroup>> GetTimelineByFieldAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading timeline grouped by field for case ID: {CaseId}", caseId);

        var history = await _caseStore.GetCaseHistoryAsync(caseId, cancellationToken);
        
        if (history.Count == 0)
        {
            _logger.LogDebug("No history found for case ID: {CaseId}", caseId);
            return Array.Empty<FieldHistoryGroup>();
        }

        // Group by field name and order each group's entries by ChangedAt descending
        var groups = history
            .GroupBy(h => h.FieldName)
            .Select(g => new FieldHistoryGroup
            {
                FieldName = g.Key,
                FieldDisplayName = FormatFieldName(g.Key),
                Entries = CaseFieldHistoryViewModel.FromEntityList(
                    g.OrderByDescending(h => h.ChangedAt).ToList())
            })
            .OrderBy(g => g.FieldDisplayName)
            .ToList();

        return groups;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CaseFieldHistoryViewModel>> GetFieldTimelineAsync(
        int caseId,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading field timeline for case ID: {CaseId}, field: {FieldName}", 
            caseId, fieldName);

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            _logger.LogWarning("Field name is empty for case ID: {CaseId}", caseId);
            return Array.Empty<CaseFieldHistoryViewModel>();
        }

        var history = await _caseStore.GetFieldHistoryAsync(caseId, fieldName, cancellationToken);
        
        if (history.Count == 0)
        {
            _logger.LogDebug("No history found for case ID: {CaseId}, field: {FieldName}", 
                caseId, fieldName);
            return Array.Empty<CaseFieldHistoryViewModel>();
        }

        // Order by ChangedAt descending (newest first)
        var orderedHistory = history.OrderByDescending(h => h.ChangedAt).ToList();
        return CaseFieldHistoryViewModel.FromEntityList(orderedHistory);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetAvailableFieldsAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading available fields for case ID: {CaseId}", caseId);

        var fieldNames = await _caseStore.GetHistoryFieldNamesAsync(caseId, cancellationToken);
        return fieldNames;
    }

    /// <inheritdoc/>
    public async Task<FieldDiffSelection?> GetDiffSelectionAsync(
        int caseId,
        string fieldName,
        int indexA = 1,
        int indexB = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading diff selection for case ID: {CaseId}, field: {FieldName}, indices: A={IndexA}, B={IndexB}", 
            caseId, fieldName, indexA, indexB);

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            _logger.LogWarning("Field name is empty for case ID: {CaseId}", caseId);
            return null;
        }

        // Get history for the field (already ordered newest first)
        var history = await _caseStore.GetFieldHistoryAsync(caseId, fieldName, cancellationToken);
        
        if (history.Count == 0)
        {
            _logger.LogDebug("No history found for diff selection: case {CaseId}, field: {FieldName}", 
                caseId, fieldName);
            return null;
        }

        // Convert to view models for easier access
        var viewModels = CaseFieldHistoryViewModel.FromEntityList(
            history.OrderByDescending(h => h.ChangedAt).ToList());

        // Validate indices
        if (indexA < 0 || indexA >= viewModels.Count || 
            indexB < 0 || indexB >= viewModels.Count)
        {
            _logger.LogWarning("Invalid indices for diff selection: A={IndexA}, B={IndexB}, count={Count}", 
                indexA, indexB, viewModels.Count);
            return null;
        }

        // Ensure A is older than B (indexA > indexB means A is older since newest first)
        if (indexA <= indexB)
        {
            // Swap to ensure proper A/B ordering
            (indexA, indexB) = (indexB, indexA);
        }

        return new FieldDiffSelection
        {
            FieldName = fieldName,
            VersionA = viewModels[indexA],
            VersionB = viewModels[indexB]
        };
    }

    /// <inheritdoc/>
    public async Task<bool> HasHistoryAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        var history = await _caseStore.GetCaseHistoryAsync(caseId, cancellationToken);
        return history.Count > 0;
    }

    private static string FormatFieldName(string fieldName)
    {
        // Convert PascalCase to Title Case with spaces
        if (string.IsNullOrEmpty(fieldName))
            return fieldName;

        var result = string.Concat(fieldName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));

        return char.ToUpper(result[0]) + result[1..];
    }
}
