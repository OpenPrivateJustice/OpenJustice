namespace AtrocidadesRSS.Generator.Contracts.Cases;

/// <summary>
/// DTO for case field history entries.
/// </summary>
public class CaseFieldHistoryDto
{
    /// <summary>
    /// Unique identifier for the history entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The case ID this history entry belongs to.
    /// </summary>
    public int CaseId { get; set; }

    /// <summary>
    /// Name of the field that was changed.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// The previous value of the field (JSON serialized).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// The new value of the field (JSON serialized).
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Timestamp when the change was made (UTC).
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// ID of the curator who made the change.
    /// </summary>
    public string? CuratorId { get; set; }

    /// <summary>
    /// Optional reason for the change.
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Confidence score (0-100) associated with this change.
    /// </summary>
    public int ChangeConfidence { get; set; }

    /// <summary>
    /// Timestamp when this history entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
