namespace AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;

public class CaseTag
{
    public int CaseId { get; set; }
    public int TagId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public virtual Case? Case { get; set; }
    public virtual Tag? Tag { get; set; }
}

