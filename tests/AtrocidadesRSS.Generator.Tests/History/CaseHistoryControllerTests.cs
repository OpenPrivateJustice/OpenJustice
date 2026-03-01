using Microsoft.EntityFrameworkCore;
using AtrocidadesRSS.Generator.Contracts.Cases;
using AtrocidadesRSS.Generator.Controllers;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using AtrocidadesRSS.Generator.Services.Cases;
using AtrocidadesRSS.Generator.Services.History;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AtrocidadesRSS.Generator.Tests.History;

public class CaseHistoryControllerTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new AppDbContext(options);
        
        // Seed lookup tables
        context.CrimeTypes.Add(new CrimeType { Id = 1, Name = "Homicídio", Confidence = 100 });
        context.CaseTypes.Add(new CaseType { Id = 1, Name = "Consumado", Confidence = 100 });
        context.JudicialStatuses.Add(new JudicialStatus { Id = 1, Name = "Em Andamento", Confidence = 100 });
        
        context.SaveChanges();
        
        return context;
    }

    private Case CreateTestCase(AppDbContext context)
    {
        var referenceCodeGenerator = new CaseReferenceCodeGenerator(context);
        var fieldHistoryService = new CaseFieldHistoryService(context);
        
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

        var workflowService = new Services.Cases.CaseWorkflowService(context, referenceCodeGenerator, fieldHistoryService);
        return workflowService.CreateCaseAsync(request).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetCaseHistory_ValidCaseId_ReturnsHistoryOrderedByChangedAtDescending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        // Add some history entries manually
        var historyService = new CaseFieldHistoryService(context);
        
        var oldCase = new Case
        {
            Id = testCase.Id,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Original",
            VictimConfidence = 50,
            AccusedName = "Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Description",
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var newCase = new Case
        {
            Id = testCase.Id,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Updated",
            VictimConfidence = 80,
            AccusedName = "Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Description",
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await historyService.AppendChangesAsync(testCase.Id, oldCase, newCase, "curator-1");

        var controller = new CaseHistoryController(context, historyService);

        // Act
        var result = await controller.GetCaseHistory(testCase.Id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var history = okResult.Value.Should().BeOfType<List<CaseFieldHistoryDto>>().Subject;
        
        history.Should().NotBeEmpty();
        history.Should().BeInDescendingOrder(h => h.ChangedAt);
    }

    [Fact]
    public async Task GetCaseHistory_NonExistentCaseId_Returns404()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var historyService = new CaseFieldHistoryService(context);
        var controller = new CaseHistoryController(context, historyService);

        // Act
        var result = await controller.GetCaseHistory(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result.Result!;
        notFoundResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task GetFieldHistory_ValidCaseAndField_ReturnsFieldHistory()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        // Add history
        var historyService = new CaseFieldHistoryService(context);
        
        var oldCase = new Case
        {
            Id = testCase.Id,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Original",
            VictimConfidence = 50,
            AccusedName = "Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Description",
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var newCase = new Case
        {
            Id = testCase.Id,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Updated",
            VictimConfidence = 80,
            AccusedName = "Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Description",
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await historyService.AppendChangesAsync(testCase.Id, oldCase, newCase, "curator-1");

        var controller = new CaseHistoryController(context, historyService);

        // Act
        var result = await controller.GetFieldHistory(testCase.Id, "VictimName");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var history = okResult.Value.Should().BeOfType<List<CaseFieldHistoryDto>>().Subject;
        
        history.Should().HaveCount(1);
        history[0].FieldName.Should().Be("VictimName");
        history[0].OldValue.Should().Contain("Original");
        history[0].NewValue.Should().Contain("Updated");
    }

    [Fact]
    public async Task GetFieldHistory_NonExistentCaseId_Returns404()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var historyService = new CaseFieldHistoryService(context);
        var controller = new CaseHistoryController(context, historyService);

        // Act
        var result = await controller.GetFieldHistory(999, "VictimName");

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetCaseHistory_ContainsAllRequiredFields()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        // Add history
        var historyService = new CaseFieldHistoryService(context);
        
        var oldCase = new Case
        {
            Id = testCase.Id,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Original",
            VictimConfidence = 50,
            AccusedName = "Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Description",
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var newCase = new Case
        {
            Id = testCase.Id,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Updated",
            VictimConfidence = 80,
            AccusedName = "Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Description",
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await historyService.AppendChangesAsync(testCase.Id, oldCase, newCase, "curator-1");

        var controller = new CaseHistoryController(context, historyService);

        // Act
        var result = await controller.GetCaseHistory(testCase.Id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var history = okResult.Value.Should().BeOfType<List<CaseFieldHistoryDto>>().Subject;
        
        var entry = history.First();
        entry.Id.Should().BeGreaterThan(0);
        entry.CaseId.Should().Be(testCase.Id);
        entry.FieldName.Should().NotBeEmpty();
        entry.ChangedAt.Should().NotBe(default);
        entry.CuratorId.Should().Be("curator-1");
        entry.ChangeConfidence.Should().BeGreaterOrEqualTo(0);
        entry.ChangeConfidence.Should().BeLessOrEqualTo(100);
        entry.CreatedAt.Should().NotBe(default);
    }
}
