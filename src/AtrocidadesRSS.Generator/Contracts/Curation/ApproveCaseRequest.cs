namespace AtrocidadesRSS.Generator.Contracts.Curation;

/// <summary>
/// Request contract for approving a case.
/// </summary>
public class ApproveCaseRequest
{
    /// <summary>
    /// The ID of the curator approving the case. Required.
    /// </summary>
    public required string CuratorId { get; set; }
    
    /// <summary>
    /// Optional notes for the approval.
    /// </summary>
    public string? Notes { get; set; }
}
