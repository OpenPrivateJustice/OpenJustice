using OpenJustice.Generator.Domain.Enums;

namespace OpenJustice.Generator.Infrastructure.Persistence.Entities;

/// <summary>
/// Immutable audit log entry for case curation actions.
/// This is an append-only table - entries are never updated or deleted.
/// </summary>
public class CaseAuditLog
{
    public int Id { get; set; }
    
    /// <summary>
    /// The case ID this audit entry pertains to.
    /// </summary>
    public int CaseId { get; set; }
    
    /// <summary>
    /// The type of action performed (Approve, Reject, Verify).
    /// </summary>
    public string ActionType { get; set; } = string.Empty;
    
    /// <summary>
    /// The previous curation status before this action.
    /// </summary>
    public CurationStatus? PreviousStatus { get; set; }
    
    /// <summary>
    /// The new curation status after this action.
    /// </summary>
    public CurationStatus? NewStatus { get; set; }
    
    /// <summary>
    /// The curator who performed this action.
    /// </summary>
    public string? CuratorId { get; set; }
    
    /// <summary>
    /// Optional notes or reason for the action.
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Timestamp when this action was performed (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    // Navigation Property
    public virtual Case? Case { get; set; }
}

/// <summary>
/// Constants for audit action types.
/// </summary>
public static class AuditActionTypes
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Verified = "Verified";
    public const string Unverified = "Unverified";
}
