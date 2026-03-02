using System.Text.Json;

namespace OpenJustice.Reader.Models.Cases;

/// <summary>
/// View model for displaying case field history in the UI timeline.
/// Mirrors the generator's CaseFieldHistoryViewModel pattern.
/// </summary>
public class CaseFieldHistoryViewModel
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
    /// Display name for the field (human-readable).
    /// </summary>
    public string FieldDisplayName => FormatFieldName(FieldName);

    /// <summary>
    /// The previous value of the field.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// The new value of the field.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Display-friendly old value (null-safe, trimmed).
    /// </summary>
    public string OldValueDisplay => FormatValue(OldValue);

    /// <summary>
    /// Display-friendly new value (null-safe, trimmed).
    /// </summary>
    public string NewValueDisplay => FormatValue(NewValue);

    /// <summary>
    /// Timestamp when the change was made (UTC).
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Localized display string for ChangedAt.
    /// </summary>
    public string ChangedAtDisplay => ChangedAt.ToString("dd/MM/yyyy HH:mm");

    /// <summary>
    /// ID of the curator who made the change.
    /// </summary>
    public string? CuratorId { get; set; }

    /// <summary>
    /// Display name for curator (falls back to "Sistema" if null).
    /// </summary>
    public string CuratorDisplay => string.IsNullOrEmpty(CuratorId) ? "Sistema" : CuratorId;

    /// <summary>
    /// Optional reason for the change.
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Confidence score (0-100) associated with this change.
    /// </summary>
    public int ChangeConfidence { get; set; }

    /// <summary>
    /// Display-friendly confidence level (e.g., "Alta", "Média", "Baixa").
    /// </summary>
    public string ConfidenceLevel => ChangeConfidence switch
    {
        >= 80 => "Alta",
        >= 50 => "Média",
        _ => "Baixa"
    };

    /// <summary>
    /// CSS class for confidence badge styling.
    /// </summary>
    public string ConfidenceBadgeClass => ChangeConfidence switch
    {
        >= 80 => "bg-success",
        >= 50 => "bg-warning",
        _ => "bg-danger"
    };

    /// <summary>
    /// Timestamp when this history entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creates a CaseFieldHistoryViewModel from a LocalCaseFieldHistory entity.
    /// </summary>
    public static CaseFieldHistoryViewModel FromEntity(Services.Data.LocalCaseFieldHistory entity)
    {
        return new CaseFieldHistoryViewModel
        {
            Id = entity.Id,
            CaseId = entity.CaseId,
            FieldName = entity.FieldName,
            OldValue = entity.OldValue,
            NewValue = entity.NewValue,
            ChangedAt = entity.ChangedAt,
            CuratorId = entity.CuratorId,
            ChangeReason = entity.ChangeReason,
            ChangeConfidence = entity.ChangeConfidence,
            CreatedAt = entity.CreatedAt
        };
    }

    /// <summary>
    /// Converts a list of entities to view models.
    /// </summary>
    public static List<CaseFieldHistoryViewModel> FromEntityList(
        IEnumerable<Services.Data.LocalCaseFieldHistory> entities)
    {
        return entities.Select(FromEntity).ToList();
    }

    private static string FormatFieldName(string fieldName)
    {
        // Convert PascalCase to Title Case with spaces
        if (string.IsNullOrEmpty(fieldName))
            return fieldName;

        var result = string.Concat(fieldName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));

        return char.ToUpper(result[0]) + result[1..];
    }

    private static string FormatValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(vazio)";

        // Try to deserialize JSON for better display
        try
        {
            using var doc = JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
                return doc.RootElement.GetString() ?? value;
            
            return doc.RootElement.GetRawText();
        }
        catch
        {
            return value.Trim();
        }
    }
}

/// <summary>
/// Grouping of history entries by field name for timeline display.
/// </summary>
public class FieldHistoryGroup
{
    /// <summary>
    /// The field name.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string FieldDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// History entries for this field, ordered newest first.
    /// </summary>
    public List<CaseFieldHistoryViewModel> Entries { get; set; } = new();

    /// <summary>
    /// Number of changes in this field.
    /// </summary>
    public int ChangeCount => Entries.Count;

    /// <summary>
    /// The most recent change for this field.
    /// </summary>
    public CaseFieldHistoryViewModel? LatestChange => 
        Entries.OrderByDescending(e => e.ChangedAt).FirstOrDefault();
}

/// <summary>
/// Selection model for comparing two history versions (A/B diff).
/// </summary>
public class FieldDiffSelection
{
    /// <summary>
    /// The selected "older" version (A).
    /// </summary>
    public CaseFieldHistoryViewModel? VersionA { get; set; }

    /// <summary>
    /// The selected "newer" version (B).
    /// </summary>
    public CaseFieldHistoryViewModel? VersionB { get; set; }

    /// <summary>
    /// Whether a valid comparison can be made.
    /// </summary>
    public bool IsValid => VersionA != null && VersionB != null;

    /// <summary>
    /// Field name being compared.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the field.
    /// </summary>
    public string FieldDisplayName => FormatFieldName(FieldName);

    private static string FormatFieldName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
            return fieldName;

        var result = string.Concat(fieldName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));

        return char.ToUpper(result[0]) + result[1..];
    }
}
