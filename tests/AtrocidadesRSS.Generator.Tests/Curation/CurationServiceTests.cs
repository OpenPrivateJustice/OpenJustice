using Microsoft.EntityFrameworkCore;
using AtrocidadesRSS.Generator.Domain.Enums;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using AtrocidadesRSS.Generator.Services.Cases;
using AtrocidadesRSS.Generator.Services.Curation;
using AtrocidadesRSS.Generator.Services.History;
using FluentAssertions;
using Xunit;

namespace AtrocidadesRSS.Generator.Tests.Curation;

public class CurationServiceTests
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
        
        var request = new Contracts.Cases.CreateCaseRequest
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

        var workflowService = new CaseWorkflowService(context, referenceCodeGenerator);
        return workflowService.CreateCaseAsync(request).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ApproveAsync_PendingCase_ApprovesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act
        var result = await curationService.ApproveAsync(testCase.Id, "curator-001", "Approved for publication");

        // Assert
        result.Should().NotBeNull();
        result.CurationStatus.Should().Be(CurationStatus.Approved);
        result.CuratorId.Should().Be("curator-001");
        result.CurationTimestamp.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveAsync_PendingCase_CreatesAuditLogEntry()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act
        await curationService.ApproveAsync(testCase.Id, "curator-001", "Approved for publication");

        // Assert
        var auditLogs = await auditLogService.GetAuditLogsForCaseAsync(testCase.Id);
        auditLogs.Should().HaveCount(1);
        auditLogs[0].ActionType.Should().Be("Approved");
        auditLogs[0].PreviousStatus.Should().Be(CurationStatus.Pending);
        auditLogs[0].NewStatus.Should().Be(CurationStatus.Approved);
        auditLogs[0].CuratorId.Should().Be("curator-001");
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // First approve
        await curationService.ApproveAsync(testCase.Id, "curator-001");

        // Act & Assert - Try to approve again
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            curationService.ApproveAsync(testCase.Id, "curator-002"));
    }

    [Fact]
    public async Task ApproveAsync_RejectedCase_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // First reject
        await curationService.RejectAsync(testCase.Id, "curator-001", "Rejected for review");

        // Act & Assert - Try to approve after rejection
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            curationService.ApproveAsync(testCase.Id, "curator-002"));
    }

    [Fact]
    public async Task RejectAsync_PendingCase_RejectsSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act
        var result = await curationService.RejectAsync(testCase.Id, "curator-001", "Does not meet criteria");

        // Assert
        result.Should().NotBeNull();
        result.CurationStatus.Should().Be(CurationStatus.Rejected);
        result.CuratorId.Should().Be("curator-001");
    }

    [Fact]
    public async Task RejectAsync_PendingCase_CreatesAuditLogEntry()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act
        await curationService.RejectAsync(testCase.Id, "curator-001", "Does not meet criteria");

        // Assert
        var auditLogs = await auditLogService.GetAuditLogsForCaseAsync(testCase.Id);
        auditLogs.Should().HaveCount(1);
        auditLogs[0].ActionType.Should().Be("Rejected");
        auditLogs[0].PreviousStatus.Should().Be(CurationStatus.Pending);
        auditLogs[0].NewStatus.Should().Be(CurationStatus.Rejected);
        auditLogs[0].Notes.Should().Be("Does not meet criteria");
    }

    [Fact]
    public async Task RejectAsync_ApprovedCase_RejectsSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // First approve
        await curationService.ApproveAsync(testCase.Id, "curator-001");

        // Act - Reject after approval
        var result = await curationService.RejectAsync(testCase.Id, "curator-002", "Found issues");

        // Assert
        result.CurationStatus.Should().Be(CurationStatus.Rejected);
    }

    [Fact]
    public async Task RejectAsync_AlreadyRejected_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // First reject
        await curationService.RejectAsync(testCase.Id, "curator-001", "Rejected");

        // Act & Assert - Try to reject again
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            curationService.RejectAsync(testCase.Id, "curator-002", "Rejected again"));
    }

    [Fact]
    public async Task VerifyAsync_ApprovedCase_VerifiesSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // First approve
        await curationService.ApproveAsync(testCase.Id, "curator-001");

        // Act
        var result = await curationService.VerifyAsync(testCase.Id, "verifier-001", "Verified the content");

        // Assert
        result.Should().NotBeNull();
        result.IsVerified.Should().BeTrue();
        result.CuratorId.Should().Be("verifier-001"); // Curator ID updated to verifier
    }

    [Fact]
    public async Task VerifyAsync_ApprovedCase_CreatesAuditLogEntry()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // First approve
        await curationService.ApproveAsync(testCase.Id, "curator-001");

        // Act
        await curationService.VerifyAsync(testCase.Id, "verifier-001", "Verified the content");

        // Assert
        var auditLogs = await auditLogService.GetAuditLogsForCaseAsync(testCase.Id);
        auditLogs.Should().HaveCount(2); // Approve + Verify
        
        var verifyLog = auditLogs.Last();
        verifyLog.ActionType.Should().Be("Verified");
        verifyLog.CuratorId.Should().Be("verifier-001");
    }

    [Fact]
    public async Task VerifyAsync_PendingCase_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act & Assert - Cannot verify pending case
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            curationService.VerifyAsync(testCase.Id, "verifier-001"));
    }

    [Fact]
    public async Task VerifyAsync_RejectedCase_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // First reject
        await curationService.RejectAsync(testCase.Id, "curator-001", "Rejected");

        // Act & Assert - Cannot verify rejected case
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            curationService.VerifyAsync(testCase.Id, "verifier-001"));
    }

    [Fact]
    public async Task ApproveAsync_NonExistingCase_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            curationService.ApproveAsync(999, "curator-001"));
    }

    [Fact]
    public async Task RejectAsync_NonExistingCase_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            curationService.RejectAsync(999, "curator-001", "Test"));
    }

    [Fact]
    public async Task VerifyAsync_NonExistingCase_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            curationService.VerifyAsync(999, "verifier-001"));
    }

    [Fact]
    public async Task GetCaseByIdAsync_ExistingCase_ReturnsCaseWithCurationStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act
        var result = await curationService.GetCaseByIdAsync(testCase.Id);

        // Assert
        result.Should().NotBeNull();
        result!.CurationStatus.Should().Be(CurationStatus.Pending); // Default status
    }

    [Fact]
    public async Task ApproveAsync_WithNotes_StoresNotesInAuditLog()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var testCase = CreateTestCase(context);
        
        var auditLogService = new CaseAuditLogService(context);
        var curationService = new CurationService(context, auditLogService);

        // Act
        await curationService.ApproveAsync(testCase.Id, "curator-001", "Approved after thorough review");

        // Assert
        var auditLogs = await auditLogService.GetAuditLogsForCaseAsync(testCase.Id);
        auditLogs[0].Notes.Should().Be("Approved after thorough review");
    }
}
