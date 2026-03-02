namespace OpenJustice.BrazilExtractor.Models;

/// <summary>
/// Represents a single PDF OCR extraction result.
/// </summary>
public class OcrExtractionResult
{
    /// <summary>
    /// The input PDF file path.
    /// </summary>
    public required string PdfFilePath { get; set; }

    /// <summary>
    /// Whether the OCR extraction succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// The extracted text content (UTF-8).
    /// </summary>
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Error message if extraction failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of characters in the extracted text.
    /// </summary>
    public int CharacterCount { get; set; }

    /// <summary>
    /// Number of encoding replacement characters (e.g., �) in the extracted text.
    /// </summary>
    public int EncodingReplacementCharCount { get; set; }

    /// <summary>
    /// Number of pages in the PDF.
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// Whether the output was empty (no text extracted).
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(ExtractedText);

    /// <summary>
    /// Failure reason classification.
    /// </summary>
    public OcrFailureReason? FailureReason { get; set; }
}

/// <summary>
/// Failure reasons for OCR extraction.
/// </summary>
public enum OcrFailureReason
{
    /// <summary>
    /// File not found.
    /// </summary>
    FileNotFound,

    /// <summary>
    /// PDF is corrupted or unreadable.
    /// </summary>
    CorruptPdf,

    /// <summary>
    /// PDF has no readable pages.
    /// </summary>
    EmptyPdf,

    /// <summary>
    /// Tesseract processing failed.
    /// </summary>
    TesseractError,

    /// <summary>
    /// PDF to image conversion failed.
    /// </summary>
    ImageConversionError,

    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown
}

/// <summary>
/// Represents the result of a batch OCR extraction operation.
/// </summary>
public class OcrExtractionBatchResult
{
    /// <summary>
    /// Total number of PDFs processed.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of successful extractions.
    /// </summary>
    public int SucceededCount { get; set; }

    /// <summary>
    /// Number of failed extractions.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Total number of pages processed across all PDFs.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Total characters extracted across all successful PDFs.
    /// </summary>
    public int TotalCharacters { get; set; }

    /// <summary>
    /// Total encoding replacement characters across all PDFs.
    /// </summary>
    public int TotalEncodingReplacementChars { get; set; }

    /// <summary>
    /// Individual PDF extraction results.
    /// </summary>
    public List<OcrExtractionResult> Results { get; set; } = new();

    /// <summary>
    /// Whether all extractions succeeded.
    /// </summary>
    public bool AllSucceeded => FailedCount == 0 && TotalCount > 0;

    /// <summary>
    /// Failure details for failed extractions.
    /// </summary>
    public List<OcrExtractionResult> Failures => Results.Where(r => !r.Succeeded).ToList();
}
