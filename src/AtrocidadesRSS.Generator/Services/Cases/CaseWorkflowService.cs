using Microsoft.EntityFrameworkCore;
using AtrocidadesRSS.Generator.Contracts.Cases;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;

namespace AtrocidadesRSS.Generator.Services.Cases;

/// <summary>
/// Service for managing case workflow operations (create, update).
/// </summary>
public interface ICaseWorkflowService
{
    /// <summary>
    /// Creates a new case with a generated reference code.
    /// </summary>
    /// <param name="request">The create case request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created case entity.</returns>
    /// <exception cref="ArgumentException">Thrown when validation fails or foreign key is invalid.</exception>
    Task<Case> CreateCaseAsync(CreateCaseRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing case.
    /// </summary>
    /// <param name="caseId">The case ID to update.</param>
    /// <param name="request">The update case request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated case entity.</returns>
    /// <exception cref="ArgumentException">Thrown when case not found or validation fails.</exception>
    Task<Case> UpdateCaseAsync(int caseId, UpdateCaseRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a case by ID.
    /// </summary>
    Task<Case?> GetCaseByIdAsync(int caseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a foreign key ID exists in the database.
    /// </summary>
    Task<bool> ValidateForeignKeyAsync(int crimeTypeId, int caseTypeId, int judicialStatusId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of case workflow service.
/// </summary>
public class CaseWorkflowService : ICaseWorkflowService
{
    private readonly AppDbContext _context;
    private readonly ICaseReferenceCodeGenerator _referenceCodeGenerator;

    public CaseWorkflowService(AppDbContext context, ICaseReferenceCodeGenerator referenceCodeGenerator)
    {
        _context = context;
        _referenceCodeGenerator = referenceCodeGenerator;
    }

    /// <inheritdoc/>
    public async Task<Case> CreateCaseAsync(CreateCaseRequest request, CancellationToken cancellationToken = default)
    {
        // Validate foreign keys exist
        var fkValid = await ValidateForeignKeyAsync(
            request.CrimeTypeId, 
            request.CaseTypeId, 
            request.JudicialStatusId, 
            cancellationToken);
        
        if (!fkValid)
        {
            throw new ArgumentException(
                "One or more foreign key references are invalid. " +
                "Please ensure CrimeTypeId, CaseTypeId, and JudicialStatusId are valid.");
        }

        // Generate reference code
        var referenceCode = await _referenceCodeGenerator.GenerateAsync(cancellationToken);
        
        var now = DateTime.UtcNow;
        
        var caseEntity = new Case
        {
            // Identification
            ReferenceCode = referenceCode,
            RegistrationDate = now,
            CrimeDate = request.CrimeDate,
            ReportDate = request.ReportDate,
            LastUpdated = now,
            
            // Victim Information
            VictimName = request.VictimName,
            VictimGender = request.VictimGender,
            VictimAge = request.VictimAge,
            VictimNationality = request.VictimNationality,
            VictimProfession = request.VictimProfession,
            VictimRelationshipToAccused = request.VictimRelationshipToAccused,
            VictimConfidence = request.VictimConfidence,
            
            // Accused Information
            AccusedName = request.AccusedName,
            AccusedSocialName = request.AccusedSocialName,
            AccusedGender = request.AccusedGender,
            AccusedAge = request.AccusedAge,
            AccusedNationality = request.AccusedNationality,
            AccusedProfession = request.AccusedProfession,
            AccusedDocument = request.AccusedDocument,
            AccusedAddress = request.AccusedAddress,
            AccusedRelationshipToVictim = request.AccusedRelationshipToVictim,
            AccusedConfidence = request.AccusedConfidence,
            
            // Crime Details
            CrimeTypeId = request.CrimeTypeId,
            CrimeSubtype = request.CrimeSubtype,
            EstimatedCrimeDateTime = request.EstimatedCrimeDateTime,
            CrimeLocationAddress = request.CrimeLocationAddress,
            CrimeLocationCity = request.CrimeLocationCity,
            CrimeLocationState = request.CrimeLocationState,
            CrimeCoordinates = request.CrimeCoordinates,
            CrimeDescription = request.CrimeDescription,
            CaseTypeId = request.CaseTypeId,
            NumberOfVictims = request.NumberOfVictims,
            NumberOfAccused = request.NumberOfAccused,
            WeaponUsed = request.WeaponUsed,
            Motivation = request.Motivation,
            Premeditation = request.Premeditation,
            CrimeConfidence = request.CrimeConfidence,
            
            // Judicial Information
            JudicialStatusId = request.JudicialStatusId,
            ProcessNumber = request.ProcessNumber,
            Court = request.Court,
            County = request.County,
            CurrentPhase = request.CurrentPhase,
            JudicialReportDate = request.JudicialReportDate,
            SentencingDate = request.SentencingDate,
            Sentence = request.Sentence,
            PendingAppeals = request.PendingAppeals,
            JudicialConfidence = request.JudicialConfidence,
            
            // Classification
            MainCategory = request.MainCategory,
            IsSensitiveContent = request.IsSensitiveContent,
            IsVerified = request.IsVerified,
            AnonymizationStatus = request.AnonymizationStatus,
            
            // Metadata
            CreatedAt = now,
            UpdatedAt = now,
            CuratorId = request.CuratorId
        };

        _context.Cases.Add(caseEntity);
        await _context.SaveChangesAsync(cancellationToken);
        
        return caseEntity;
    }

    /// <inheritdoc/>
    public async Task<Case> UpdateCaseAsync(int caseId, UpdateCaseRequest request, CancellationToken cancellationToken = default)
    {
        var existingCase = await _context.Cases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
        
        if (existingCase == null)
        {
            throw new ArgumentException($"Case with ID {caseId} not found.");
        }

        // Validate foreign keys exist
        var fkValid = await ValidateForeignKeyAsync(
            request.CrimeTypeId, 
            request.CaseTypeId, 
            request.JudicialStatusId, 
            cancellationToken);
        
        if (!fkValid)
        {
            throw new ArgumentException(
                "One or more foreign key references are invalid. " +
                "Please ensure CrimeTypeId, CaseTypeId, and JudicialStatusId are valid.");
        }

        var now = DateTime.UtcNow;

        // Update fields (reference code is NOT changed on edit - it's stable)
        existingCase.CrimeDate = request.CrimeDate;
        existingCase.ReportDate = request.ReportDate;
        
        // Victim Information
        existingCase.VictimName = request.VictimName;
        existingCase.VictimGender = request.VictimGender;
        existingCase.VictimAge = request.VictimAge;
        existingCase.VictimNationality = request.VictimNationality;
        existingCase.VictimProfession = request.VictimProfession;
        existingCase.VictimRelationshipToAccused = request.VictimRelationshipToAccused;
        existingCase.VictimConfidence = request.VictimConfidence;
        
        // Accused Information
        existingCase.AccusedName = request.AccusedName;
        existingCase.AccusedSocialName = request.AccusedSocialName;
        existingCase.AccusedGender = request.AccusedGender;
        existingCase.AccusedAge = request.AccusedAge;
        existingCase.AccusedNationality = request.AccusedNationality;
        existingCase.AccusedProfession = request.AccusedProfession;
        existingCase.AccusedDocument = request.AccusedDocument;
        existingCase.AccusedAddress = request.AccusedAddress;
        existingCase.AccusedRelationshipToVictim = request.AccusedRelationshipToVictim;
        existingCase.AccusedConfidence = request.AccusedConfidence;
        
        // Crime Details
        existingCase.CrimeTypeId = request.CrimeTypeId;
        existingCase.CrimeSubtype = request.CrimeSubtype;
        existingCase.EstimatedCrimeDateTime = request.EstimatedCrimeDateTime;
        existingCase.CrimeLocationAddress = request.CrimeLocationAddress;
        existingCase.CrimeLocationCity = request.CrimeLocationCity;
        existingCase.CrimeLocationState = request.CrimeLocationState;
        existingCase.CrimeCoordinates = request.CrimeCoordinates;
        existingCase.CrimeDescription = request.CrimeDescription;
        existingCase.CaseTypeId = request.CaseTypeId;
        existingCase.NumberOfVictims = request.NumberOfVictims;
        existingCase.NumberOfAccused = request.NumberOfAccused;
        existingCase.WeaponUsed = request.WeaponUsed;
        existingCase.Motivation = request.Motivation;
        existingCase.Premeditation = request.Premeditation;
        existingCase.CrimeConfidence = request.CrimeConfidence;
        
        // Judicial Information
        existingCase.JudicialStatusId = request.JudicialStatusId;
        existingCase.ProcessNumber = request.ProcessNumber;
        existingCase.Court = request.Court;
        existingCase.County = request.County;
        existingCase.CurrentPhase = request.CurrentPhase;
        existingCase.JudicialReportDate = request.JudicialReportDate;
        existingCase.SentencingDate = request.SentencingDate;
        existingCase.Sentence = request.Sentence;
        existingCase.PendingAppeals = request.PendingAppeals;
        existingCase.JudicialConfidence = request.JudicialConfidence;
        
        // Classification
        existingCase.MainCategory = request.MainCategory;
        existingCase.IsSensitiveContent = request.IsSensitiveContent;
        existingCase.IsVerified = request.IsVerified;
        existingCase.AnonymizationStatus = request.AnonymizationStatus;
        
        // Metadata
        existingCase.UpdatedAt = now;
        existingCase.CuratorId = request.CuratorId;
        existingCase.LastUpdated = now;

        await _context.SaveChangesAsync(cancellationToken);
        
        return existingCase;
    }

    /// <inheritdoc/>
    public async Task<Case?> GetCaseByIdAsync(int caseId, CancellationToken cancellationToken = default)
    {
        return await _context.Cases
            .Include(c => c.CrimeType)
            .Include(c => c.CaseType)
            .Include(c => c.JudicialStatus)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateForeignKeyAsync(
        int crimeTypeId, 
        int caseTypeId, 
        int judicialStatusId, 
        CancellationToken cancellationToken = default)
    {
        // Check CrimeType exists
        var crimeTypeExists = await _context.CrimeTypes
            .AnyAsync(ct => ct.Id == crimeTypeId, cancellationToken);
        
        if (!crimeTypeExists)
            return false;
        
        // Check CaseType exists
        var caseTypeExists = await _context.CaseTypes
            .AnyAsync(ct => ct.Id == caseTypeId, cancellationToken);
        
        if (!caseTypeExists)
            return false;
        
        // Check JudicialStatus exists
        var judicialStatusExists = await _context.JudicialStatuses
            .AnyAsync(js => js.Id == judicialStatusId, cancellationToken);
        
        return judicialStatusExists;
    }
}
