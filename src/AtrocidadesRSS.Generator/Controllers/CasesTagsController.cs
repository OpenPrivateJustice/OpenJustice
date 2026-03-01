using Microsoft.AspNetCore.Mvc;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AtrocidadesRSS.Generator.Controllers;

/// <summary>
/// Controller for managing tag association with cases.
/// </summary>
[ApiController]
[Route("api/cases/{caseId}/tags")]
public class CasesTagsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CasesTagsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets all tags associated with a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tags for the case.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<Infrastructure.Persistence.Entities.Tag>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTagsForCase(int caseId, CancellationToken cancellationToken)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.CaseTags)
            .ThenInclude(ct => ct.Tag)
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

        var tags = caseEntity.CaseTags.Select(ct => ct.Tag!).ToList();
        return Ok(tags);
    }

    /// <summary>
    /// Adds a tag to a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="request">The tag association request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The associated tag.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Infrastructure.Persistence.Entities.Tag), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddTag(int caseId, [FromBody] AddTagRequest request, CancellationToken cancellationToken)
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

        // Validate tag ID or name is provided
        if (request.TagId.HasValue)
        {
            // Associate by Tag ID
            var tag = await _dbContext.Tags.FindAsync(new object[] { request.TagId.Value }, cancellationToken);
            if (tag == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = $"Tag with ID {request.TagId} not found.",
                    Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
                });
            }

            // Check for duplicate association
            var existingAssociation = await _dbContext.CaseTags
                .FirstOrDefaultAsync(ct => ct.CaseId == caseId && ct.TagId == request.TagId.Value, cancellationToken);
            if (existingAssociation != null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Duplicate Tag",
                    Detail = "This tag is already associated with the case.",
                    Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
                });
            }

            var caseTag = new Infrastructure.Persistence.Entities.CaseTag
            {
                CaseId = caseId,
                TagId = tag.Id,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.CaseTags.Add(caseTag);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetTagsForCase), new { caseId }, tag);
        }
        else if (!string.IsNullOrWhiteSpace(request.TagName))
        {
            // Find or create tag by name
            var tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Name == request.TagName, cancellationToken);
            if (tag == null)
            {
                // Create new tag if it doesn't exist
                tag = new Infrastructure.Persistence.Entities.Tag
                {
                    Name = request.TagName,
                    Description = request.Description,
                    Category = request.Category,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Tags.Add(tag);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // Check for duplicate association
            var existingAssociationByName = await _dbContext.CaseTags
                .FirstOrDefaultAsync(ct => ct.CaseId == caseId && ct.TagId == tag.Id, cancellationToken);
            if (existingAssociationByName != null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Duplicate Tag",
                    Detail = "This tag is already associated with the case.",
                    Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
                });
            }

            var caseTag = new Infrastructure.Persistence.Entities.CaseTag
            {
                CaseId = caseId,
                TagId = tag.Id,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.CaseTags.Add(caseTag);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetTagsForCase), new { caseId }, tag);
        }

        return BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "Either TagId or TagName must be provided.",
            Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
        });
    }

    /// <summary>
    /// Removes a tag from a case.
    /// </summary>
    /// <param name="caseId">The case ID.</param>
    /// <param name="tagId">The tag ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{tagId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTag(int caseId, int tagId, CancellationToken cancellationToken)
    {
        var caseTag = await _dbContext.CaseTags
            .FirstOrDefaultAsync(ct => ct.CaseId == caseId && ct.TagId == tagId, cancellationToken);

        if (caseTag == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = $"Tag with ID {tagId} is not associated with case {caseId}.",
                Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
            });
        }

        _dbContext.CaseTags.Remove(caseTag);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

/// <summary>
/// Request model for adding a tag to a case.
/// </summary>
public class AddTagRequest
{
    /// <summary>
    /// ID of an existing tag to associate.
    /// </summary>
    public int? TagId { get; set; }

    /// <summary>
    /// Name of a tag to find or create.
    /// </summary>
    public string? TagName { get; set; }

    /// <summary>
    /// Description for a new tag.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category for a new tag.
    /// </summary>
    public string? Category { get; set; }
}
