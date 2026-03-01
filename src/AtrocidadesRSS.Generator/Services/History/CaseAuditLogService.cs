using AtrocidadesRSS.Generator.Domain.Enums;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtrocidadesRSS.Generator.Services.History;

/// <summary>
/// Interface for immutable case audit log service.
/// </summary>
public interface ICaseAuditLogService
{
    /// <summary>
    /// Adds an immutable audit log entry for a case action.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="actionType">The type of action performed.</param>
    /// <param name="previousStatus">The previous curation status (optional).</param>
    /// <param name="newStatus">The new curation status (optional).</param>
    /// <param name="curatorId">The curator who performed the action.</param>
    /// <param name="notes">Optional notes or reason for the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created audit log entry.</returns>
    Task<CaseAuditLog> AddAuditLogAsync(
        int caseId,
        string actionType,
        CurationStatus? previousStatus,
        CurationStatus? newStatus,
        string? curatorId,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit log entries for a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit log entries ordered by timestamp.</returns>
    Task<List<CaseAuditLog>> GetAuditLogsForCaseAsync(int caseId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of append-only case audit log service.
/// This service only appends entries - updates and deletes are not supported.
/// </summary>
public class CaseAuditLogService : ICaseAuditLogService
{
    private readonly AppDbContext _context;

    public CaseAuditLogService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<CaseAuditLog> AddAuditLogAsync(
        int caseId,
        string actionType,
        CurationStatus? previousStatus,
        CurationStatus? newStatus,
        string? curatorId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new CaseAuditLog
        {
            CaseId = caseId,
            ActionType = actionType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            CuratorId = curatorId,
            Notes = notes,
            Timestamp = DateTime.UtcNow
        };

        _context.CaseAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        return auditLog;
    }

    /// <inheritdoc/>
    public async Task<List<CaseAuditLog>> GetAuditLogsForCaseAsync(int caseId, CancellationToken cancellationToken = default)
    {
        return await _context.CaseAuditLogs
            .Where(log => log.CaseId == caseId)
            .OrderBy(log => log.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
