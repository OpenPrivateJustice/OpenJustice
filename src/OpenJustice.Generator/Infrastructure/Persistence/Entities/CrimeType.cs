namespace OpenJustice.Generator.Infrastructure.Persistence.Entities;

public class CrimeType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public virtual ICollection<Case> Cases { get; set; } = new List<Case>();
}

