namespace OpenJustice.BrazilExtractor.Models;

/// <summary>
/// Represents a captured PDF link from a TJGO search result page.
/// Contains the normalized URL and metadata about the link's source position.
/// </summary>
public class TjgoPublicationPdfLink
{
    /// <summary>
    /// The normalized, absolute URL to the PDF publication.
    /// </summary>
    public string NormalizedUrl { get; init; } = string.Empty;

    /// <summary>
    /// Original href attribute as found in the DOM (may be relative).
    /// </summary>
    public string OriginalHref { get; init; } = string.Empty;

    /// <summary>
    /// Zero-based index of this link in DOM order (before de-duplication).
    /// </summary>
    public int DomOrderIndex { get; init; }

    /// <summary>
    /// Optional display text or process context extracted from anchor or parent.
    /// </summary>
    public string? DisplayText { get; init; }

    /// <summary>
    /// The source page index (0 for first page, 1 for second, etc.).
    /// </summary>
    public int SourcePageIndex { get; init; }

    /// <summary>
    /// Timestamp when this link was captured.
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a new PDF link entry.
    /// </summary>
    public static TjgoPublicationPdfLink Create(
        string normalizedUrl,
        string originalHref,
        int domOrderIndex,
        int sourcePageIndex = 0,
        string? displayText = null)
    {
        return new TjgoPublicationPdfLink
        {
            NormalizedUrl = normalizedUrl,
            OriginalHref = originalHref,
            DomOrderIndex = domOrderIndex,
            SourcePageIndex = sourcePageIndex,
            DisplayText = displayText,
            CapturedAt = DateTime.UtcNow
        };
    }
}
