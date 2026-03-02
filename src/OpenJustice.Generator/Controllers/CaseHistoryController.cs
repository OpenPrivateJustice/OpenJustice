using OpenJustice.Generator.Contracts.Cases;
using OpenJustice.Generator.Infrastructure.Persistence;
using OpenJustice.Generator.Infrastructure.Persistence.Entities;
using OpenJustice.Generator.Services.History;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OpenJustice.Generator.Controllers;

/// <summary>
/// API controller for querying case field history.
/// </summary>
[ApiController]
[Route("api/cases/{caseId}/history")]
public class CaseHistoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICaseFieldHistoryService _historyService;

    public CaseHistoryController(AppDbContext context, ICaseFieldHistoryService historyService)
    {
        _context = context;
        _historyService = historyService;
    }

    /// <summary>
    /// Gets all field history for a case, ordered by ChangedAt descending (most recent first).
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of field history entries.</returns>
    /// <response code="404">Case not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CaseFieldHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CaseFieldHistoryDto>>> GetCaseHistory(
        int caseId, 
        CancellationToken cancellationToken = default)
    {
        // Validate case exists
        var caseExists = await _context.Cases.AnyAsync(c => c.Id == caseId, cancellationToken);
        if (!caseExists)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Case Not Found",
                Detail = $"Case with ID {caseId} not found.",
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }

        var history = await _historyService.GetHistoryForCaseAsync(caseId, cancellationToken);
        var dtos = history.Select(MapToDto).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Gets history for a specific field on a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="fieldName">The field name to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of field history entries for the specified field.</returns>
    /// <response code="404">Case not found.</response>
    [HttpGet("{fieldName}")]
    [ProducesResponseType(typeof(List<CaseFieldHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CaseFieldHistoryDto>>> GetFieldHistory(
        int caseId, 
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        // Validate case exists
        var caseExists = await _context.Cases.AnyAsync(c => c.Id == caseId, cancellationToken);
        if (!caseExists)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Case Not Found",
                Detail = $"Case with ID {caseId} not found.",
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }

        var history = await _historyService.GetFieldHistoryAsync(caseId, fieldName, cancellationToken);
        var dtos = history.Select(MapToDto).ToList();

        return Ok(dtos);
    }

    private static CaseFieldHistoryDto MapToDto(CaseFieldHistory entity)
    {
        return new CaseFieldHistoryDto
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
}
