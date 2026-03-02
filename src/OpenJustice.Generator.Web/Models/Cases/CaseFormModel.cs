using System.ComponentModel.DataAnnotations;

namespace OpenJustice.Generator.Web.Models.Cases;

/// <summary>
/// Form model for creating and editing cases.
/// Used with Blazor EditForm for client-side validation.
/// </summary>
public class CaseFormModel
{
    // Crime Date Information
    [DataType(DataType.Date)]
    public DateTime? CrimeDate { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime? ReportDate { get; set; }
    
    // Victim Information
    [StringLength(500)]
    public string? VictimName { get; set; }
    
    [StringLength(20)]
    public string? VictimGender { get; set; }
    
    [Range(0, 150)]
    public int? VictimAge { get; set; }
    
    [StringLength(100)]
    public string? VictimNationality { get; set; }
    
    [StringLength(200)]
    public string? VictimProfession { get; set; }
    
    [StringLength(200)]
    public string? VictimRelationshipToAccused { get; set; }
    
    [Range(0, 100)]
    public int VictimConfidence { get; set; } = 50;
    
    // Accused Information
    [StringLength(500)]
    public string? AccusedName { get; set; }
    
    [StringLength(500)]
    public string? AccusedSocialName { get; set; }
    
    [StringLength(20)]
    public string? AccusedGender { get; set; }
    
    [Range(0, 150)]
    public int? AccusedAge { get; set; }
    
    [StringLength(100)]
    public string? AccusedNationality { get; set; }
    
    [StringLength(200)]
    public string? AccusedProfession { get; set; }
    
    [StringLength(50)]
    public string? AccusedDocument { get; set; }
    
    [StringLength(500)]
    public string? AccusedAddress { get; set; }
    
    [StringLength(200)]
    public string? AccusedRelationshipToVictim { get; set; }
    
    [Range(0, 100)]
    public int AccusedConfidence { get; set; } = 50;
    
    // Crime Details (Required)
    [Required(ErrorMessage = "Tipo de crime é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "Tipo de crime é obrigatório")]
    public int CrimeTypeId { get; set; }
    
    [StringLength(200)]
    public string? CrimeSubtype { get; set; }
    
    [DataType(DataType.DateTime)]
    public DateTime? EstimatedCrimeDateTime { get; set; }
    
    [StringLength(500)]
    public string? CrimeLocationAddress { get; set; }
    
    [StringLength(200)]
    public string? CrimeLocationCity { get; set; }
    
    [StringLength(100)]
    public string? CrimeLocationState { get; set; }
    
    [StringLength(100)]
    public string? CrimeCoordinates { get; set; }
    
    public string? CrimeDescription { get; set; }
    
    [Required(ErrorMessage = "Tipo de caso é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "Tipo de caso é obrigatório")]
    public int CaseTypeId { get; set; }
    
    [Range(1, 10000)]
    public int NumberOfVictims { get; set; } = 1;
    
    [Range(1, 10000)]
    public int NumberOfAccused { get; set; } = 1;
    
    [StringLength(200)]
    public string? WeaponUsed { get; set; }
    
    [StringLength(500)]
    public string? Motivation { get; set; }
    
    [StringLength(50)]
    public string? Premeditation { get; set; }
    
    [Range(0, 100)]
    public int CrimeConfidence { get; set; } = 50;
    
    // Judicial Information (Required)
    [Required(ErrorMessage = "Status judicial é obrigatório")]
    [Range(1, int.MaxValue, ErrorMessage = "Status judicial é obrigatório")]
    public int JudicialStatusId { get; set; }
    
    [StringLength(100)]
    public string? ProcessNumber { get; set; }
    
    [StringLength(300)]
    public string? Court { get; set; }
    
    [StringLength(200)]
    public string? County { get; set; }
    
    [StringLength(200)]
    public string? CurrentPhase { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime? JudicialReportDate { get; set; }
    
    [DataType(DataType.Date)]
    public DateTime? SentencingDate { get; set; }
    
    public string? Sentence { get; set; }
    
    [StringLength(500)]
    public string? PendingAppeals { get; set; }
    
    [Range(0, 100)]
    public int JudicialConfidence { get; set; } = 50;
    
    // Classification
    [StringLength(200)]
    public string? MainCategory { get; set; }
    
    public bool IsSensitiveContent { get; set; }
    
    public bool IsVerified { get; set; }
    
    [StringLength(50)]
    public string? AnonymizationStatus { get; set; }
    
    // Metadata
    [StringLength(100)]
    public string? CuratorId { get; set; }

    /// <summary>
    /// Creates a CreateCaseRequest from this form model.
    /// </summary>
    public OpenJustice.Generator.Contracts.Cases.CreateCaseRequest ToCreateRequest()
    {
        return new OpenJustice.Generator.Contracts.Cases.CreateCaseRequest
        {
            CrimeDate = CrimeDate,
            ReportDate = ReportDate,
            VictimName = VictimName,
            VictimGender = VictimGender,
            VictimAge = VictimAge,
            VictimNationality = VictimNationality,
            VictimProfession = VictimProfession,
            VictimRelationshipToAccused = VictimRelationshipToAccused,
            VictimConfidence = VictimConfidence,
            AccusedName = AccusedName,
            AccusedSocialName = AccusedSocialName,
            AccusedGender = AccusedGender,
            AccusedAge = AccusedAge,
            AccusedNationality = AccusedNationality,
            AccusedProfession = AccusedProfession,
            AccusedDocument = AccusedDocument,
            AccusedAddress = AccusedAddress,
            AccusedRelationshipToVictim = AccusedRelationshipToVictim,
            AccusedConfidence = AccusedConfidence,
            CrimeTypeId = CrimeTypeId,
            CrimeSubtype = CrimeSubtype,
            EstimatedCrimeDateTime = EstimatedCrimeDateTime,
            CrimeLocationAddress = CrimeLocationAddress,
            CrimeLocationCity = CrimeLocationCity,
            CrimeLocationState = CrimeLocationState,
            CrimeCoordinates = CrimeCoordinates,
            CrimeDescription = CrimeDescription,
            CaseTypeId = CaseTypeId,
            NumberOfVictims = NumberOfVictims,
            NumberOfAccused = NumberOfAccused,
            WeaponUsed = WeaponUsed,
            Motivation = Motivation,
            Premeditation = Premeditation,
            CrimeConfidence = CrimeConfidence,
            JudicialStatusId = JudicialStatusId,
            ProcessNumber = ProcessNumber,
            Court = Court,
            County = County,
            CurrentPhase = CurrentPhase,
            JudicialReportDate = JudicialReportDate,
            SentencingDate = SentencingDate,
            Sentence = Sentence,
            PendingAppeals = PendingAppeals,
            JudicialConfidence = JudicialConfidence,
            MainCategory = MainCategory,
            IsSensitiveContent = IsSensitiveContent,
            IsVerified = IsVerified,
            AnonymizationStatus = AnonymizationStatus,
            CuratorId = CuratorId
        };
    }

    /// <summary>
    /// Creates an UpdateCaseRequest from this form model.
    /// </summary>
    public OpenJustice.Generator.Contracts.Cases.UpdateCaseRequest ToUpdateRequest()
    {
        return new OpenJustice.Generator.Contracts.Cases.UpdateCaseRequest
        {
            CrimeDate = CrimeDate,
            ReportDate = ReportDate,
            VictimName = VictimName,
            VictimGender = VictimGender,
            VictimAge = VictimAge,
            VictimNationality = VictimNationality,
            VictimProfession = VictimProfession,
            VictimRelationshipToAccused = VictimRelationshipToAccused,
            VictimConfidence = VictimConfidence,
            AccusedName = AccusedName,
            AccusedSocialName = AccusedSocialName,
            AccusedGender = AccusedGender,
            AccusedAge = AccusedAge,
            AccusedNationality = AccusedNationality,
            AccusedProfession = AccusedProfession,
            AccusedDocument = AccusedDocument,
            AccusedAddress = AccusedAddress,
            AccusedRelationshipToVictim = AccusedRelationshipToVictim,
            AccusedConfidence = AccusedConfidence,
            CrimeTypeId = CrimeTypeId,
            CrimeSubtype = CrimeSubtype,
            EstimatedCrimeDateTime = EstimatedCrimeDateTime,
            CrimeLocationAddress = CrimeLocationAddress,
            CrimeLocationCity = CrimeLocationCity,
            CrimeLocationState = CrimeLocationState,
            CrimeCoordinates = CrimeCoordinates,
            CrimeDescription = CrimeDescription,
            CaseTypeId = CaseTypeId,
            NumberOfVictims = NumberOfVictims,
            NumberOfAccused = NumberOfAccused,
            WeaponUsed = WeaponUsed,
            Motivation = Motivation,
            Premeditation = Premeditation,
            CrimeConfidence = CrimeConfidence,
            JudicialStatusId = JudicialStatusId,
            ProcessNumber = ProcessNumber,
            Court = Court,
            County = County,
            CurrentPhase = CurrentPhase,
            JudicialReportDate = JudicialReportDate,
            SentencingDate = SentencingDate,
            Sentence = Sentence,
            PendingAppeals = PendingAppeals,
            JudicialConfidence = JudicialConfidence,
            MainCategory = MainCategory,
            IsSensitiveContent = IsSensitiveContent,
            IsVerified = IsVerified,
            AnonymizationStatus = AnonymizationStatus,
            CuratorId = CuratorId
        };
    }

    /// <summary>
    /// Populates this form model from an existing case entity (for editing).
    /// </summary>
    public static CaseFormModel FromCase(OpenJustice.Generator.Infrastructure.Persistence.Entities.Case caseEntity)
    {
        return new CaseFormModel
        {
            CrimeDate = caseEntity.CrimeDate,
            ReportDate = caseEntity.ReportDate,
            VictimName = caseEntity.VictimName,
            VictimGender = caseEntity.VictimGender,
            VictimAge = caseEntity.VictimAge,
            VictimNationality = caseEntity.VictimNationality,
            VictimProfession = caseEntity.VictimProfession,
            VictimRelationshipToAccused = caseEntity.VictimRelationshipToAccused,
            VictimConfidence = caseEntity.VictimConfidence,
            AccusedName = caseEntity.AccusedName,
            AccusedSocialName = caseEntity.AccusedSocialName,
            AccusedGender = caseEntity.AccusedGender,
            AccusedAge = caseEntity.AccusedAge,
            AccusedNationality = caseEntity.AccusedNationality,
            AccusedProfession = caseEntity.AccusedProfession,
            AccusedDocument = caseEntity.AccusedDocument,
            AccusedAddress = caseEntity.AccusedAddress,
            AccusedRelationshipToVictim = caseEntity.AccusedRelationshipToVictim,
            AccusedConfidence = caseEntity.AccusedConfidence,
            CrimeTypeId = caseEntity.CrimeTypeId,
            CrimeSubtype = caseEntity.CrimeSubtype,
            EstimatedCrimeDateTime = caseEntity.EstimatedCrimeDateTime,
            CrimeLocationAddress = caseEntity.CrimeLocationAddress,
            CrimeLocationCity = caseEntity.CrimeLocationCity,
            CrimeLocationState = caseEntity.CrimeLocationState,
            CrimeCoordinates = caseEntity.CrimeCoordinates,
            CrimeDescription = caseEntity.CrimeDescription,
            CaseTypeId = caseEntity.CaseTypeId,
            NumberOfVictims = caseEntity.NumberOfVictims,
            NumberOfAccused = caseEntity.NumberOfAccused,
            WeaponUsed = caseEntity.WeaponUsed,
            Motivation = caseEntity.Motivation,
            Premeditation = caseEntity.Premeditation,
            CrimeConfidence = caseEntity.CrimeConfidence,
            JudicialStatusId = caseEntity.JudicialStatusId,
            ProcessNumber = caseEntity.ProcessNumber,
            Court = caseEntity.Court,
            County = caseEntity.County,
            CurrentPhase = caseEntity.CurrentPhase,
            JudicialReportDate = caseEntity.JudicialReportDate,
            SentencingDate = caseEntity.SentencingDate,
            Sentence = caseEntity.Sentence,
            PendingAppeals = caseEntity.PendingAppeals,
            JudicialConfidence = caseEntity.JudicialConfidence,
            MainCategory = caseEntity.MainCategory,
            IsSensitiveContent = caseEntity.IsSensitiveContent,
            IsVerified = caseEntity.IsVerified,
            AnonymizationStatus = caseEntity.AnonymizationStatus,
            CuratorId = caseEntity.CuratorId
        };
    }
}
