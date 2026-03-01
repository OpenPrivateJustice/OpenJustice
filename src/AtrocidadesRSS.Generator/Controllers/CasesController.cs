using Microsoft.AspNetCore.Mvc;
using AtrocidadesRSS.Generator.Contracts.Cases;
using AtrocidadesRSS.Generator.Services.Cases;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;

namespace AtrocidadesRSS.Generator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CasesController : ControllerBase
{
    private readonly ICaseWorkflowService _caseWorkflowService;

    public CasesController(ICaseWorkflowService caseWorkflowService)
    {
        _caseWorkflowService = caseWorkflowService;
    }

    /// <summary>
    /// Creates a new case with a generated ATRO reference code.
    /// </summary>
    /// <param name="request">The case creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created case with reference code.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Case), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var createdCase = await _caseWorkflowService.CreateCaseAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = createdCase.Id }, createdCase);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }
    }

    /// <summary>
    /// Updates an existing case. Reference code remains unchanged.
    /// </summary>
    /// <param name="id">The case ID.</param>
    /// <param name="request">The case update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated case.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Case), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updatedCase = await _caseWorkflowService.UpdateCaseAsync(id, request, cancellationToken);
            return Ok(updatedCase);
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
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = ex.Message,
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }
    }

    /// <summary>
    /// Gets a case by ID.
    /// </summary>
    /// <param name="id">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The case if found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Case), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var caseEntity = await _caseWorkflowService.GetCaseByIdAsync(id, cancellationToken);
        
        if (caseEntity == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = $"Case with ID {id} not found.",
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }
        
        return Ok(caseEntity);
    }
}
