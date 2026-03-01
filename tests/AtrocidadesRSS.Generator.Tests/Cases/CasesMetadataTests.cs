using Microsoft.AspNetCore.Mvc;
using AtrocidadesRSS.Generator.Controllers;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace AtrocidadesRSS.Generator.Tests.Cases;

public class CasesMetadataTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly CasesEvidenceController _evidenceController;
    private readonly CasesTagsController _tagsController;

    public CasesMetadataTests()
    {
        // Use in-memory database for tests
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new AppDbContext(options);
        _evidenceController = new CasesEvidenceController(_dbContext);
        _tagsController = new CasesTagsController(_dbContext);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add a case
        var testCase = new Case
        {
            Id = 1,
            ReferenceCode = "ATRO-2026-0001",
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            VictimConfidence = 50,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Cases.Add(testCase);

        // Add another case
        var testCase2 = new Case
        {
            Id = 2,
            ReferenceCode = "ATRO-2026-0002",
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            VictimConfidence = 50,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Cases.Add(testCase2);

        // Add some tags
        var tag1 = new Tag { Id = 1, Name = "Urgent", Category = "Priority", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var tag2 = new Tag { Id = 2, Name = "Review", Category = "Status", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _dbContext.Tags.AddRange(tag1, tag2);

        _dbContext.SaveChanges();
    }

    #region Evidence Tests

    [Fact]
    public async Task GetEvidenceForCase_ExistingCase_ReturnsEvidenceList()
    {
        // Arrange
        var caseId = 1;

        // Add evidence to case
        var evidence = new Evidence
        {
            Id = 1,
            CaseId = caseId,
            EvidenceType = "Document",
            Description = "Test document",
            Confidence = 75,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Evidences.Add(evidence);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _evidenceController.GetEvidenceForCase(caseId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var evidenceList = okResult.Value.Should().BeOfType<List<Evidence>>().Subject;
        evidenceList.Should().HaveCount(1);
        evidenceList.First().EvidenceType.Should().Be("Document");
    }

    [Fact]
    public async Task GetEvidenceForCase_NonExistentCase_ReturnsNotFound()
    {
        // Act
        var result = await _evidenceController.GetEvidenceForCase(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddEvidence_ValidRequest_CreatesEvidence()
    {
        // Arrange
        var caseId = 1;
        var request = new AddEvidenceRequest
        {
            EvidenceType = "Photo",
            Description = "Crime scene photo",
            Link = "https://example.com/photo.jpg",
            Confidence = 80
        };

        // Act
        var result = await _evidenceController.AddEvidence(caseId, request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var evidence = createdResult.Value.Should().BeOfType<Evidence>().Subject;
        evidence.EvidenceType.Should().Be("Photo");
        evidence.CaseId.Should().Be(caseId);

        // Verify in database
        var dbEvidence = await _dbContext.Evidences.FirstOrDefaultAsync(e => e.CaseId == caseId);
        dbEvidence.Should().NotBeNull();
    }

    [Fact]
    public async Task AddEvidence_MissingEvidenceType_ReturnsBadRequest()
    {
        // Arrange
        var caseId = 1;
        var request = new AddEvidenceRequest
        {
            EvidenceType = "", // Invalid
            Description = "Test"
        };

        // Act
        var result = await _evidenceController.AddEvidence(caseId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddEvidence_DuplicateLink_ReturnsConflict()
    {
        // Arrange
        var caseId = 1;
        var existingLink = "https://example.com/doc.pdf";
        
        // Add existing evidence
        var existingEvidence = new Evidence
        {
            CaseId = caseId,
            EvidenceType = "Document",
            Link = existingLink,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Evidences.Add(existingEvidence);
        await _dbContext.SaveChangesAsync();

        // Try to add duplicate
        var request = new AddEvidenceRequest
        {
            EvidenceType = "Document",
            Link = existingLink
        };

        // Act
        var result = await _evidenceController.AddEvidence(caseId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task AddEvidence_CaseNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new AddEvidenceRequest
        {
            EvidenceType = "Document",
            Description = "Test"
        };

        // Act
        var result = await _evidenceController.AddEvidence(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveEvidence_ExistingEvidence_ReturnsNoContent()
    {
        // Arrange
        var caseId = 1;
        var evidenceId = 1;
        
        var evidence = new Evidence
        {
            Id = evidenceId,
            CaseId = caseId,
            EvidenceType = "Document",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Evidences.Add(evidence);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _evidenceController.RemoveEvidence(caseId, evidenceId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        // Verify deleted
        var deletedEvidence = await _dbContext.Evidences.FindAsync(new object[] { evidenceId });
        deletedEvidence.Should().BeNull();
    }

    [Fact]
    public async Task RemoveEvidence_NonExistentEvidence_ReturnsNotFound()
    {
        // Act
        var result = await _evidenceController.RemoveEvidence(1, 999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Tags Tests

    [Fact]
    public async Task GetTagsForCase_ExistingCase_ReturnsTagsList()
    {
        // Arrange
        var caseId = 1;
        
        // Add tag association
        var caseTag = new CaseTag
        {
            CaseId = caseId,
            TagId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CaseTags.Add(caseTag);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _tagsController.GetTagsForCase(caseId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var tags = okResult.Value.Should().BeOfType<List<Tag>>().Subject;
        tags.Should().HaveCount(1);
        tags.First().Name.Should().Be("Urgent");
    }

    [Fact]
    public async Task GetTagsForCase_NonExistentCase_ReturnsNotFound()
    {
        // Act
        var result = await _tagsController.GetTagsForCase(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddTag_ByTagId_AssociatesExistingTag()
    {
        // Arrange
        var caseId = 1;
        var request = new AddTagRequest { TagId = 1 };

        // Act
        var result = await _tagsController.AddTag(caseId, request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var tag = createdResult.Value.Should().BeOfType<Tag>().Subject;
        tag.Name.Should().Be("Urgent");

        // Verify association in database
        var association = await _dbContext.CaseTags.FirstOrDefaultAsync(ct => ct.CaseId == caseId && ct.TagId == 1);
        association.Should().NotBeNull();
    }

    [Fact]
    public async Task AddTag_ByTagName_CreatesNewTag()
    {
        // Arrange
        var caseId = 1;
        var request = new AddTagRequest { TagName = "NewTag", Category = "Custom" };

        // Act
        var result = await _tagsController.AddTag(caseId, request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var tag = createdResult.Value.Should().BeOfType<Tag>().Subject;
        tag.Name.Should().Be("NewTag");
        tag.Category.Should().Be("Custom");

        // Verify tag created in database
        var newTag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Name == "NewTag");
        newTag.Should().NotBeNull();
    }

    [Fact]
    public async Task AddTag_DuplicateAssociation_ReturnsConflict()
    {
        // Arrange
        var caseId = 1;
        
        // Add existing association
        var caseTag = new CaseTag
        {
            CaseId = caseId,
            TagId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CaseTags.Add(caseTag);
        await _dbContext.SaveChangesAsync();

        // Try to add duplicate
        var request = new AddTagRequest { TagId = 1 };

        // Act
        var result = await _tagsController.AddTag(caseId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task AddTag_TagIdNotFound_ReturnsNotFound()
    {
        // Arrange
        var caseId = 1;
        var request = new AddTagRequest { TagId = 999 };

        // Act
        var result = await _tagsController.AddTag(caseId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddTag_NoTagIdOrName_ReturnsBadRequest()
    {
        // Arrange
        var caseId = 1;
        var request = new AddTagRequest { };

        // Act
        var result = await _tagsController.AddTag(caseId, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddTag_CaseNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new AddTagRequest { TagId = 1 };

        // Act
        var result = await _tagsController.AddTag(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveTag_ExistingAssociation_ReturnsNoContent()
    {
        // Arrange
        var caseId = 1;
        var tagId = 1;
        
        var caseTag = new CaseTag
        {
            CaseId = caseId,
            TagId = tagId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.CaseTags.Add(caseTag);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _tagsController.RemoveTag(caseId, tagId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        // Verify removed
        var association = await _dbContext.CaseTags.FirstOrDefaultAsync(ct => ct.CaseId == caseId && ct.TagId == tagId);
        association.Should().BeNull();
    }

    [Fact]
    public async Task RemoveTag_NonExistentAssociation_ReturnsNotFound()
    {
        // Act
        var result = await _tagsController.RemoveTag(1, 999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
