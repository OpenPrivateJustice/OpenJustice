using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenJustice.BrazilExtractor.Data;

/// <summary>
/// Represents the status of an OCR page processing attempt.
/// </summary>
public enum OcrPageStatus
{
    /// <summary>
    /// Page processing is pending or in progress.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Page was successfully processed.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Page processing failed.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Page was skipped (e.g., page limit reached).
    /// </summary>
    Skipped = 3
}

/// <summary>
/// OCR provider used for processing.
/// </summary>
public enum OcrProviderType
{
    /// <summary>
    /// Llama.cpp with vision capabilities.
    /// </summary>
    LlamaCpp = 0,

    /// <summary>
    /// OpenAI Vision API.
    /// </summary>
    OpenAI = 1
}

/// <summary>
/// Represents a single page OCR record in the database.
/// This is the primary tracking entity for OCR progress.
/// </summary>
public class OcrPageRecord
{
    /// <summary>
    /// Unique identifier for the record.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Execution date (day of the folder) - part of composite key concept.
    /// </summary>
    [Required]
    public DateTime ExecutionDate { get; set; }

    /// <summary>
    /// Full path to the PDF file or unique identifier.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string PdfPath { get; set; } = string.Empty;

    /// <summary>
    /// Page number within the PDF (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Processing status.
    /// </summary>
    public OcrPageStatus Status { get; set; } = OcrPageStatus.Pending;

    /// <summary>
    /// OCR provider used (llama/openai).
    /// </summary>
    public OcrProviderType Provider { get; set; }

    /// <summary>
    /// SHA256 hash of the PNG image (optional, for deduplication).
    /// </summary>
    [MaxLength(64)]
    public string? ImageHash { get; set; }

    /// <summary>
    /// Number of characters extracted from this page.
    /// </summary>
    public int? CharactersExtracted { get; set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the page processing started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the page processing completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Composite index for efficient queries by document + page.
    /// </summary>
    [NotMapped]
    public string CompositeKey => $"{ExecutionDate:yyyy-MM-dd}|{PdfPath}|{PageNumber}";
}
