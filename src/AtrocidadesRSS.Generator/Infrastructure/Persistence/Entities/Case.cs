using AtrocidadesRSS.Generator.Domain.Enums;

namespace AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;

public class Case
{
    // Identification
    public int Id { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public DateTime? CrimeDate { get; set; }
    public DateTime? ReportDate { get; set; }
    public DateTime LastUpdated { get; set; }

    // Victim Information
    public string? VictimName { get; set; }
    public string? VictimGender { get; set; }
    public int? VictimAge { get; set; }
    public string? VictimNationality { get; set; }
    public string? VictimProfession { get; set; }
    public string? VictimRelationshipToAccused { get; set; }
    public int VictimConfidence { get; set; }

    // Accused Information
    public string? AccusedName { get; set; }
    public string? AccusedSocialName { get; set; }
    public string? AccusedGender { get; set; }
    public int? AccusedAge { get; set; }
    public string? AccusedNationality { get; set; }
    public string? AccusedProfession { get; set; }
    public string? AccusedDocument { get; set; }
    public string? AccusedAddress { get; set; }
    public string? AccusedRelationshipToVictim { get; set; }
    public int AccusedConfidence { get; set; }

    // Crime Details
    public int CrimeTypeId { get; set; }
    public string? CrimeSubtype { get; set; }
    public DateTime? EstimatedCrimeDateTime { get; set; }
    public string? CrimeLocationAddress { get; set; }
    public string? CrimeLocationCity { get; set; }
    public string? CrimeLocationState { get; set; }
    public string? CrimeCoordinates { get; set; }
    public string? CrimeDescription { get; set; }
    public int CaseTypeId { get; set; }
    public int NumberOfVictims { get; set; } = 1;
    public int NumberOfAccused { get; set; } = 1;
    public string? WeaponUsed { get; set; }
    public string? Motivation { get; set; }
    public string? Premeditation { get; set; }
    public int CrimeConfidence { get; set; }

    // Judicial Information
    public int JudicialStatusId { get; set; }
    public string? ProcessNumber { get; set; }
    public string? Court { get; set; }
    public string? County { get; set; }
    public string? CurrentPhase { get; set; }
    public DateTime? JudicialReportDate { get; set; }
    public DateTime? SentencingDate { get; set; }
    public string? Sentence { get; set; }
    public string? PendingAppeals { get; set; }
    public int JudicialConfidence { get; set; }

    // Classification
    public string? MainCategory { get; set; }
    public bool IsSensitiveContent { get; set; }
    public bool IsVerified { get; set; }
    public string? AnonymizationStatus { get; set; }
    
    // Curation
    public CurationStatus CurationStatus { get; set; } = CurationStatus.Pending;
    public DateTime? CurationTimestamp { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CuratorId { get; set; }

    // Navigation Properties
    public virtual CrimeType? CrimeType { get; set; }
    public virtual CaseType? CaseType { get; set; }
    public virtual JudicialStatus? JudicialStatus { get; set; }
    public virtual ICollection<Source> Sources { get; set; } = new List<Source>();
    public virtual ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
    public virtual ICollection<CaseTag> CaseTags { get; set; } = new List<CaseTag>();
    public virtual ICollection<CaseFieldHistory> FieldHistories { get; set; } = new List<CaseFieldHistory>();
}

