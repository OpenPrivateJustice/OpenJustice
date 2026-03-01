using AtrocidadesRSS.Generator.Domain.Enums;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using AtrocidadesRSS.Generator.Services.Cases;
using Microsoft.EntityFrameworkCore;

namespace AtrocidadesRSS.Generator.Services.Discovery;

/// <summary>
/// Service for reviewing and processing discovered cases.
/// </summary>
public interface IDiscoveredCaseReviewService
{
    /// <summary>
    /// Gets all discovered cases with optional filtering.
    /// </summary>
    Task<List<DiscoveredCase>> GetAllCasesAsync(DiscoveryStatus? status = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a discovered case by ID.
    /// </summary>
    Task<DiscoveredCase?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Approves a discovered case (marks as approved, ready for promotion).
    /// </summary>
    Task<DiscoveredCase> ApproveAsync(int id, string curatorId, string? notes = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rejects a discovered case.
    /// </summary>
    Task<DiscoveredCase> RejectAsync(int id, string curatorId, string reason, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Promotes an approved discovered case to a draft case in the curation pipeline.
    /// </summary>
    Task<(DiscoveredCase DiscoveredCase, Case PromotedCase)> PromoteToCaseAsync(int id, Contracts.Discovery.PromoteToCaseRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Triggers discovery from all configured sources.
    /// </summary>
    Task<int> RunDiscoveryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for reviewing and processing discovered cases.
/// </summary>
public class DiscoveredCaseReviewService : IDiscoveredCaseReviewService
{
    private readonly AppDbContext _dbContext;
    private readonly IRssAggregatorService _rssAggregatorService;
    private readonly IRedditThreadScraperService _redditScraperService;
    private readonly ICaseReferenceCodeGenerator _referenceCodeGenerator;
    private readonly ILogger<DiscoveredCaseReviewService> _logger;

    public DiscoveredCaseReviewService(
        AppDbContext dbContext,
        IRssAggregatorService rssAggregatorService,
        IRedditThreadScraperService redditScraperService,
        ICaseReferenceCodeGenerator referenceCodeGenerator,
        ILogger<DiscoveredCaseReviewService> logger)
    {
        _dbContext = dbContext;
        _rssAggregatorService = rssAggregatorService;
        _redditScraperService = redditScraperService;
        _referenceCodeGenerator = referenceCodeGenerator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<DiscoveredCase>> GetAllCasesAsync(DiscoveryStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DiscoveredCases.AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }
        
        return await query
            .OrderByDescending(d => d.DiscoveredAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DiscoveredCase?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DiscoveredCases.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DiscoveredCase> ApproveAsync(int id, string curatorId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var discoveredCase = await _dbContext.DiscoveredCases.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Discovered case {id} not found");
        
        // Idempotent: if already approved, return the same record
        if (discoveredCase.Status == DiscoveryStatus.Approved)
        {
            _logger.LogInformation("Discovered case {Id} already approved, returning same record", id);
            return discoveredCase;
        }
        
        // Can only approve pending cases
        if (discoveredCase.Status != DiscoveryStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot approve discovered case {id} with status {discoveredCase.Status}. Only pending cases can be approved.");
        }
        
        discoveredCase.Status = DiscoveryStatus.Approved;
        discoveredCase.ReviewedBy = curatorId;
        discoveredCase.ReviewedAt = DateTime.UtcNow;
        discoveredCase.ReviewNotes = notes;
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Discovered case {Id} approved by curator {CuratorId}", id, curatorId);
        
        return discoveredCase;
    }

    /// <inheritdoc/>
    public async Task<DiscoveredCase> RejectAsync(int id, string curatorId, string reason, CancellationToken cancellationToken = default)
    {
        var discoveredCase = await _dbContext.DiscoveredCases.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Discovered case {id} not found");
        
        // Idempotent: if already rejected, return the same record
        if (discoveredCase.Status == DiscoveryStatus.Rejected)
        {
            _logger.LogInformation("Discovered case {Id} already rejected, returning same record", id);
            return discoveredCase;
        }
        
        // Can reject from any non-rejected status
        if (discoveredCase.Status == DiscoveryStatus.Rejected)
        {
            throw new InvalidOperationException($"Discovered case {id} is already rejected.");
        }
        
        discoveredCase.Status = DiscoveryStatus.Rejected;
        discoveredCase.ReviewedBy = curatorId;
        discoveredCase.ReviewedAt = DateTime.UtcNow;
        discoveredCase.ReviewNotes = reason;
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Discovered case {Id} rejected by curator {CuratorId}", id, curatorId);
        
        return discoveredCase;
    }

    /// <inheritdoc/>
    public async Task<(DiscoveredCase DiscoveredCase, Case PromotedCase)> PromoteToCaseAsync(
        int id, 
        Contracts.Discovery.PromoteToCaseRequest request, 
        CancellationToken cancellationToken = default)
    {
        var discoveredCase = await _dbContext.DiscoveredCases.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Discovered case {id} not found");
        
        // Can only promote approved cases
        if (discoveredCase.Status != DiscoveryStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot promote discovered case {id} with status {discoveredCase.Status}. Only approved cases can be promoted.");
        }
        
        // Check if already promoted
        if (discoveredCase.PromotedCaseId.HasValue)
        {
            throw new InvalidOperationException($"Discovered case {id} has already been promoted to case {discoveredCase.PromotedCaseId}.");
        }
        
        // Generate reference code
        var referenceCode = await _referenceCodeGenerator.GenerateAsync(cancellationToken);
        
        // Create the promoted case as a draft (pending curation)
        var promotedCase = new Case
        {
            ReferenceCode = referenceCode,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CrimeTypeId = request.CrimeTypeId,
            CaseTypeId = request.CaseTypeId,
            JudicialStatusId = request.JudicialStatusId,
            VictimName = request.VictimName,
            VictimConfidence = request.VictimConfidence,
            AccusedName = request.AccusedName,
            AccusedConfidence = request.AccusedConfidence,
            CrimeDescription = request.CrimeDescription ?? discoveredCase.Summary,
            CrimeConfidence = request.CrimeConfidence,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            JudicialConfidence = request.JudicialConfidence,
            CurationStatus = CurationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            // Copy source information
            Sources = new List<Source>
            {
                new Source
                {
                    SourceName = discoveredCase.SourceName,
                    OriginalLink = discoveredCase.SourceUrl,
                    Confidence = 70
                }
            }
        };
        
        _dbContext.Cases.Add(promotedCase);
        
        // Update discovered case
        discoveredCase.PromotedCaseId = promotedCase.Id;
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Discovered case {Id} promoted to case {CaseId} (Reference: {ReferenceCode})",
            id, promotedCase.Id, referenceCode);
        
        return (discoveredCase, promotedCase);
    }

    /// <inheritdoc/>
    public async Task<int> RunDiscoveryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting discovery run from all sources");
        
        var totalProcessed = 0;
        
        try
        {
            var rssCount = await _rssAggregatorService.FetchAndProcessAllFeedsAsync(cancellationToken);
            totalProcessed += rssCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RSS discovery");
        }
        
        try
        {
            var redditCount = await _redditScraperService.FetchAndProcessAllSubredditsAsync(cancellationToken);
            totalProcessed += redditCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Reddit discovery");
        }
        
        _logger.LogInformation("Discovery run completed. Total items processed: {Count}", totalProcessed);
        
        return totalProcessed;
    }
}
