using OpenJustice.BrazilExtractor.Services.Tjgo;

namespace OpenJustice.BrazilExtractor.Models;

/// <summary>
/// Represents a TJGO search result after form submission.
/// </summary>
public class TjgoSearchResult
{
    /// <summary>
    /// Whether the search was successful and returned results.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The URL after form submission (for debugging/verification).
    /// </summary>
    public string? ResultUrl { get; set; }

    /// <summary>
    /// Number of records found (if available).
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Error message if the search failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The query that produced this result.
    /// </summary>
    public TjgoSearchQuery? Query { get; set; }

    /// <summary>
    /// The applied criminal filter profile name (for auditability).
    /// </summary>
    public string? AppliedFilterProfile { get; set; }

    /// <summary>
    /// Number of records excluded by criminal filter (if applicable).
    /// </summary>
    public int ExcludedRecordCount { get; set; }

    /// <summary>
    /// The date window used in the query.
    /// </summary>
    public string? DateWindow { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TjgoSearchResult Successful(string resultUrl, int recordCount = 0, TjgoSearchQuery? query = null)
    {
        return new TjgoSearchResult
        {
            Success = true,
            ResultUrl = resultUrl,
            RecordCount = recordCount,
            Query = query,
            AppliedFilterProfile = query?.CriminalFilter?.Name,
            DateWindow = query != null ? $"{query.FormattedDate} to {query.FormattedDate}" : null
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static TjgoSearchResult Failed(string errorMessage, TjgoSearchQuery? query = null)
    {
        return new TjgoSearchResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Query = query,
            AppliedFilterProfile = query?.CriminalFilter?.Name,
            DateWindow = query != null ? $"{query.FormattedDate} to {query.FormattedDate}" : null
        };
    }
}
