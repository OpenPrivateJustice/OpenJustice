namespace OpenJustice.BrazilExtractor.Web.Services;

/// <summary>
/// OCR provider types available for text extraction.
/// </summary>
public enum OcrProvider
{
    /// <summary>
    /// OpenAI Vision API (cloud, paid).
    /// </summary>
    OpenAI,

    /// <summary>
    /// llama.cpp server with vision model (local, free).
    /// </summary>
    LlamaCpp
}

/// <summary>
/// Represents the current state of the extractor.
/// </summary>
public class ExtractorState
{
    /// <summary>
    /// Legacy aggregate status (kept for compatibility).
    /// </summary>
    public ExtractorStatus Status { get; set; } = ExtractorStatus.Idle;

    /// <summary>
    /// Status of "Run Extraction" workflow (search + download + OCR of downloaded PDFs).
    /// </summary>
    public ExtractorStatus ExtractionStatus { get; set; } = ExtractorStatus.Idle;

    /// <summary>
    /// Status of "OCR Only" workflow.
    /// </summary>
    public ExtractorStatus OcrOnlyStatus { get; set; } = ExtractorStatus.Idle;

    // Extraction workflow telemetry
    public DateTime? LastRunTime { get; set; }
    public DateTime? LastSuccessfulRun { get; set; }
    public string? LastError { get; set; }
    public string? LastDateQueried { get; set; }
    public int LastPdfCount { get; set; }
    public int LastOcrSucceeded { get; set; }
    public int LastOcrFailed { get; set; }

    // OCR-only workflow telemetry
    public DateTime? LastOcrOnlyRunTime { get; set; }
    public DateTime? LastOcrOnlySuccessfulRun { get; set; }
    public string? LastOcrOnlyError { get; set; }
    public string? LastOcrOnlyDateQueried { get; set; }
    public int LastOcrOnlySucceeded { get; set; }
    public int LastOcrOnlyFailed { get; set; }

    public List<string> ExtractionLogs { get; set; } = new();
    public List<string> OcrOnlyLogs { get; set; } = new();
    
    /// <summary>
    /// Selected OCR provider for extractions.
    /// </summary>
    public OcrProvider SelectedOcrProvider { get; set; } = OcrProvider.OpenAI;
}

public enum ExtractorStatus
{
    Idle,
    Running,
    RunningOcr,
    Completed,
    Error
}
