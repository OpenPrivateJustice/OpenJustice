using Microsoft.AspNetCore.Mvc;
using Moq;
using AtrocidadesRSS.Generator.Controllers;
using AtrocidadesRSS.Generator.Contracts.Cases;
using AtrocidadesRSS.Generator.Services.Cases;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using FluentAssertions;
using Xunit;

namespace AtrocidadesRSS.Generator.Tests.Cases;

public class CasesControllerTests
{
    private readonly Mock<ICaseWorkflowService> _mockWorkflowService;
    private readonly CasesController _controller;

    public CasesControllerTests()
    {
        _mockWorkflowService = new Mock<ICaseWorkflowService>();
        _controller = new CasesController(_mockWorkflowService.Object);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Test Victim",
            AccusedName = "Test Accused",
            CrimeDescription = "Test Description",
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            VictimConfidence = 50,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50
        };

        var createdCase = new Case
        {
            Id = 1,
            ReferenceCode = "ATRO-2026-0001",
            RegistrationDate = DateTime.UtcNow,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Test Victim",
            AccusedName = "Test Accused",
            CrimeDescription = "Test Description",
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            VictimConfidence = 50,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockWorkflowService
            .Setup(s => s.CreateCaseAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCase);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(CasesController.GetById));
        createdResult.RouteValues!["id"].Should().Be(1);
        
        var returnedCase = createdResult.Value.Should().BeOfType<Case>().Subject;
        returnedCase.ReferenceCode.Should().Be("ATRO-2026-0001");
    }

    [Fact]
    public async Task Create_InvalidFK_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCaseRequest
        {
            CrimeTypeId = 999, // Invalid
            CaseTypeId = 1,
            JudicialStatusId = 1,
            NumberOfVictims = 1,
            NumberOfAccused = 1
        };

        _mockWorkflowService
            .Setup(s => s.CreateCaseAsync(It.IsAny<CreateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid foreign key."));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new UpdateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Updated Victim",
            NumberOfVictims = 2,
            NumberOfAccused = 1,
            VictimConfidence = 75,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50
        };

        var existingCase = new Case
        {
            Id = 1,
            ReferenceCode = "ATRO-2026-0001",
            RegistrationDate = DateTime.UtcNow,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Updated Victim",
            NumberOfVictims = 2,
            NumberOfAccused = 1,
            VictimConfidence = 75,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockWorkflowService
            .Setup(s => s.UpdateCaseAsync(It.IsAny<int>(), It.IsAny<UpdateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCase);

        // Act
        var result = await _controller.Update(1, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCase = okResult.Value.Should().BeOfType<Case>().Subject;
        returnedCase.VictimName.Should().Be("Updated Victim");
        returnedCase.ReferenceCode.Should().Be("ATRO-2026-0001"); // Unchanged
    }

    [Fact]
    public async Task Update_CaseNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            NumberOfVictims = 1,
            NumberOfAccused = 1
        };

        _mockWorkflowService
            .Setup(s => s.UpdateCaseAsync(It.IsAny<int>(), It.IsAny<UpdateCaseRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Case not found."));

        // Act
        var result = await _controller.Update(999, request, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetById_ExistingCase_ReturnsCase()
    {
        // Arrange
        var caseEntity = new Case
        {
            Id = 1,
            ReferenceCode = "ATRO-2026-0001",
            RegistrationDate = DateTime.UtcNow,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockWorkflowService
            .Setup(s => s.GetCaseByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseEntity);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCase = okResult.Value.Should().BeOfType<Case>().Subject;
        returnedCase.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_NonExistingCase_ReturnsNotFound()
    {
        // Arrange
        _mockWorkflowService
            .Setup(s => s.GetCaseByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Case?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
