using Microsoft.EntityFrameworkCore;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using AtrocidadesRSS.Generator.Services.History;
using FluentAssertions;
using Xunit;

namespace AtrocidadesRSS.Generator.Tests.History;

public class CaseFieldHistoryServiceTests
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

    [Fact]
    public async Task AppendChangesAsync_NoExistingCase_CreatesHistoryForAllFields()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CaseFieldHistoryService(context);
        
        var newCase = new Case
        {
            Id = 1,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Test Victim",
            VictimConfidence = 80,
            AccusedName = "Test Accused",
            AccusedConfidence = 75,
            CrimeDescription = "Test Description",
            CrimeConfidence = 90,
            JudicialConfidence = 85,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await service.AppendChangesAsync(1, null, newCase, "curator-1");

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        
        // Should have entries for the populated fields
        var victimNameHistory = result.Should().Contain(h => h.FieldName == "VictimName").Subject;
        victimNameHistory.OldValue.Should().BeNull();
        victimNameHistory.NewValue.Should().Contain("Test Victim");
        victimNameHistory.CuratorId.Should().Be("curator-1");
        victimNameHistory.ChangeConfidence.Should().Be(80); // VictimConfidence
    }

    [Fact]
    public async Task AppendChangesAsync_WithExistingCase_CreatesHistoryOnlyForChangedFields()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CaseFieldHistoryService(context);
        
        var oldCase = new Case
        {
            Id = 1,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Old Victim Name",
            VictimConfidence = 50,
            AccusedName = "Old Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Old Description",
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
            Id = 1,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "New Victim Name", // Changed
            VictimConfidence = 80, // Changed
            AccusedName = "Old Accused", // Not changed
            AccusedConfidence = 50, // Not changed
            CrimeDescription = "Old Description", // Not changed
            CrimeConfidence = 50, // Not changed
            JudicialConfidence = 50, // Not changed
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await service.AppendChangesAsync(1, oldCase, newCase, "curator-1");

        // Assert
        result.Should().NotBeNull();
        
        // Should only have 2 entries (VictimName and VictimConfidence changed)
        result.Count.Should().Be(2);
        
        var victimNameHistory = result.Should().Contain(h => h.FieldName == "VictimName").Subject;
        victimNameHistory.OldValue.Should().Contain("Old Victim Name");
        victimNameHistory.NewValue.Should().Contain("New Victim Name");
        
        var victimConfidenceHistory = result.Should().Contain(h => h.FieldName == "VictimConfidence").Subject;
        victimConfidenceHistory.OldValue.Should().Be("50");
        victimConfidenceHistory.NewValue.Should().Be("80");
    }

    [Fact]
    public async Task AppendChangesAsync_AppendOnlyMode_PreviousHistoryNotModified()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CaseFieldHistoryService(context);
        
        // First update (simulating an edit to existing case)
        var oldCase1 = new Case
        {
            Id = 1,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Original Name",
            VictimConfidence = 50,
            AccusedName = "Original Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Original",
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var newCase1 = new Case
        {
            Id = 1,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "First Update",
            VictimConfidence = 60,
            AccusedName = "Original Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Original",
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await service.AppendChangesAsync(1, oldCase1, newCase1, "curator-1");
        
        // Second update
        var oldCase2 = newCase1;
        var newCase2 = new Case
        {
            Id = 1,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Second Update", // Changed again
            VictimConfidence = 70, // Changed
            AccusedName = "Original Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Original",
            CrimeConfidence = 50,
            JudicialConfidence = 50,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await service.AppendChangesAsync(1, oldCase2, newCase2, "curator-2");

        // Assert - Verify all history entries exist
        var allHistory = await context.CaseFieldHistories
            .Where(h => h.CaseId == 1)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        allHistory.Should().HaveCount(4); // 2 from first update + 2 from second update
        
        // First update entries should still exist with curator-1
        var firstUpdateEntries = allHistory.Where(h => h.CuratorId == "curator-1").ToList();
        firstUpdateEntries.Should().HaveCount(2);
        
        // Second update entries should exist with curator-2
        var secondUpdateEntries = allHistory.Where(h => h.CuratorId == "curator-2").ToList();
        secondUpdateEntries.Should().HaveCount(2);
        
        // Verify the chain of changes is preserved
        var victimNameHistory = allHistory.Where(h => h.FieldName == "VictimName").OrderBy(h => h.ChangedAt).ToList();
        victimNameHistory.Should().HaveCount(2); // 2 updates (first and second)
        
        victimNameHistory[0].OldValue.Should().Contain("Original Name");
        victimNameHistory[0].NewValue.Should().Contain("First Update");
        
        victimNameHistory[1].OldValue.Should().Contain("First Update");
        victimNameHistory[1].NewValue.Should().Contain("Second Update");
    }

    [Fact]
    public async Task GetHistoryForCaseAsync_ReturnsHistoryOrderedByChangedAtDescending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CaseFieldHistoryService(context);
        
        // Create initial case with history
        var oldCase = new Case
        {
            Id = 1,
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
            Id = 1,
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

        await service.AppendChangesAsync(1, oldCase, newCase, "curator-1");

        // Act
        var result = await service.GetHistoryForCaseAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeInDescendingOrder(h => h.ChangedAt);
    }

    [Fact]
    public async Task GetFieldHistoryAsync_ReturnsOnlySpecifiedField()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CaseFieldHistoryService(context);
        
        var oldCase = new Case
        {
            Id = 1,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Original",
            VictimConfidence = 50,
            AccusedName = "Original Accused",
            AccusedConfidence = 50,
            CrimeDescription = "Original",
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
            Id = 1,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Updated",
            VictimConfidence = 80,
            AccusedName = "Updated Accused",
            AccusedConfidence = 90,
            CrimeDescription = "Updated",
            CrimeConfidence = 85,
            JudicialConfidence = 75,
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await service.AppendChangesAsync(1, oldCase, newCase, "curator-1");

        // Act
        var result = await service.GetFieldHistoryAsync(1, "VictimName");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].FieldName.Should().Be("VictimName");
        result[0].OldValue.Should().Contain("Original");
        result[0].NewValue.Should().Contain("Updated");
    }

    [Fact]
    public async Task AppendChangesAsync_StoresChangeConfidence()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new CaseFieldHistoryService(context);
        
        var newCase = new Case
        {
            Id = 1,
            CrimeTypeId = 1,
            CaseTypeId = 1,
            JudicialStatusId = 1,
            VictimName = "Test Victim",
            VictimConfidence = 85, // High confidence
            AccusedName = "Test Accused",
            AccusedConfidence = 60, // Medium confidence
            CrimeDescription = "Test Description",
            CrimeConfidence = 95, // Very high confidence
            JudicialConfidence = 70, // Medium-high confidence
            NumberOfVictims = 1,
            NumberOfAccused = 1,
            RegistrationDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await service.AppendChangesAsync(1, null, newCase, "curator-1");

        // Assert
        var victimNameHistory = result.Should().Contain(h => h.FieldName == "VictimName").Subject;
        victimNameHistory.ChangeConfidence.Should().Be(85); // Should use VictimConfidence

        var accusedNameHistory = result.Should().Contain(h => h.FieldName == "AccusedName").Subject;
        accusedNameHistory.ChangeConfidence.Should().Be(60); // Should use AccusedConfidence
    }
}
