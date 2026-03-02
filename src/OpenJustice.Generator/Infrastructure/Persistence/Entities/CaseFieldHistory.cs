namespace OpenJustice.Generator.Infrastructure.Persistence.Entities;

public class CaseFieldHistory
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? CuratorId { get; set; }
    public string? ChangeReason { get; set; }
    public int ChangeConfidence { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public virtual Case? Case { get; set; }
}

