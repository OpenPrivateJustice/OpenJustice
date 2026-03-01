using System.Text.Json;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtrocidadesRSS.Generator.Services.History;

/// <summary>
/// Implementation of append-only case field history tracking.
/// This service compares field values before/after updates and persists only inserts (never updates/deletes).
/// </summary>
public class CaseFieldHistoryService : ICaseFieldHistoryService
{
    private readonly AppDbContext _context;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    // Fields to track for history - mutable fields that change during case updates
    private static readonly string[] TrackedFields = new[]
    {
        // Victim Information
        nameof(Case.VictimName),
        nameof(Case.VictimGender),
        nameof(Case.VictimAge),
        nameof(Case.VictimNationality),
        nameof(Case.VictimProfession),
        nameof(Case.VictimRelationshipToAccused),
        nameof(Case.VictimConfidence),
        
        // Accused Information
        nameof(Case.AccusedName),
        nameof(Case.AccusedSocialName),
        nameof(Case.AccusedGender),
        nameof(Case.AccusedAge),
        nameof(Case.AccusedNationality),
        nameof(Case.AccusedProfession),
        nameof(Case.AccusedDocument),
        nameof(Case.AccusedAddress),
        nameof(Case.AccusedRelationshipToVictim),
        nameof(Case.AccusedConfidence),
        
        // Crime Details
        nameof(Case.CrimeTypeId),
        nameof(Case.CrimeSubtype),
        nameof(Case.EstimatedCrimeDateTime),
        nameof(Case.CrimeDate),
        nameof(Case.ReportDate),
        nameof(Case.CrimeLocationAddress),
        nameof(Case.CrimeLocationCity),
        nameof(Case.CrimeLocationState),
        nameof(Case.CrimeCoordinates),
        nameof(Case.CrimeDescription),
        nameof(Case.CaseTypeId),
        nameof(Case.NumberOfVictims),
        nameof(Case.NumberOfAccused),
        nameof(Case.WeaponUsed),
        nameof(Case.Motivation),
        nameof(Case.Premeditation),
        nameof(Case.CrimeConfidence),
        
        // Judicial Information
        nameof(Case.JudicialStatusId),
        nameof(Case.ProcessNumber),
        nameof(Case.Court),
        nameof(Case.County),
        nameof(Case.CurrentPhase),
        nameof(Case.JudicialReportDate),
        nameof(Case.SentencingDate),
        nameof(Case.Sentence),
        nameof(Case.PendingAppeals),
        nameof(Case.JudicialConfidence),
        
        // Classification
        nameof(Case.MainCategory),
        nameof(Case.IsSensitiveContent),
        nameof(Case.IsVerified),
        nameof(Case.AnonymizationStatus)
    };

    public CaseFieldHistoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<List<CaseFieldHistory>> AppendChangesAsync(
        int caseId,
        Case? oldCase,
        Case newCase,
        string? curatorId,
        CancellationToken cancellationToken = default)
    {
        var historyEntries = new List<CaseFieldHistory>();
        var now = DateTime.UtcNow;

        foreach (var fieldName in TrackedFields)
        {
            var oldValue = GetFieldValue(oldCase, fieldName);
            var newValue = GetFieldValue(newCase, fieldName);

            // Only create history entry if value changed
            if (!ValuesEqual(oldValue, newValue))
            {
                // Determine the confidence score for this field change
                var confidence = GetConfidenceForField(newCase, fieldName);

                var historyEntry = new CaseFieldHistory
                {
                    CaseId = caseId,
                    FieldName = fieldName,
                    OldValue = SerializeValue(oldValue),
                    NewValue = SerializeValue(newValue),
                    ChangedAt = now,
                    CuratorId = curatorId,
                    ChangeConfidence = confidence,
                    CreatedAt = now
                };

                historyEntries.Add(historyEntry);
            }
        }

        if (historyEntries.Count > 0)
        {
            _context.CaseFieldHistories.AddRange(historyEntries);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return historyEntries;
    }

    /// <inheritdoc/>
    public async Task<List<CaseFieldHistory>> GetHistoryForCaseAsync(int caseId, CancellationToken cancellationToken = default)
    {
        return await _context.CaseFieldHistories
            .Where(h => h.CaseId == caseId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<CaseFieldHistory>> GetFieldHistoryAsync(int caseId, string fieldName, CancellationToken cancellationToken = default)
    {
        return await _context.CaseFieldHistories
            .Where(h => h.CaseId == caseId && h.FieldName == fieldName)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    private object? GetFieldValue(Case? caseEntity, string fieldName)
    {
        if (caseEntity == null) return null;

        var property = typeof(Case).GetProperty(fieldName);
        return property?.GetValue(caseEntity);
    }

    private bool ValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;
        
        // Handle value types and strings
        if (value1 is string str1 && value2 is string str2)
            return str1 == str2;
        
        return value1.Equals(value2);
    }

    private string? SerializeValue(object? value)
    {
        if (value == null) return null;
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private int GetConfidenceForField(Case caseEntity, string fieldName)
    {
        // Return the confidence score associated with the field category
        return fieldName switch
        {
            // Victim confidence
            nameof(Case.VictimConfidence) => caseEntity.VictimConfidence,
            
            // Accused confidence
            nameof(Case.AccusedConfidence) => caseEntity.AccusedConfidence,
            
            // Crime confidence
            nameof(Case.CrimeConfidence) => caseEntity.CrimeConfidence,
            
            // Judicial confidence
            nameof(Case.JudicialConfidence) => caseEntity.JudicialConfidence,
            
            // For other fields, use the most relevant confidence or default to a reasonable value
            // Crime type changes affect crime confidence area
            nameof(Case.CrimeTypeId) => caseEntity.CrimeConfidence,
            
            // Judicial status changes affect judicial confidence
            nameof(Case.JudicialStatusId) => caseEntity.JudicialConfidence,
            
            // Case type changes affect crime confidence
            nameof(Case.CaseTypeId) => caseEntity.CrimeConfidence,
            
            // Default: use highest relevant confidence or 50 as neutral
            _ when fieldName.StartsWith("Victim") => caseEntity.VictimConfidence,
            _ when fieldName.StartsWith("Accused") => caseEntity.AccusedConfidence,
            _ when fieldName.StartsWith("Crime") => caseEntity.CrimeConfidence,
            _ when fieldName.StartsWith("Judicial") => caseEntity.JudicialConfidence,
            _ => 50 // Default neutral confidence for unspecified fields
        };
    }
}
