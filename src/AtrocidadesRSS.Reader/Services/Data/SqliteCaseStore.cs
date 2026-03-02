using System.Text.RegularExpressions;

namespace AtrocidadesRSS.Reader.Services.Data;

/// <summary>
/// Local case store implementation using in-memory storage.
/// In a production Blazor WASM app, this would use sql.js (SQLite compiled to WebAssembly)
/// or IndexedDB via JS interop for persistence.
/// </summary>
public class SqliteCaseStore : ILocalCaseStore
{
    private readonly ILogger<SqliteCaseStore> _logger;
    
    // In-memory storage (in production, would use sql.js or IndexedDB)
    private readonly List<LocalCase> _cases = new();
    private readonly List<LocalCaseFieldHistory> _fieldHistory = new();
    private readonly object _lock = new();
    
    private ImportStatus _importStatus = new(
        IsImporting: false,
        Progress: 0,
        CurrentPhase: null,
        ErrorMessage: null,
        ImportedCount: null,
        StartedAt: null,
        CompletedAt: null);

    private string? _currentVersion;
    private DateTime? _lastUpdated;

    public SqliteCaseStore(ILogger<SqliteCaseStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public ImportStatus GetImportStatus() => _importStatus;

    /// <inheritdoc/>
    public bool HasData()
    {
        lock (_lock)
        {
            return _cases.Count > 0;
        }
    }

    /// <inheritdoc/>
    public async Task<ImportResult> ImportSnapshotAsync(
        string sqlContent,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting SQL snapshot import");
        
        var warnings = new List<string>();
        var errors = new List<string>();
        var importedCount = 0;

        try
        {
            UpdateImportStatus(true, 0, "Parsing SQL...", null, null);

            // Parse SQL content to extract INSERT statements
            var statements = ParseSqlStatements(sqlContent);
            
            if (statements.Count == 0)
            {
                return new ImportResult(
                    Success: false,
                    RecordsImported: 0,
                    ErrorMessage: "No valid INSERT statements found in SQL file",
                    Warnings: warnings);
            }

            _logger.LogInformation("Found {Count} SQL statements to process", statements.Count);

            // Clear existing data
            lock (_lock)
            {
                _cases.Clear();
                _fieldHistory.Clear();
            }

            var totalStatements = statements.Count;
            var processedStatements = 0;

            foreach (var statement in statements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Try to parse as Case first
                    var caseData = ParseCaseInsertStatement(statement);
                    if (caseData != null)
                    {
                        lock (_lock)
                        {
                            _cases.Add(caseData);
                        }
                        importedCount++;
                        continue;
                    }

                    // Try to parse as CaseFieldHistory
                    var historyData = ParseCaseFieldHistoryStatement(statement);
                    if (historyData != null)
                    {
                        lock (_lock)
                        {
                            _fieldHistory.Add(historyData);
                        }
                        importedCount++;
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Failed to parse statement: {TruncateString(statement, 100)} - {ex.Message}");
                    _logger.LogWarning(ex, "Failed to parse SQL statement");
                }

                processedStatements++;
                var progressPercent = (double)processedStatements / totalStatements * 100;
                progress?.Report(progressPercent);
                
                UpdateImportStatus(
                    true,
                    progressPercent,
                    $"Importing {importedCount} records...",
                    null,
                    null);
            }

            // Finalize
            _lastUpdated = DateTime.UtcNow;
            
            UpdateImportStatus(
                false,
                100,
                "Import complete",
                null,
                importedCount);

            _logger.LogInformation("Import complete: {Count} records imported", importedCount);

            return new ImportResult(
                Success: true,
                RecordsImported: importedCount,
                ErrorMessage: errors.Count > 0 ? string.Join("; ", errors) : null,
                Warnings: warnings);
        }
        catch (OperationCanceledException)
        {
            UpdateImportStatus(false, 0, "Import cancelled", "Import was cancelled", null);
            
            return new ImportResult(
                Success: false,
                RecordsImported: importedCount,
                ErrorMessage: "Import was cancelled",
                Warnings: warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed");
            UpdateImportStatus(false, 0, "Import failed", ex.Message, null);
            
            return new ImportResult(
                Success: false,
                RecordsImported: importedCount,
                ErrorMessage: ex.Message,
                Warnings: warnings);
        }
    }

    /// <inheritdoc/>
    public async Task<ImportResult> ImportFromFileAsync(
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // In a full implementation, this would read from IndexedDB or fetch the file
        // For now, we simulate by loading from a placeholder
        _logger.LogInformation("Import from file: {Path}", filePath);
        
        // Simulate async operation
        await Task.Delay(100, cancellationToken);
        
        return new ImportResult(
            Success: false,
            RecordsImported: 0,
            ErrorMessage: "File import requires JS interop implementation for Blazor WASM",
            Warnings: new List<string>());
    }

    /// <inheritdoc/>
    public Task<LocalCase?> GetCaseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var caseItem = _cases.FirstOrDefault(c => c.Id == id);
            return Task.FromResult(caseItem);
        }
    }

    /// <inheritdoc/>
    public Task<LocalCase?> GetCaseByReferenceCodeAsync(string referenceCode, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var caseItem = _cases.FirstOrDefault(
                c => c.ReferenceCode.Equals(referenceCode, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(caseItem);
        }
    }

    /// <inheritdoc/>
    public Task<CaseSearchResult> SearchCasesAsync(
        CaseSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _cases.AsEnumerable();

            // Apply text search
            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.ToLowerInvariant();
                query = query.Where(c =>
                    (c.VictimName?.ToLowerInvariant().Contains(q) ?? false) ||
                    (c.AccusedName?.ToLowerInvariant().Contains(q) ?? false) ||
                    (c.Description?.ToLowerInvariant().Contains(q) ?? false) ||
                    c.ReferenceCode.ToLowerInvariant().Contains(q));
            }

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.CrimeType))
            {
                query = query.Where(c => c.CrimeType == filter.CrimeType);
            }

            if (!string.IsNullOrWhiteSpace(filter.LocationState))
            {
                query = query.Where(c => c.LocationState == filter.LocationState);
            }

            if (!string.IsNullOrWhiteSpace(filter.JudicialStatus))
            {
                query = query.Where(c => c.JudicialStatus == filter.JudicialStatus);
            }

            if (filter.DateFrom.HasValue)
            {
                query = query.Where(c => c.CrimeDate >= filter.DateFrom.Value);
            }

            if (filter.DateTo.HasValue)
            {
                query = query.Where(c => c.CrimeDate <= filter.DateTo.Value);
            }

            // Get total count before pagination
            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            // Apply pagination
            var results = query
                .OrderByDescending(c => c.CrimeDate ?? c.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return Task.FromResult(new CaseSearchResult(
                Cases: results,
                TotalCount: totalCount,
                Page: filter.Page,
                PageSize: filter.PageSize,
                TotalPages: totalPages));
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetCrimeTypesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var types = _cases
                .Where(c => !string.IsNullOrWhiteSpace(c.CrimeType))
                .Select(c => c.CrimeType!)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(types);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetJudicialStatusesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var statuses = _cases
                .Where(c => !string.IsNullOrWhiteSpace(c.JudicialStatus))
                .Select(c => c.JudicialStatus!)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(statuses);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetStatesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var states = _cases
                .Where(c => !string.IsNullOrWhiteSpace(c.LocationState))
                .Select(c => c.LocationState!)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(states);
        }
    }

    /// <inheritdoc/>
    public Task<DatabaseStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var stats = new DatabaseStats(
                TotalCases: _cases.Count,
                VerifiedCases: _cases.Count(c => c.IsVerified),
                SensitiveCases: _cases.Count(c => c.IsSensitiveContent),
                LastUpdated: _lastUpdated,
                CurrentVersion: _currentVersion);

            return Task.FromResult(stats);
        }
    }

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _cases.Clear();
            _fieldHistory.Clear();
            _currentVersion = null;
            _lastUpdated = null;
        }

        _logger.LogInformation("Local case store cleared");
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<LocalCaseFieldHistory>> GetCaseHistoryAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var history = _fieldHistory
                .Where(h => h.CaseId == caseId)
                .OrderByDescending(h => h.ChangedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<LocalCaseFieldHistory>>(history);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<LocalCaseFieldHistory>> GetFieldHistoryAsync(
        int caseId,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var history = _fieldHistory
                .Where(h => h.CaseId == caseId && 
                           h.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.ChangedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<LocalCaseFieldHistory>>(history);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetHistoryFieldNamesAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var fieldNames = _fieldHistory
                .Where(h => h.CaseId == caseId)
                .Select(h => h.FieldName)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(fieldNames);
        }
    }

    #region Private Helper Methods

    private void UpdateImportStatus(
        bool isImporting,
        double progress,
        string? phase,
        string? errorMessage,
        int? importedCount)
    {
        _importStatus = new ImportStatus(
            IsImporting: isImporting,
            Progress: progress,
            CurrentPhase: phase,
            ErrorMessage: errorMessage,
            ImportedCount: importedCount,
            StartedAt: isImporting && _importStatus.StartedAt == null ? DateTime.UtcNow : _importStatus.StartedAt,
            CompletedAt: !isImporting && _importStatus.StartedAt != null ? DateTime.UtcNow : _importStatus.CompletedAt);
    }

    /// <summary>
    /// Parses SQL content into individual statements.
    /// </summary>
    private static List<string> ParseSqlStatements(string sqlContent)
    {
        var statements = new List<string>();
        
        // Remove comments
        var cleaned = Regex.Replace(sqlContent, @"--[^\n]*", "");
        cleaned = Regex.Replace(cleaned, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Split by semicolons, but be careful with strings
        var parts = cleaned.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && 
                trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            {
                statements.Add(trimmed);
            }
        }

        return statements;
    }

    /// <summary>
    /// Parses an INSERT statement into a LocalCase record.
    /// </summary>
    private static LocalCase? ParseCaseInsertStatement(string statement)
    {
        // Pattern: INSERT INTO cases (...) VALUES (...);
        var match = Regex.Match(
            statement,
            @"INSERT\s+INTO\s+cases\s*\(([^)]+)\)\s*VALUES\s*\(([^)]+)\)",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            // Try without column list (assumes all columns in order)
            match = Regex.Match(
                statement,
                @"INSERT\s+INTO\s+cases\s+VALUES\s*\(([^)]+)\)",
                RegexOptions.IgnoreCase);
            
            if (!match.Success)
            {
                return null;
            }
        }

        try
        {
            // Parse column values
            var valuesString = match.Groups[2].Value;
            var values = ParseValues(valuesString);

            // Map values to LocalCase (assuming column order from standard schema)
            // In production, would parse column names and map properly
            return new LocalCase(
                Id: GetIntValue(values, 0),
                ReferenceCode: GetStringValue(values, 1) ?? "UNKNOWN",
                CrimeDate: GetDateTimeValue(values, 2),
                CrimeType: GetStringValue(values, 3),
                CaseType: GetStringValue(values, 4),
                VictimName: GetStringValue(values, 5),
                AccusedName: GetStringValue(values, 6),
                LocationCity: GetStringValue(values, 7),
                LocationState: GetStringValue(values, 8),
                JudicialStatus: GetStringValue(values, 9),
                Description: GetStringValue(values, 10),
                ConfidenceScore: GetIntValue(values, 11, 50),
                IsVerified: GetBoolValue(values, 12),
                IsSensitiveContent: GetBoolValue(values, 13),
                CreatedAt: GetDateTimeValue(values, 14) ?? DateTime.UtcNow,
                UpdatedAt: GetDateTimeValue(values, 15) ?? DateTime.UtcNow
            );
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses an INSERT statement for case_field_history table.
    /// </summary>
    private static LocalCaseFieldHistory? ParseCaseFieldHistoryStatement(string statement)
    {
        // Pattern: INSERT INTO case_field_history (...) VALUES (...);
        var match = Regex.Match(
            statement,
            @"INSERT\s+INTO\s+case_field_history\s*\(([^)]+)\)\s*VALUES\s*\(([^)]+)\)",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            // Try without column list (assumes all columns in order)
            match = Regex.Match(
                statement,
                @"INSERT\s+INTO\s+case_field_history\s+VALUES\s*\(([^)]+)\)",
                RegexOptions.IgnoreCase);
            
            if (!match.Success)
            {
                return null;
            }
        }

        try
        {
            // Parse column values
            var valuesString = match.Groups[2].Value;
            var values = ParseValues(valuesString);

            // Map values to LocalCaseFieldHistory
            // Column order based on generator schema: id, case_id, field_name, old_value, new_value, 
            // changed_at, curator_id, change_reason, change_confidence, created_at
            return new LocalCaseFieldHistory(
                Id: GetIntValue(values, 0),
                CaseId: GetIntValue(values, 1),
                FieldName: GetStringValue(values, 2) ?? "Unknown",
                OldValue: GetStringValue(values, 3),
                NewValue: GetStringValue(values, 4),
                ChangedAt: GetDateTimeValue(values, 5) ?? DateTime.UtcNow,
                CuratorId: GetStringValue(values, 6),
                ChangeReason: GetStringValue(values, 7),
                ChangeConfidence: GetIntValue(values, 8, 50),
                CreatedAt: GetDateTimeValue(values, 9) ?? DateTime.UtcNow
            );
        }
        catch
        {
            return null;
        }
    }

    private static List<string?> ParseValues(string valuesString)
    {
        var values = new List<string?>();
        var current = "";
        var inString = false;
        var escapeNext = false;

        foreach (var c in valuesString)
        {
            if (escapeNext)
            {
                current += c;
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                continue;
            }

            if (c == '\'' && !inString)
            {
                inString = true;
                continue;
            }

            if (c == '\'' && inString)
            {
                inString = false;
                continue;
            }

            if (c == ',' && !inString)
            {
                values.Add(TrimAndUnquote(current));
                current = "";
                continue;
            }

            current += c;
        }

        // Add last value
        if (!string.IsNullOrEmpty(current))
        {
            values.Add(TrimAndUnquote(current));
        }

        return values;
    }

    private static string? TrimAndUnquote(string value)
    {
        var trimmed = value.Trim();
        
        if ((trimmed.StartsWith("'") && trimmed.EndsWith("'")) ||
            (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")))
        {
            trimmed = trimmed[1..^1];
        }

        // Unescape common sequences
        trimmed = trimmed
            .Replace("''", "'")
            .Replace("\\n", "\n")
            .Replace("\\t", "\t");

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string? GetStringValue(List<string?> values, int index)
    {
        return index < values.Count ? values[index] : null;
    }

    private static int GetIntValue(List<string?> values, int index, int defaultValue = 0)
    {
        var val = GetStringValue(values, index);
        return int.TryParse(val, out var result) ? result : defaultValue;
    }

    private static bool GetBoolValue(List<string?> values, int index)
    {
        var val = GetStringValue(values, index)?.ToLowerInvariant();
        return val == "true" || val == "1" || val == "t" || val == "yes";
    }

    private static DateTime? GetDateTimeValue(List<string?> values, int index)
    {
        var val = GetStringValue(values, index);
        if (DateTime.TryParse(val, out var result))
        {
            return result;
        }
        return null;
    }

    private static string TruncateString(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    #endregion
}
