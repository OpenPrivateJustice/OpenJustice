using OpenJustice.Generator.Contracts.Discovery;
using OpenJustice.Generator.Domain.Enums;
using OpenJustice.Generator.Infrastructure.Persistence.Entities;
using OpenJustice.Generator.Services.Discovery;
using Microsoft.AspNetCore.Mvc;

namespace OpenJustice.Generator.Controllers;

/// <summary>
/// Controller for discovery and curation of automatically discovered cases.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private readonly IDiscoveredCaseReviewService _reviewService;
    private readonly ILogger<DiscoveryController> _logger;

    public DiscoveryController(
        IDiscoveredCaseReviewService reviewService,
        ILogger<DiscoveryController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all discovered cases, optionally filtered by status.
    /// </summary>
    /// <param name="status">Optional status filter (Pending, Approved, Rejected).</param>
    [HttpGet("cases")]
    [ProducesResponseType(typeof(List<DiscoveredCaseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DiscoveredCaseDto>>> GetCases([FromQuery] DiscoveryStatus? status = null)
    {
        var cases = await _reviewService.GetAllCasesAsync(status);
        
        var dtos = cases.Select(MapToDto).ToList();
        
        return Ok(dtos);
    }

    /// <summary>
    /// Gets a specific discovered case by ID.
    /// </summary>
    /// <param name="id">The discovered case ID.</param>
    [HttpGet("cases/{id}")]
    [ProducesResponseType(typeof(DiscoveredCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscoveredCaseDto>> GetCase(int id)
    {
        var discoveredCase = await _reviewService.GetByIdAsync(id);
        
        if (discoveredCase == null)
        {
            return NotFound($"Discovered case {id} not found");
        }
        
        return Ok(MapToDto(discoveredCase));
    }

    /// <summary>
    /// Triggers discovery from all configured RSS and Reddit sources.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> RunDiscovery()
    {
        var count = await _reviewService.RunDiscoveryAsync();
        
        return Ok(new { ItemsDiscovered = count, Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Approves a discovered case for promotion to a draft case.
    /// </summary>
    /// <param name="id">The discovered case ID.</param>
    /// <param name="request">The approval request.</param>
    [HttpPost("cases/{id}/approve")]
    [ProducesResponseType(typeof(DiscoveredCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DiscoveredCaseDto>> ApproveCase(int id, [FromBody] ApproveDiscoveredCaseRequest request)
    {
        try
        {
            var discoveredCase = await _reviewService.ApproveAsync(id, request.CuratorId, request.Notes);
            
            return Ok(MapToDto(discoveredCase));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Rejects a discovered case.
    /// </summary>
    /// <param name="id">The discovered case ID.</param>
    /// <param name="request">The rejection request.</param>
    [HttpPost("cases/{id}/reject")]
    [ProducesResponseType(typeof(DiscoveredCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscoveredCaseDto>> RejectCase(int id, [FromBody] RejectDiscoveredCaseRequest request)
    {
        try
        {
            var discoveredCase = await _reviewService.RejectAsync(id, request.CuratorId, request.Reason);
            
            return Ok(MapToDto(discoveredCase));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Promotes an approved discovered case to a draft case in the curation pipeline.
    /// </summary>
    /// <param name="id">The discovered case ID.</param>
    /// <param name="request">The promotion request with case details.</param>
    [HttpPost("cases/{id}/promote")]
    [ProducesResponseType(typeof(DiscoveredCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscoveredCaseDto>> PromoteToCase(int id, [FromBody] PromoteToCaseRequest request)
    {
        try
        {
            var (discoveredCase, promotedCase) = await _reviewService.PromoteToCaseAsync(id, request);
            
            var dto = MapToDto(discoveredCase);
            dto.PromotedCaseId = promotedCase.Id;
            
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    private static DiscoveredCaseDto MapToDto(DiscoveredCase entity)
    {
        return new DiscoveredCaseDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Summary = entity.Summary,
            SourceUrl = entity.SourceUrl,
            SourceName = entity.SourceName,
            SourceType = entity.SourceType,
            PublishedDate = entity.PublishedDate,
            DiscoveredAt = entity.DiscoveredAt,
            Status = entity.Status,
            ReviewedBy = entity.ReviewedBy,
            ReviewedAt = entity.ReviewedAt,
            ReviewNotes = entity.ReviewNotes,
            PromotedCaseId = entity.PromotedCaseId
        };
    }
}
