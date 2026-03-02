namespace OpenJustice.BrazilExtractor.Services.Downloads;

/// <summary>
/// Result of a batch PDF download operation.
/// </summary>
public class PdfDownloadBatchResult
{
    /// <summary>
    /// Whether the batch operation completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total number of URLs attempted.
    /// </summary>
    public int AttemptedCount { get; set; }

    /// <summary>
    /// Number of downloads that succeeded.
    /// </summary>
    public int SucceededCount { get; set; }

    /// <summary>
    /// Number of downloads that failed.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Successfully downloaded file paths.
    /// </summary>
    public IReadOnlyList<string> SucceededFiles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Failed download entries with reasons.
    /// </summary>
    public IReadOnlyList<PdfDownloadFailure> Failures { get; set; } = Array.Empty<PdfDownloadFailure>();

    /// <summary>
    /// Timestamp when the batch started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Timestamp when the batch completed.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Total duration of the batch operation.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;
}

/// <summary>
/// Represents a failed PDF download.
/// </summary>
public class PdfDownloadFailure
{
    /// <summary>
    /// The URL that failed to download.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Reason for the failure.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// HTTP status code if available.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Exception message if applicable.
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// Creates a new failure entry.
    /// </summary>
    public static PdfDownloadFailure Create(string url, string reason, int? httpStatusCode = null, string? exceptionMessage = null)
    {
        return new PdfDownloadFailure
        {
            Url = url,
            Reason = reason,
            HttpStatusCode = httpStatusCode,
            ExceptionMessage = exceptionMessage
        };
    }
}
