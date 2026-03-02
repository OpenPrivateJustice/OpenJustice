namespace OpenJustice.Generator.Infrastructure.Persistence.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public virtual ICollection<CaseTag> CaseTags { get; set; } = new List<CaseTag>();
}

