using System.ComponentModel.DataAnnotations;

namespace OpenJustice.BrazilExtractor;

/// <summary>
/// Configuration options for the BrazilExtractor worker service.
/// </summary>
public class BrazilExtractorOptions
{
    /// <summary>
    /// Base URL for the TJGO (Tribunal de Justiça do Estado de Goiás) portal.
    /// </summary>
    [Required]
    public string TjgoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Full URL for the ConsultaPublicacao page.
    /// </summary>
    [Required]
    public string ConsultaPublicacaoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Number of days to look back for case searches.
    /// [DEPRECATED] This setting is no longer used. The extractor always performs single-day queries.
    /// Use QueryDateWindowStartDate to specify which date to query.
    /// </summary>
    [Obsolete("DateWindowDays is deprecated. The extractor now performs single-day queries only.")]
    public int DateWindowDays { get; set; } = 30;

    /// <summary>
    /// Start date for the query date window. If set, queries will use this specific date.
    /// </summary>
    public DateTime? QueryDateWindowStartDate { get; set; }

    /// <summary>
    /// Enable criminal case mode (different search parameters).
    /// </summary>
    public bool CriminalMode { get; set; } = true;

    /// <summary>
    /// Run browser in headless mode.
    /// </summary>
    public bool HeadlessMode { get; set; } = true;

    /// <summary>
    /// Profile name for extraction (e.g., "daily", "full").
    /// </summary>
    public string Profile { get; set; } = "daily";

    /// <summary>
    /// Base directory for downloading PDF files.
    /// </summary>
    [Required]
    public string DownloadPath { get; set; } = string.Empty;

    /// <summary>
    /// Number of PDFs to process per query.
    /// </summary>
    public int MaxResultsPerQuery { get; set; } = 15;

    /// <summary>
    /// Interval between queries in seconds.
    /// </summary>
    public int QueryIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Enable detailed logging for debugging.
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// Base directory for OCR output files (extracted text).
    /// </summary>
    [Required]
    public string OcrOutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the tessdata directory containing Tesseract language data.
    /// </summary>
    [Required]
    public string TessdataPath { get; set; } = string.Empty;

    /// <summary>
    /// Language for OCR (default: Portuguese - "por").
    /// </summary>
    public string OcrLanguage { get; set; } = "por";

    /// <summary>
    /// Path to the Tesseract executable (if not in PATH).
    /// </summary>
    public string? TesseractExecutablePath { get; set; }

    /// <summary>
    /// Path for OCR failure log file.
    /// </summary>
    public string OcrFailureLogPath { get; set; } = "./ocr_failures.log";
}
