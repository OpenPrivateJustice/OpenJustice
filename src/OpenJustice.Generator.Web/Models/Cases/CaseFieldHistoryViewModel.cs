using System.Text.Json;

namespace OpenJustice.Generator.Web.Models.Cases;

/// <summary>
/// View model for displaying case field history in the UI timeline.
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
    /// Creates a CaseFieldHistoryViewModel from a CaseFieldHistoryDto.
    /// </summary>
    public static CaseFieldHistoryViewModel FromDto(Contracts.Cases.CaseFieldHistoryDto dto)
    {
        return new CaseFieldHistoryViewModel
        {
            Id = dto.Id,
            CaseId = dto.CaseId,
            FieldName = dto.FieldName,
            OldValue = dto.OldValue,
            NewValue = dto.NewValue,
            ChangedAt = dto.ChangedAt,
            CuratorId = dto.CuratorId,
            ChangeReason = dto.ChangeReason,
            ChangeConfidence = dto.ChangeConfidence,
            CreatedAt = dto.CreatedAt
        };
    }

    /// <summary>
    /// Converts a list of DTOs to view models.
    /// </summary>
    public static List<CaseFieldHistoryViewModel> FromDtoList(IEnumerable<Contracts.Cases.CaseFieldHistoryDto> dtos)
    {
        return dtos.Select(FromDto).ToList();
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
    public string FieldName { get; set; } = string.Empty;
    public string FieldDisplayName { get; set; } = string.Empty;
    public List<CaseFieldHistoryViewModel> Entries { get; set; } = new();
    public int ChangeCount => Entries.Count;
    public CaseFieldHistoryViewModel? LatestChange => Entries.OrderByDescending(e => e.ChangedAt).FirstOrDefault();
}
