namespace OpenJustice.Generator.Infrastructure.Persistence.Entities;

public class Source
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public DateTime PostDate { get; set; }
    public string? OriginalLink { get; set; }
    public int? Upvotes { get; set; }
    public int? CommentsCount { get; set; }
    public string? CurationNotes { get; set; }
    public int Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Case? Case { get; set; }
}

