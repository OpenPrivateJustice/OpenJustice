using OpenJustice.BrazilExtractor.Models;

namespace OpenJustice.BrazilExtractor.Services.Ocr;

/// <summary>
/// Service interface for OCR text extraction from PDF files.
/// </summary>
public interface IOcrExtractionService
{
    /// <summary>
    /// Extracts text from multiple PDF files using OCR.
    /// </summary>
    /// <param name="pdfFilePaths">List of PDF file paths to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch result containing extraction outcomes and quality metadata.</returns>
    Task<OcrExtractionBatchResult> ExtractTextAsync(
        IEnumerable<string> pdfFilePaths,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts text from a single PDF file using OCR.
    /// </summary>
    /// <param name="pdfFilePath">PDF file path to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extraction result with quality metadata.</returns>
    Task<OcrExtractionResult> ExtractTextAsync(
        string pdfFilePath,
        CancellationToken cancellationToken = default);
}
