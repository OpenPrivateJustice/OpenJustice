using AtrocidadesRSS.Generator.Domain.Enums;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using AtrocidadesRSS.Generator.Services.History;
using Microsoft.EntityFrameworkCore;

namespace AtrocidadesRSS.Generator.Services.Curation;

/// <summary>
/// Interface for case curation workflow service.
/// </summary>
public interface ICurationService
{
    /// <summary>
    /// Approves a case for publication.
    /// </summary>
    /// <param name="caseId">The case ID to approve.</param>
    /// <param name="curatorId">The curator approving the case.</param>
    /// <param name="notes">Optional notes for the approval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated case.</returns>
    /// <exception cref="ArgumentException">Thrown when case not found or invalid transition.</exception>
    Task<Case> ApproveAsync(int caseId, string curatorId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a case from publication.
    /// </summary>
    /// <param name="caseId">The case ID to reject.</param>
    /// <param name="curatorId">The curator rejecting the case.</param>
    /// <param name="notes">Reason for rejection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated case.</returns>
    /// <exception cref="ArgumentException">Thrown when case not found or invalid transition.</exception>
    Task<Case> RejectAsync(int caseId, string curatorId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a case as verified by a curator.
    /// </summary>
    /// <param name="caseId">The case ID to verify.</param>
    /// <param name="curatorId">The curator verifying the case.</param>
    /// <param name="notes">Optional notes for verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated case.</returns>
    /// <exception cref="ArgumentException">Thrown when case not found or invalid transition.</exception>
    Task<Case> VerifyAsync(int caseId, string curatorId, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a case by ID with its current curation status.
    /// </summary>
    Task<Case?> GetCaseByIdAsync(int caseId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of case curation workflow service.
/// Enforces state transition rules and integrates with audit logging.
/// </summary>
public class CurationService : ICurationService
{
    private readonly AppDbContext _context;
    private readonly ICaseAuditLogService _auditLogService;

    public CurationService(AppDbContext context, ICaseAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc/>
    public async Task<Case> ApproveAsync(int caseId, string curatorId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var caseEntity = await GetCaseForUpdateAsync(caseId, cancellationToken);
        
        var previousStatus = caseEntity.CurationStatus;
        
        // Validate transition: can only approve pending cases
        if (caseEntity.CurationStatus != CurationStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot approve case in '{caseEntity.CurationStatus}' status. Only pending cases can be approved.");
        }

        // Apply transition
        caseEntity.CurationStatus = CurationStatus.Approved;
        caseEntity.CurationTimestamp = DateTime.UtcNow;
        caseEntity.CuratorId = curatorId;
        
        // Write audit log atomically
        await _auditLogService.AddAuditLogAsync(
            caseId,
            AuditActionTypes.Approved,
            previousStatus,
            CurationStatus.Approved,
            curatorId,
            notes,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        
        return caseEntity;
    }

    /// <inheritdoc/>
    public async Task<Case> RejectAsync(int caseId, string curatorId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var caseEntity = await GetCaseForUpdateAsync(caseId, cancellationToken);
        
        var previousStatus = caseEntity.CurationStatus;
        
        // Validate transition: can only reject pending or approved cases
        if (caseEntity.CurationStatus == CurationStatus.Rejected)
        {
            throw new InvalidOperationException(
                $"Case is already in '{CurationStatus.Rejected}' status.");
        }

        // Apply transition
        caseEntity.CurationStatus = CurationStatus.Rejected;
        caseEntity.CurationTimestamp = DateTime.UtcNow;
        caseEntity.CuratorId = curatorId;
        
        // Write audit log atomically
        await _auditLogService.AddAuditLogAsync(
            caseId,
            AuditActionTypes.Rejected,
            previousStatus,
            CurationStatus.Rejected,
            curatorId,
            notes,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        
        return caseEntity;
    }

    /// <inheritdoc/>
    public async Task<Case> VerifyAsync(int caseId, string curatorId, string? notes = null, CancellationToken cancellationToken = default)
    {
        var caseEntity = await GetCaseForUpdateAsync(caseId, cancellationToken);
        
        // Can only verify approved cases
        if (caseEntity.CurationStatus != CurationStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot verify case in '{caseEntity.CurationStatus}' status. Only approved cases can be verified.");
        }

        // Apply verification (set IsVerified flag)
        caseEntity.IsVerified = true;
        caseEntity.CuratorId = curatorId; // Update curator to the verifier
        
        // Write audit log atomically
        await _auditLogService.AddAuditLogAsync(
            caseId,
            AuditActionTypes.Verified,
            null, // No status change for verification
            null,
            curatorId,
            notes,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        
        return caseEntity;
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

    private async Task<Case> GetCaseForUpdateAsync(int caseId, CancellationToken cancellationToken)
    {
        var caseEntity = await _context.Cases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
        
        if (caseEntity == null)
        {
            throw new ArgumentException($"Case with ID {caseId} not found.");
        }
        
        return caseEntity;
    }
}
