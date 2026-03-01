namespace AtrocidadesRSS.Reader.Services.Data;

/// <summary>
/// Represents a case entity for local querying.
/// </summary>
public record LocalCase(
    int Id,
    string ReferenceCode,
    DateTime? CrimeDate,
    string? CrimeType,
    string? CaseType,
    string? VictimName,
    string? AccusedName,
    string? LocationCity,
    string? LocationState,
    string? JudicialStatus,
    string? Description,
    int ConfidenceScore,
    bool IsVerified,
    bool IsSensitiveContent,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Search filter for querying local cases.
/// </summary>
public record CaseSearchFilter(
    string? Query = null,
    string? CrimeType = null,
    string? LocationState = null,
    string? JudicialStatus = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    int Page = 1,
    int PageSize = 20
);

/// <summary>
/// Paginated result for case searches.
/// </summary>
public record CaseSearchResult(
    IReadOnlyList<LocalCase> Cases,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>
/// Import status information.
/// </summary>
public record ImportStatus(
    bool IsImporting,
    double Progress,
    string? CurrentPhase,
    string? ErrorMessage,
    int? ImportedCount,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

/// <summary>
/// Result of an import operation.
/// </summary>
public record ImportResult(
    bool Success,
    int RecordsImported,
    string? ErrorMessage,
    IReadOnlyList<string>? Warnings
);

/// <summary>
/// Interface for local case storage and querying.
/// </summary>
public interface ILocalCaseStore
{
    /// <summary>
    /// Gets the current import status.
    /// </summary>
    ImportStatus GetImportStatus();

    /// <summary>
    /// Imports a SQL snapshot into the local store.
    /// </summary>
    Task<ImportResult> ImportSnapshotAsync(
        string sqlContent,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports from a file path (for downloaded snapshots).
    /// </summary>
    Task<ImportResult> ImportFromFileAsync(
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the local store has data.
    /// </summary>
    bool HasData();

    /// <summary>
    /// Gets a case by ID.
    /// </summary>
    Task<LocalCase?> GetCaseByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a case by reference code.
    /// </summary>
    Task<LocalCase?> GetCaseByReferenceCodeAsync(string referenceCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches cases with filters.
    /// </summary>
    Task<CaseSearchResult> SearchCasesAsync(
        CaseSearchFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all crime types for filtering.
    /// </summary>
    Task<IReadOnlyList<string>> GetCrimeTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all judicial statuses for filtering.
    /// </summary>
    Task<IReadOnlyList<string>> GetJudicialStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all states for filtering.
    /// </summary>
    Task<IReadOnlyList<string>> GetStatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the database statistics.
    /// </summary>
    Task<DatabaseStats> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all data from the local store.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Database statistics.
/// </summary>
public record DatabaseStats(
    int TotalCases,
    int VerifiedCases,
    int SensitiveCases,
    DateTime? LastUpdated,
    string? CurrentVersion
);
