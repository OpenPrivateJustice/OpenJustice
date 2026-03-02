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
    /// Captured PDF publication links from the result page.
    /// Non-null, empty list if no links found.
    /// </summary>
    public IReadOnlyList<TjgoPublicationPdfLink> PdfLinks { get; set; } = Array.Empty<TjgoPublicationPdfLink>();

    /// <summary>
    /// Total number of PDF link candidates seen on the page (before de-duplication).
    /// </summary>
    public int TotalLinksSeen { get; set; }

    /// <summary>
    /// Number of unique links retained after de-duplication.
    /// </summary>
    public int UniqueLinksRetained { get; set; }

    /// <summary>
    /// Whether the result was capped at MaxResultsPerQuery.
    /// </summary>
    public bool WasCapped { get; set; }

    /// <summary>
    /// The configured max results per query limit that was applied.
    /// </summary>
    public int MaxResultsPerQuery { get; set; }

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
            DateWindow = query != null ? $"{query.FormattedDate} to {query.FormattedDate}" : null,
            PdfLinks = Array.Empty<TjgoPublicationPdfLink>(),
            TotalLinksSeen = 0,
            UniqueLinksRetained = 0,
            WasCapped = false,
            MaxResultsPerQuery = 0
        };
    }

    /// <summary>
    /// Creates a successful result with captured PDF links.
    /// </summary>
    public static TjgoSearchResult SuccessfulWithPdfLinks(
        string resultUrl,
        int recordCount,
        IReadOnlyList<TjgoPublicationPdfLink> pdfLinks,
        int totalLinksSeen,
        int maxResultsPerQuery,
        TjgoSearchQuery? query = null)
    {
        var wasCapped = pdfLinks.Count >= maxResultsPerQuery;
        
        return new TjgoSearchResult
        {
            Success = true,
            ResultUrl = resultUrl,
            RecordCount = recordCount,
            Query = query,
            AppliedFilterProfile = query?.CriminalFilter?.Name,
            DateWindow = query != null ? $"{query.FormattedDate} to {query.FormattedDate}" : null,
            PdfLinks = pdfLinks,
            TotalLinksSeen = totalLinksSeen,
            UniqueLinksRetained = pdfLinks.Count,
            WasCapped = wasCapped,
            MaxResultsPerQuery = maxResultsPerQuery
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
            DateWindow = query != null ? $"{query.FormattedDate} to {query.FormattedDate}" : null,
            PdfLinks = Array.Empty<TjgoPublicationPdfLink>(),
            TotalLinksSeen = 0,
            UniqueLinksRetained = 0,
            WasCapped = false,
            MaxResultsPerQuery = 0
        };
    }
}
