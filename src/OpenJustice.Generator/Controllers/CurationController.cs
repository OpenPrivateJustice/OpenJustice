using Microsoft.AspNetCore.Mvc;
using OpenJustice.Generator.Contracts.Curation;
using OpenJustice.Generator.Services.Curation;
using OpenJustice.Generator.Infrastructure.Persistence.Entities;

namespace OpenJustice.Generator.Controllers;

/// <summary>
/// Controller for case curation workflow operations.
/// </summary>
[ApiController]
[Route("api/curation/cases")]
public class CurationController : ControllerBase
{
    private readonly ICurationService _curationService;

    public CurationController(ICurationService curationService)
    {
        _curationService = curationService;
    }

    /// <summary>
    /// Approves a case for publication.
    /// </summary>
    /// <param name="id">The case ID.</param>
    /// <param name="request">The approval request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated case.</returns>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(Case), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(int id, [FromBody] ApproveCaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var approvedCase = await _curationService.ApproveAsync(
                id, 
                request.CuratorId, 
                request.Notes, 
                cancellationToken);
            
            return Ok(approvedCase);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid State Transition",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }
    }

    /// <summary>
    /// Rejects a case from publication.
    /// </summary>
    /// <param name="id">The case ID.</param>
    /// <param name="request">The rejection request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated case.</returns>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(typeof(Case), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectCaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var rejectedCase = await _curationService.RejectAsync(
                id, 
                request.CuratorId, 
                request.Notes, 
                cancellationToken);
            
            return Ok(rejectedCase);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid State Transition",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }
    }

    /// <summary>
    /// Marks a case as verified by a curator.
    /// </summary>
    /// <param name="id">The case ID.</param>
    /// <param name="request">The verification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated case.</returns>
    [HttpPost("{id}/verify")]
    [ProducesResponseType(typeof(Case), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Verify(int id, [FromBody] VerifyCaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var verifiedCase = await _curationService.VerifyAsync(
                id, 
                request.CuratorId, 
                request.Notes, 
                cancellationToken);
            
            return Ok(verifiedCase);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid State Transition",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }
    }
}
