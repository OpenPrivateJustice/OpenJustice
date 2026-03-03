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
    /// Maximum number of PDFs to process per query.
    /// Set to 0 for unlimited (traverse all pagination pages).
    /// </summary>
    public int MaxResultsPerQuery { get; set; } = 0;

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
    /// Path for OCR failure log file.
    /// </summary>
    public string OcrFailureLogPath { get; set; } = "./ocr_failures.log";

    /// <summary>
    /// OpenAI API Key for vision OCR (from environment variable or secrets).
    /// </summary>
    public string? OpenAiApiKey { get; set; }

    /// <summary>
    /// OpenAI Vision model to use (default: gpt-4o-mini).
    /// </summary>
    public string OpenAiVisionModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Enable OpenAI Vision OCR.
    /// </summary>
    public bool UseOpenAiVision { get; set; } = false;

    /// <summary>
    /// Enable llama.cpp Vision OCR (OpenAI-compatible local server).
    /// </summary>
    public bool UseLlamaCppVision { get; set; } = false;

    /// <summary>
    /// Base URL for llama.cpp server (e.g., http://localhost:8080 or http://localhost:8080/v1).
    /// </summary>
    public string LlamaCppBaseUrl { get; set; } = "http://localhost:8080";

    /// <summary>
    /// Vision-capable model name loaded in llama.cpp server.
    /// </summary>
    public string LlamaCppVisionModel { get; set; } = "llava";

    /// <summary>
    /// Optional API key for llama.cpp server (if configured behind auth).
    /// </summary>
    public string? LlamaCppApiKey { get; set; }

    /// <summary>
    /// Maximum number of pages rendered to images per PDF when using llama.cpp vision OCR.
    /// Set to 0 or null for unlimited.
    /// </summary>
    public int LlamaCppMaxPagesPerPdf { get; set; } = 0;

    /// <summary>
    /// Timeout in seconds for llama.cpp requests.
    /// </summary>
    public int LlamaCppRequestTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Max tokens for llama.cpp chat completion response.
    /// </summary>
    public int LlamaCppMaxTokens { get; set; } = 2048;
}
