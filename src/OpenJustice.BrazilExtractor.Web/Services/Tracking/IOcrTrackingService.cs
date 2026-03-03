using OpenJustice.BrazilExtractor.Data;

namespace OpenJustice.BrazilExtractor.Services.Tracking;

/// <summary>
/// Service interface for OCR tracking operations abstraction.
/// Provides an layer over the database for tracking OCR progress.
/// </summary>
public interface IOcrTrackingService
{
    /// <summary>
    /// Checks if a specific page has already been successfully processed.
    /// This is the primary method used to decide whether to skip a page.
    /// </summary>
    /// <param name="executionDate">The execution date (folder day).</param>
    /// <param name="pdfPath">Full path to the PDF file.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the page was already successfully processed, false otherwise.</returns>
    Task<bool> IsPageProcessedAsync(DateTime executionDate, string pdfPath, int pageNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all successfully processed pages for a specific PDF.
    /// </summary>
    /// <param name="executionDate">The execution date.</param>
    /// <param name="pdfPath">Full path to the PDF file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HashSet of successfully processed page numbers.</returns>
    Task<HashSet<int>> GetProcessedPagesAsync(DateTime executionDate, string pdfPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending pages for a specific date (useful for resuming).
    /// </summary>
    /// <param name="executionDate">The execution date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of OcrPageRecord for pages not yet successfully processed.</returns>
    Task<List<OcrPageRecord>> GetPendingPagesAsync(DateTime executionDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending pages for a specific PDF (useful for resuming).
    /// </summary>
    /// <param name="executionDate">The execution date.</param>
    /// <param name="pdfPath">Full path to the PDF file.</param>
    /// <param name="totalPages">Total number of pages in the PDF.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of page numbers that need processing.</returns>
    Task<List<int>> GetPendingPageNumbersAsync(DateTime executionDate, string pdfPath, int totalPages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the start of page processing.
    /// </summary>
    Task<OcrPageRecord> RecordPageStartedAsync(
        DateTime executionDate,
        string pdfPath,
        int pageNumber,
        OcrProviderType provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records successful completion of page processing.
    /// </summary>
    Task RecordPageSuccessAsync(
        int recordId,
        string? imageHash,
        int charactersExtracted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records failed page processing.
    /// </summary>
    Task RecordPageFailedAsync(
        int recordId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records skipped page (e.g., due to page limit).
    /// </summary>
    Task RecordPageSkippedAsync(
        DateTime executionDate,
        string pdfPath,
        int pageNumber,
        OcrProviderType provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new page record for tracking.
    /// </summary>
    Task<OcrPageRecord> CreatePageRecordAsync(
        DateTime executionDate,
        string pdfPath,
        int pageNumber,
        OcrProviderType provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of processing status for a PDF.
    /// </summary>
    Task<(int total, int success, int failed, int pending)> GetPdfStatusSummaryAsync(
        DateTime executionDate,
        string pdfPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of processing status for a date.
    /// </summary>
    Task<(int total, int success, int failed, int pending)> GetDateStatusSummaryAsync(
        DateTime executionDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all records for a specific PDF (for audit).
    /// </summary>
    Task<List<OcrPageRecord>> GetRecordsForPdfAsync(
        DateTime executionDate,
        string pdfPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the database (creates tables if not exist).
    /// </summary>
    Task InitializeDatabaseAsync(CancellationToken cancellationToken = default);
}
