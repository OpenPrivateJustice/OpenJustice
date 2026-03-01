using AtrocidadesRSS.Generator.Domain.Enums;

namespace AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;

/// <summary>
/// Represents a discovered case candidate from automated discovery sources (RSS feeds, Reddit threads).
/// These candidates require curator review before being promoted to official cases.
/// </summary>
public class DiscoveredCase
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique identifier for this discovered item (hash of source URL for deduplication).
    /// </summary>
    public string DiscoveryHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Title of the discovered item.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Summary or description extracted from the source.
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// URL of the original source.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the source (RSS feed name or subreddit).
    /// </summary>
    public string SourceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of discovery source (RSS, Reddit).
    /// </summary>
    public DiscoverySourceType SourceType { get; set; }
    
    /// <summary>
    /// Publication date from the source.
    /// </summary>
    public DateTime? PublishedDate { get; set; }
    
    /// <summary>
    /// Date when this item was discovered by the system.
    /// </summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Current status of the discovered case in the review workflow.
    /// </summary>
    public DiscoveryStatus Status { get; set; } = DiscoveryStatus.Pending;
    
    /// <summary>
    /// ID of the curator who performed the review action.
    /// </summary>
    public string? ReviewedBy { get; set; }
    
    /// <summary>
    /// Timestamp of the review action.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }
    
    /// <summary>
    /// Reason for rejection or notes from review.
    /// </summary>
    public string? ReviewNotes { get; set; }
    
    /// <summary>
    /// ID of the promoted case (if approved and promoted to official case).
    /// </summary>
    public int? PromotedCaseId { get; set; }
    
    /// <summary>
    /// Raw JSON content from the source for traceability.
    /// </summary>
    public string? RawContent { get; set; }
    
    /// <summary>
    /// Additional metadata extracted from the source.
    /// </summary>
    public string? Metadata { get; set; }
    
    // Navigation Property
    public virtual Case? PromotedCase { get; set; }
}
