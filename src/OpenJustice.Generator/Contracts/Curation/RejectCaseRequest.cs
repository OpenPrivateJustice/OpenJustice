namespace OpenJustice.Generator.Contracts.Curation;

/// <summary>
/// Request contract for rejecting a case.
/// </summary>
public class RejectCaseRequest
{
    /// <summary>
    /// The ID of the curator rejecting the case. Required.
    /// </summary>
    public required string CuratorId { get; set; }
    
    /// <summary>
    /// Reason for rejection. Required.
    /// </summary>
    public required string Notes { get; set; }
}
