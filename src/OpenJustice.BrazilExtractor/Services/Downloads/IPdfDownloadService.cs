using OpenJustice.BrazilExtractor.Models;

namespace OpenJustice.BrazilExtractor.Services.Downloads;

/// <summary>
/// Service for downloading PDF files from harvested URLs.
/// Provides persistence with collision-safe unique naming.
/// </summary>
public interface IPdfDownloadService
{
    /// <summary>
    /// Downloads a batch of PDF files from the provided URLs.
    /// </summary>
    /// <param name="pdfLinks">PDF links to download.</param>
    /// <param name="queryDate">The query date for naming purposes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch result with success/failure details and file paths.</returns>
    Task<PdfDownloadBatchResult> DownloadBatchAsync(
        IReadOnlyList<TjgoPublicationPdfLink> pdfLinks,
        DateTime queryDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configured download directory path.
    /// </summary>
    string DownloadPath { get; }
}
