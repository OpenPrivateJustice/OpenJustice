using OpenJustice.Generator.Domain.Enums;

namespace OpenJustice.Generator.Contracts.Discovery;

/// <summary>
/// Data transfer object for discovered case.
/// </summary>
public class DiscoveredCaseDto
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Title of the discovered item.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Summary or description.
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// URL of the original source.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the source.
    /// </summary>
    public string SourceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of discovery source.
    /// </summary>
    public DiscoverySourceType SourceType { get; set; }
    
    /// <summary>
    /// Publication date from the source.
    /// </summary>
    public DateTime? PublishedDate { get; set; }
    
    /// <summary>
    /// Date when discovered by the system.
    /// </summary>
    public DateTime DiscoveredAt { get; set; }
    
    /// <summary>
    /// Current status.
    /// </summary>
    public DiscoveryStatus Status { get; set; }
    
    /// <summary>
    /// ID of the curator who reviewed.
    /// </summary>
    public string? ReviewedBy { get; set; }
    
    /// <summary>
    /// Timestamp of review.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }
    
    /// <summary>
    /// Notes from review.
    /// </summary>
    public string? ReviewNotes { get; set; }
    
    /// <summary>
    /// ID of the promoted case (if approved).
    /// </summary>
    public int? PromotedCaseId { get; set; }
}

/// <summary>
/// Request to approve a discovered case.
/// </summary>
public class ApproveDiscoveredCaseRequest
{
    /// <summary>
    /// ID of the curator performing the action.
    /// </summary>
    public string CuratorId { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional notes for the approval.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Request to reject a discovered case.
/// </summary>
public class RejectDiscoveredCaseRequest
{
    /// <summary>
    /// ID of the curator performing the action.
    /// </summary>
    public string CuratorId { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason for rejection.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request to promote an approved discovered case to a draft case.
/// </summary>
public class PromoteToCaseRequest
{
    /// <summary>
    /// ID of the curator performing the action.
    /// </summary>
    public string CuratorId { get; set; } = string.Empty;
    
    /// <summary>
    /// Crime type ID for the promoted case.
    /// </summary>
    public int CrimeTypeId { get; set; }
    
    /// <summary>
    /// Case type ID (consumado/tentativa).
    /// </summary>
    public int CaseTypeId { get; set; }
    
    /// <summary>
    /// Judicial status ID.
    /// </summary>
    public int JudicialStatusId { get; set; }
    
    /// <summary>
    /// Victim name (optional).
    /// </summary>
    public string? VictimName { get; set; }
    
    /// <summary>
    /// Victim confidence score.
    /// </summary>
    public int VictimConfidence { get; set; } = 50;
    
    /// <summary>
    /// Accused name (optional).
    /// </summary>
    public string? AccusedName { get; set; }
    
    /// <summary>
    /// Accused confidence score.
    /// </summary>
    public int AccusedConfidence { get; set; } = 50;
    
    /// <summary>
    /// Crime description.
    /// </summary>
    public string? CrimeDescription { get; set; }
    
    /// <summary>
    /// Crime confidence score.
    /// </summary>
    public int CrimeConfidence { get; set; } = 50;
    
    /// <summary>
    /// Judicial confidence score.
    /// </summary>
    public int JudicialConfidence { get; set; } = 50;
}
