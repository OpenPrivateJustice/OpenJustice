namespace OpenJustice.Generator.Infrastructure.Persistence.Entities;

public class Evidence
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Link { get; set; }
    public string? FileName { get; set; }
    public string? Witnesses { get; set; }
    public string? Forensics { get; set; }
    public int Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Case? Case { get; set; }
}

