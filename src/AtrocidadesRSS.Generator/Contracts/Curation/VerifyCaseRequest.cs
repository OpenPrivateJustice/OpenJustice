namespace AtrocidadesRSS.Generator.Contracts.Curation;

/// <summary>
/// Request contract for verifying a case.
/// </summary>
public class VerifyCaseRequest
{
    /// <summary>
    /// The ID of the curator verifying the case. Required.
    /// </summary>
    public required string CuratorId { get; set; }
    
    /// <summary>
    /// Optional notes for the verification.
    /// </summary>
    public string? Notes { get; set; }
}
