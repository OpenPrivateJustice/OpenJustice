using OpenJustice.Reader.Models.Cases;
using OpenJustice.Reader.Services.Data;
using Microsoft.Extensions.Logging;

namespace OpenJustice.Reader.Services.Cases;

/// <summary>
/// Service for loading and mapping complete case details.
/// </summary>
public class CaseDetailsService : ICaseDetailsService
{
    private readonly ILocalCaseStore _caseStore;
    private readonly ILogger<CaseDetailsService> _logger;

    public CaseDetailsService(
        ILocalCaseStore caseStore,
        ILogger<CaseDetailsService> logger)
    {
        _caseStore = caseStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CaseDetailViewModel?> GetCaseDetailsAsync(int caseId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading case details for ID: {CaseId}", caseId);

        var localCase = await _caseStore.GetCaseByIdAsync(caseId, cancellationToken);
        
        if (localCase == null)
        {
            _logger.LogWarning("Case not found: {CaseId}", caseId);
            return null;
        }

        return MapToCaseDetailViewModel(localCase);
    }

    /// <inheritdoc/>
    public async Task<CaseDetailViewModel?> GetCaseDetailsByReferenceAsync(string referenceCode, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading case details for reference code: {ReferenceCode}", referenceCode);

        var localCase = await _caseStore.GetCaseByReferenceCodeAsync(referenceCode, cancellationToken);
        
        if (localCase == null)
        {
            _logger.LogWarning("Case not found with reference code: {ReferenceCode}", referenceCode);
            return null;
        }

        return MapToCaseDetailViewModel(localCase);
    }

    private CaseDetailViewModel MapToCaseDetailViewModel(LocalCase localCase)
    {
        // Map core case fields
        var viewModel = new CaseDetailViewModel
        {
            Id = localCase.Id,
            ReferenceCode = localCase.ReferenceCode,
            CrimeDate = localCase.CrimeDate,
            CrimeType = localCase.CrimeType,
            CaseType = localCase.CaseType,
            VictimName = localCase.VictimName,
            AccusedName = localCase.AccusedName,
            LocationCity = localCase.LocationCity,
            LocationState = localCase.LocationState,
            JudicialStatus = localCase.JudicialStatus,
            Description = localCase.Description,
            ConfidenceScore = localCase.ConfidenceScore,
            IsVerified = localCase.IsVerified,
            IsSensitiveContent = localCase.IsSensitiveContent,
            CreatedAt = localCase.CreatedAt,
            UpdatedAt = localCase.UpdatedAt
        };

        // In a full implementation, these would be loaded from separate tables in the local store.
        // For now, we initialize empty collections - they would be populated from:
        // - Sources: from local store's case_sources table
        // - Evidence: from local store's case_evidence table
        // - JudicialInfo: from local store's case_judicial_info table
        // - Tags: from local store's case_tags join table
        // - Metadata: from local store's case_metadata table

        viewModel.Sources = Array.Empty<CaseSourceViewModel>();
        viewModel.Evidence = Array.Empty<CaseEvidenceViewModel>();
        viewModel.JudicialInfo = null;
        viewModel.Tags = Array.Empty<string>();
        
        // Create metadata from available fields
        viewModel.Metadata = new CaseMetadataViewModel
        {
            RegisteredAt = localCase.CreatedAt,
            RegisteredBy = null, // Would come from case creator
            LastVerifiedAt = localCase.IsVerified ? localCase.UpdatedAt : null,
            LastVerifiedBy = null, // Would come from verifier
            Version = 1, // Would come from case version tracking
            DataQualityNotes = null,
            RelatedCaseCodes = Array.Empty<string>()
        };

        _logger.LogDebug("Mapped case details for {ReferenceCode}, IsSensitiveContent: {IsSensitive}", 
            viewModel.ReferenceCode, viewModel.IsSensitiveContent);

        return viewModel;
    }
}
