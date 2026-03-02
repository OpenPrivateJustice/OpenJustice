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
    /// Number of days to look back for case searches.
    /// </summary>
    public int DateWindowDays { get; set; } = 30;

    /// <summary>
    /// Enable criminal case mode (different search parameters).
    /// </summary>
    public bool CriminalMode { get; set; } = false;

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
}
