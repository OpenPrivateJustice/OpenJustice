using AtrocidadesRSS.Reader.Models.Cases;
using Microsoft.Extensions.Logging;

namespace AtrocidadesRSS.Reader.Services.Cases;

/// <summary>
/// Service for retrieving and managing case field history.
/// Provides timeline and diff functionality for UI components.
/// Consumes live data from Generator API via IGeneratorHistoryApiClient.
/// </summary>
public class CaseHistoryService : ICaseHistoryService
{
    private readonly IGeneratorHistoryApiClient _historyApiClient;
    private readonly ILogger<CaseHistoryService> _logger;

    public CaseHistoryService(
        IGeneratorHistoryApiClient historyApiClient,
        ILogger<CaseHistoryService> logger)
    {
        _historyApiClient = historyApiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CaseFieldHistoryViewModel>> GetFullTimelineAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading full timeline for case ID: {CaseId}", caseId);

        try
        {
            var history = await _historyApiClient.GetCaseHistoryAsync(caseId, cancellationToken);
            
            if (history.Count == 0)
            {
                _logger.LogDebug("No history found for case ID: {CaseId}", caseId);
                return Array.Empty<CaseFieldHistoryViewModel>();
            }

            // Order by ChangedAt descending (newest first) for deterministic output
            var orderedHistory = history.OrderByDescending(h => h.ChangedAt).ToList();
            return orderedHistory;
        }
        catch (GeneratorHistoryApiUnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to case history for case ID: {CaseId}", caseId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FieldHistoryGroup>> GetTimelineByFieldAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading timeline grouped by field for case ID: {CaseId}", caseId);

        try
        {
            var history = await _historyApiClient.GetCaseHistoryAsync(caseId, cancellationToken);
            
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
                    Entries = g.OrderByDescending(h => h.ChangedAt).ToList()
                })
                .OrderBy(g => g.FieldDisplayName)
                .ToList();

            return groups;
        }
        catch (GeneratorHistoryApiUnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to case history for case ID: {CaseId}", caseId);
            throw;
        }
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

        try
        {
            var history = await _historyApiClient.GetFieldHistoryAsync(caseId, fieldName, cancellationToken);
            
            if (history.Count == 0)
            {
                _logger.LogDebug("No history found for case ID: {CaseId}, field: {FieldName}", 
                    caseId, fieldName);
                return Array.Empty<CaseFieldHistoryViewModel>();
            }

            // Order by ChangedAt descending (newest first)
            var orderedHistory = history.OrderByDescending(h => h.ChangedAt).ToList();
            return orderedHistory;
        }
        catch (GeneratorHistoryApiUnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to field history for case ID: {CaseId}, field: {FieldName}", 
                caseId, fieldName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetAvailableFieldsAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading available fields for case ID: {CaseId}", caseId);

        try
        {
            var history = await _historyApiClient.GetCaseHistoryAsync(caseId, cancellationToken);
            var fieldNames = history.Select(h => h.FieldName).Distinct().OrderBy(f => f).ToList();
            return fieldNames;
        }
        catch (GeneratorHistoryApiUnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to case history for case ID: {CaseId}", caseId);
            throw;
        }
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

        try
        {
            // Get history for the field (already ordered newest first)
            var history = await _historyApiClient.GetFieldHistoryAsync(caseId, fieldName, cancellationToken);
            
            if (history.Count == 0)
            {
                _logger.LogDebug("No history found for diff selection: case {CaseId}, field: {FieldName}", 
                    caseId, fieldName);
                return null;
            }

            // Order by ChangedAt descending (newest first) for deterministic index access
            var viewModels = history.OrderByDescending(h => h.ChangedAt).ToList();

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
        catch (GeneratorHistoryApiUnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to diff selection for case ID: {CaseId}, field: {FieldName}", 
                caseId, fieldName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasHistoryAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _historyApiClient.GetCaseHistoryAsync(caseId, cancellationToken);
            return history.Count > 0;
        }
        catch (GeneratorHistoryApiUnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to check history existence for case ID: {CaseId}", caseId);
            throw;
        }
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
