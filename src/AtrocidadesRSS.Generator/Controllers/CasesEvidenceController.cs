using Microsoft.AspNetCore.Mvc;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AtrocidadesRSS.Generator.Controllers;

/// <summary>
/// Controller for managing evidence association with cases.
/// </summary>
[ApiController]
[Route("api/cases/{caseId}/evidence")]
public class CasesEvidenceController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CasesEvidenceController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets all evidence associated with a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of evidence for the case.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<Infrastructure.Persistence.Entities.Evidence>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvidenceForCase(int caseId, CancellationToken cancellationToken)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.Evidences)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (caseEntity == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = $"Case with ID {caseId} not found.",
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }

        return Ok(caseEntity.Evidences);
    }

    /// <summary>
    /// Adds evidence to a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="request">The evidence creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created evidence.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Infrastructure.Persistence.Entities.Evidence), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddEvidence(int caseId, [FromBody] AddEvidenceRequest request, CancellationToken cancellationToken)
    {
        // Validate case exists
        var caseEntity = await _dbContext.Cases.FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
        if (caseEntity == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = $"Case with ID {caseId} not found.",
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }

        // Validate evidence type is provided
        if (string.IsNullOrWhiteSpace(request.EvidenceType))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = "EvidenceType is required.",
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }

        // Check for duplicate evidence (same case and link or same case and filename)
        if (!string.IsNullOrWhiteSpace(request.Link))
        {
            var existingByLink = await _dbContext.Evidences
                .FirstOrDefaultAsync(e => e.CaseId == caseId && e.Link == request.Link, cancellationToken);
            if (existingByLink != null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Duplicate Evidence",
                    Detail = "Evidence with this link already exists for this case.",
                    Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(request.FileName))
        {
            var existingByFileName = await _dbContext.Evidences
                .FirstOrDefaultAsync(e => e.CaseId == caseId && e.FileName == request.FileName, cancellationToken);
            if (existingByFileName != null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Duplicate Evidence",
                    Detail = "Evidence with this filename already exists for this case.",
                    Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
                });
            }
        }

        var evidence = new Infrastructure.Persistence.Entities.Evidence
        {
            CaseId = caseId,
            EvidenceType = request.EvidenceType,
            Description = request.Description,
            Link = request.Link,
            FileName = request.FileName,
            Witnesses = request.Witnesses,
            Forensics = request.Forensics,
            Confidence = request.Confidence > 0 && request.Confidence <= 100 ? request.Confidence : 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Evidences.Add(evidence);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetEvidenceForCase), new { caseId }, evidence);
    }

    /// <summary>
    /// Removes evidence from a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="evidenceId">The evidence ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{evidenceId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEvidence(int caseId, int evidenceId, CancellationToken cancellationToken)
    {
        var evidence = await _dbContext.Evidences
            .FirstOrDefaultAsync(e => e.Id == evidenceId && e.CaseId == caseId, cancellationToken);

        if (evidence == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = $"Evidence with ID {evidenceId} not found for case {caseId}.",
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }

        _dbContext.Evidences.Remove(evidence);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

/// <summary>
/// Request model for adding evidence to a case.
/// </summary>
public class AddEvidenceRequest
{
    /// <summary>
    /// Type of evidence (e.g., "Document", "Photo", "Video", "Link", "Testimony").
    /// </summary>
    public string EvidenceType { get; set; } = string.Empty;

    /// <summary>
    /// Description of the evidence.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL link to the evidence.
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// File name if evidence is a document.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Witnesses associated with this evidence.
    /// </summary>
    public string? Witnesses { get; set; }

    /// <summary>
    /// Forensic details related to this evidence.
    /// </summary>
    public string? Forensics { get; set; }

    /// <summary>
    /// Confidence score (0-100).
    /// </summary>
    public int Confidence { get; set; } = 50;
}
