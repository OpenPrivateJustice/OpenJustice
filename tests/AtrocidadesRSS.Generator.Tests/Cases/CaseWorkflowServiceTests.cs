using Microsoft.EntityFrameworkCore;
using Moq;
using AtrocidadesRSS.Generator.Contracts.Cases;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using AtrocidadesRSS.Generator.Services.Cases;
using AtrocidadesRSS.Generator.Services.History;
using FluentAssertions;
using Xunit;

namespace AtrocidadesRSS.Generator.Tests.Cases;

public class CaseWorkflowServiceTests
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
        
        // Add more lookup data for FK validation tests
        context.CrimeTypes.Add(new CrimeType { Id = 2, Name = "Furto", Confidence = 100 });
        context.CaseTypes.Add(new CaseType { Id = 2, Name = "Tentativa", Confidence = 100 });
        context.JudicialStatuses.Add(new JudicialStatus { Id = 2, Name = "Encerrado", Confidence = 100 });
        
        context.SaveChanges();
        
        return context;
    }

    private ICaseWorkflowService CreateService(AppDbContext context)
    {
        var referenceCodeGenerator = new CaseReferenceCodeGenerator(context);
        var fieldHistoryService = new CaseFieldHistoryService(context);
        return new CaseWorkflowService(context, referenceCodeGenerator, fieldHistoryService);
    }

    [Fact]
    public async Task CreateCaseAsync_ValidRequest_CreatesCaseWithReferenceCode()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

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

        // Act
        var result = await service.CreateCaseAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.ReferenceCode.Should().StartWith("ATRO-");
        result.ReferenceCode.Should().MatchRegex(@"^ATRO-\d{4}-\d{4}$");
        result.CrimeTypeId.Should().Be(1);
        result.CaseTypeId.Should().Be(1);
        result.JudicialStatusId.Should().Be(1);
    }

    [Fact]
    public async Task CreateCaseAsync_InvalidCrimeTypeId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new CreateCaseRequest
        {
            CrimeTypeId = 999, // Invalid
            CaseTypeId = 1,
            JudicialStatusId = 1,
            NumberOfVictims = 1,
            NumberOfAccused = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCaseAsync(request));
    }

    [Fact]
    public async Task CreateCaseAsync_InvalidCaseTypeId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new CreateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 999, // Invalid
            JudicialStatusId = 1,
            NumberOfVictims = 1,
            NumberOfAccused = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCaseAsync(request));
    }

    [Fact]
    public async Task CreateCaseAsync_InvalidJudicialStatusId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new CreateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 999, // Invalid
            NumberOfVictims = 1,
            NumberOfAccused = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCaseAsync(request));
    }

    [Fact]
    public async Task UpdateCaseAsync_ValidRequest_UpdatesCaseWithoutChangingReferenceCode()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // First create a case
        var createRequest = new CreateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Original Victim",
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            VictimConfidence = 50,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50
        };

        var createdCase = await service.CreateCaseAsync(createRequest);
        var originalReferenceCode = createdCase.ReferenceCode;

        // Now update
        var updateRequest = new UpdateCaseRequest
        {
            CrimeTypeId = 2,
            CaseTypeId = 2,
            JudicialStatusId = 2,
            VictimName = "Updated Victim",
            NumberOfVictims = 2,
            NumberOfAccused = 1,
            VictimConfidence = 75,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50
        };

        // Act
        var result = await service.UpdateCaseAsync(createdCase.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.VictimName.Should().Be("Updated Victim");
        result.NumberOfVictims.Should().Be(2);
        result.CrimeTypeId.Should().Be(2);
        result.ReferenceCode.Should().Be(originalReferenceCode); // Unchanged!
    }

    [Fact]
    public async Task UpdateCaseAsync_NonExistingCase_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new UpdateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            NumberOfVictims = 1,
            NumberOfAccused = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateCaseAsync(999, request));
    }

    [Fact]
    public async Task GetCaseByIdAsync_ExistingCase_ReturnsCase()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var createRequest = new CreateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Test Victim",
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            VictimConfidence = 50,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50
        };

        var createdCase = await service.CreateCaseAsync(createRequest);

        // Act
        var result = await service.GetCaseByIdAsync(createdCase.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdCase.Id);
        result.VictimName.Should().Be("Test Victim");
    }

    [Fact]
    public async Task GetCaseByIdAsync_NonExistingCase_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.GetCaseByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateForeignKeyAsync_AllValid_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.ValidateForeignKeyAsync(1, 1, 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateForeignKeyAsync_InvalidCrimeType_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.ValidateForeignKeyAsync(999, 1, 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateForeignKeyAsync_InvalidCaseType_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.ValidateForeignKeyAsync(1, 999, 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateForeignKeyAsync_InvalidJudicialStatus_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act
        var result = await service.ValidateForeignKeyAsync(1, 1, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCaseAsync_MultipleCases_GeneratesSequentialReferenceCodes()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        // Act - Create multiple cases
        var case1 = await service.CreateCaseAsync(new CreateCaseRequest
        {
            CrimeTypeId = 1, CaseTypeId = 1, JudicialStatusId = 1,
            NumberOfVictims = 1, NumberOfAccused = 1,
            VictimConfidence = 50, AccusedConfidence = 50,
            CrimeConfidence = 50, JudicialConfidence = 50
        });

        var case2 = await service.CreateCaseAsync(new CreateCaseRequest
        {
            CrimeTypeId = 1, CaseTypeId = 1, JudicialStatusId = 1,
            NumberOfVictims = 1, NumberOfAccused = 1,
            VictimConfidence = 50, AccusedConfidence = 50,
            CrimeConfidence = 50, JudicialConfidence = 50
        });

        // Assert
        case1.ReferenceCode.Should().NotBe(case2.ReferenceCode);
        case1.ReferenceCode.Should().MatchRegex(@"^ATRO-2026-0001$");
        case2.ReferenceCode.Should().MatchRegex(@"^ATRO-2026-0002$");
    }

    [Fact]
    public async Task CreateCaseAsync_CreatesInitialFieldHistory()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new CreateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Test Victim Name",
            AccusedName = "Test Accused Name",
            CrimeDescription = "Test Crime Description",
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            VictimConfidence = 75,
            AccusedConfidence = 80,
            CrimeConfidence = 85,
            JudicialConfidence = 90,
            CuratorId = "curator-001"
        };

        // Act
        var createdCase = await service.CreateCaseAsync(request);

        // Assert - verify history was created for the new case
        var historyService = new CaseFieldHistoryService(context);
        var historyEntries = await historyService.GetHistoryForCaseAsync(createdCase.Id);

        historyEntries.Should().NotBeEmpty("History should be created for newly created case");
        
        // Verify we have entries for key tracked fields
        var victimNameHistory = historyEntries.FirstOrDefault(h => h.FieldName == nameof(Infrastructure.Persistence.Entities.Case.VictimName));
        victimNameHistory.Should().NotBeNull("VictimName should have history entry");
        victimNameHistory!.OldValue.Should().BeNull("Old value should be null for initial creation");
        victimNameHistory.NewValue.Should().Contain("Test Victim Name");
        victimNameHistory.CuratorId.Should().Be("curator-001");
        victimNameHistory.ChangeConfidence.Should().Be(75);
        
        // Verify timestamps are set
        victimNameHistory.ChangedAt.Should().NotBe(default);
        victimNameHistory.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task CreateCaseAsync_HistoryHasNullToValueTransitions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);

        var request = new CreateCaseRequest
        {
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            CrimeLocationCity = "São Paulo",
            CrimeLocationState = "SP",
            CrimeDescription = "Initial crime description",
            NumberOfVictims = 2,
            NumberOfAccused = 1,
            VictimConfidence = 50,
            AccusedConfidence = 50,
            CrimeConfidence = 50,
            JudicialConfidence = 50
        };

        // Act
        var createdCase = await service.CreateCaseAsync(request);

        // Assert - verify all history entries have null old values
        var historyService = new CaseFieldHistoryService(context);
        var historyEntries = await historyService.GetHistoryForCaseAsync(createdCase.Id);

        historyEntries.Should().NotBeEmpty();
        
        foreach (var entry in historyEntries)
        {
            entry.OldValue.Should().BeNull($"Field {entry.FieldName} should have null old value for initial creation");
            entry.NewValue.Should().NotBeNull($"Field {entry.FieldName} should have non-null new value");
        }
    }
}
