namespace AtrocidadesRSS.Generator.Domain.Enums;

/// <summary>
/// Represents the curation status of a case.
/// </summary>
public enum CurationStatus
{
    /// <summary>
    /// Case is pending curation review.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Case has been approved for publication.
    /// </summary>
    Approved = 1,
    
    /// <summary>
    /// Case has been rejected from publication.
    /// </summary>
    Rejected = 2
}
