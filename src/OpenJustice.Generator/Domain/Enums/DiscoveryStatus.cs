namespace OpenJustice.Generator.Domain.Enums;

/// <summary>
/// Represents the status of a discovered case in the review workflow.
/// </summary>
public enum DiscoveryStatus
{
    /// <summary>
    /// Discovered item is pending curator review.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Discovered item has been approved and promoted to a draft case.
    /// </summary>
    Approved = 1,
    
    /// <summary>
    /// Discovered item has been rejected by curator.
    /// </summary>
    Rejected = 2
}
