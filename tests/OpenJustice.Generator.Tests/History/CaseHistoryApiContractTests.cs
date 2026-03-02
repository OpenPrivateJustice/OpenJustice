using System.Net;
using System.Net.Http.Json;
using OpenJustice.Generator.Web.Models.Cases;
using OpenJustice.Generator.Web.Services;
using OpenJustice.Generator.Contracts.Cases;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;

namespace OpenJustice.Generator.Tests.History;

/// <summary>
/// Tests for the API contract of history endpoints via GeneratorApiClient.
/// </summary>
public class CaseHistoryApiContractTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly GeneratorApiClient _client;

    public CaseHistoryApiContractTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost/")
        };
        _client = new GeneratorApiClient(_httpClient);
    }

    /// <summary>
    /// Test that GetCaseHistoryAsync returns empty list when case has no history.
    /// </summary>
    [Fact]
    public async Task GetCaseHistoryAsync_EmptyHistory_ReturnsEmptyList()
    {
        // Arrange
        var caseId = 1;
        SetupHttpResponse(caseId, "history", HttpStatusCode.OK, new List<CaseFieldHistoryDto>());

        // Act
        var result = await _client.GetCaseHistoryAsync(caseId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Test that GetCaseHistoryAsync returns history entries when they exist.
    /// </summary>
    [Fact]
    public async Task GetCaseHistoryAsync_WithHistory_ReturnsViewModels()
    {
        // Arrange
        var caseId = 1;
        var dtos = new List<CaseFieldHistoryDto>
        {
            new()
            {
                Id = 1,
                CaseId = caseId,
                FieldName = "CrimeDescription",
                OldValue = null,
                NewValue = "\"Test crime description\"",
                ChangedAt = DateTime.UtcNow.AddDays(-1),
                CuratorId = "curator1",
                ChangeConfidence = 85,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = 2,
                CaseId = caseId,
                FieldName = "CrimeTypeId",
                OldValue = "1",
                NewValue = "2",
                ChangedAt = DateTime.UtcNow,
                CuratorId = "curator2",
                ChangeConfidence = 90,
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupHttpResponse(caseId, "history", HttpStatusCode.OK, dtos);

        // Act
        var result = await _client.GetCaseHistoryAsync(caseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        
        // Verify first entry
        var entry1 = result.FirstOrDefault(e => e.FieldName == "CrimeDescription");
        Assert.NotNull(entry1);
        Assert.Equal(85, entry1.ChangeConfidence);
        Assert.Equal("curator1", entry1.CuratorId);
        Assert.Equal("Crime Description", entry1.FieldDisplayName);
        
        // Verify second entry
        var entry2 = result.FirstOrDefault(e => e.FieldName == "CrimeTypeId");
        Assert.NotNull(entry2);
        Assert.Equal(90, entry2.ChangeConfidence);
    }

    /// <summary>
    /// Test that GetCaseHistoryAsync returns empty list when case not found.
    /// </summary>
    [Fact]
    public async Task GetCaseHistoryAsync_CaseNotFound_ReturnsEmptyList()
    {
        // Arrange
        var caseId = 999;
        SetupHttpResponse(caseId, "history", HttpStatusCode.NotFound, (string?)null);

        // Act
        var result = await _client.GetCaseHistoryAsync(caseId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Test that GetCaseFieldHistoryAsync returns filtered history for specific field.
    /// </summary>
    [Fact]
    public async Task GetCaseFieldHistoryAsync_SpecificField_ReturnsFilteredEntries()
    {
        // Arrange
        var caseId = 1;
        var fieldName = "CrimeDescription";
        var dtos = new List<CaseFieldHistoryDto>
        {
            new()
            {
                Id = 1,
                CaseId = caseId,
                FieldName = fieldName,
                OldValue = "\"Old description\"",
                NewValue = "\"New description\"",
                ChangedAt = DateTime.UtcNow,
                CuratorId = "curator1",
                ChangeConfidence = 80,
                CreatedAt = DateTime.UtcNow
            }
        };

        SetupFieldHistoryHttpResponse(caseId, fieldName, HttpStatusCode.OK, dtos);

        // Act
        var result = await _client.GetCaseFieldHistoryAsync(caseId, fieldName);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(fieldName, result[0].FieldName);
    }

    /// <summary>
    /// Test that CaseFieldHistoryViewModel correctly formats confidence levels.
    /// </summary>
    [Fact]
    public void CaseFieldHistoryViewModel_ConfidenceLevels_ReturnCorrectLabels()
    {
        // Test high confidence
        var highConfidence = new CaseFieldHistoryViewModel { ChangeConfidence = 90 };
        Assert.Equal("Alta", highConfidence.ConfidenceLevel);
        Assert.Equal("bg-success", highConfidence.ConfidenceBadgeClass);

        // Test medium confidence
        var mediumConfidence = new CaseFieldHistoryViewModel { ChangeConfidence = 60 };
        Assert.Equal("Média", mediumConfidence.ConfidenceLevel);
        Assert.Equal("bg-warning", mediumConfidence.ConfidenceBadgeClass);

        // Test low confidence
        var lowConfidence = new CaseFieldHistoryViewModel { ChangeConfidence = 30 };
        Assert.Equal("Baixa", lowConfidence.ConfidenceLevel);
        Assert.Equal("bg-danger", lowConfidence.ConfidenceBadgeClass);
    }

    /// <summary>
    /// Test that CaseFieldHistoryViewModel formats field names correctly.
    /// </summary>
    [Fact]
    public void CaseFieldHistoryViewModel_FieldDisplayName_FormatsCorrectly()
    {
        // Test various field name formats
        Assert.Equal("Crime Description", new CaseFieldHistoryViewModel { FieldName = "CrimeDescription" }.FieldDisplayName);
        Assert.Equal("Crime Location City", new CaseFieldHistoryViewModel { FieldName = "CrimeLocationCity" }.FieldDisplayName);
        Assert.Equal("Victim Name", new CaseFieldHistoryViewModel { FieldName = "VictimName" }.FieldDisplayName);
        Assert.Equal("Accused", new CaseFieldHistoryViewModel { FieldName = "Accused" }.FieldDisplayName);
    }

    /// <summary>
    /// Test that CaseFieldHistoryViewModel handles null curator correctly.
    /// </summary>
    [Fact]
    public void CaseFieldHistoryViewModel_NullCurator_DisplaysSistema()
    {
        var viewModel = new CaseFieldHistoryViewModel { CuratorId = null };
        Assert.Equal("Sistema", viewModel.CuratorDisplay);
    }

    /// <summary>
    /// Test that CaseFieldHistoryViewModel handles empty values correctly.
    /// </summary>
    [Fact]
    public void CaseFieldHistoryViewModel_EmptyValues_DisplaysPlaceholder()
    {
        var viewModelNull = new CaseFieldHistoryViewModel { OldValue = null };
        Assert.Equal("(vazio)", viewModelNull.OldValueDisplay);

        var viewModelEmpty = new CaseFieldHistoryViewModel { OldValue = "" };
        Assert.Equal("(vazio)", viewModelEmpty.OldValueDisplay);

        var viewModelWhitespace = new CaseFieldHistoryViewModel { OldValue = "   " };
        Assert.Equal("(vazio)", viewModelWhitespace.OldValueDisplay);
    }

    /// <summary>
    /// Test that FromDtoList correctly converts multiple DTOs.
    /// </summary>
    [Fact]
    public void CaseFieldHistoryViewModel_FromDtoList_ConvertsCorrectly()
    {
        var dtos = new List<CaseFieldHistoryDto>
        {
            new() { Id = 1, CaseId = 1, FieldName = "Field1", ChangeConfidence = 75 },
            new() { Id = 2, CaseId = 1, FieldName = "Field2", ChangeConfidence = 85 }
        };

        var viewModels = CaseFieldHistoryViewModel.FromDtoList(dtos);

        Assert.Equal(2, viewModels.Count);
        Assert.Equal(75, viewModels[0].ChangeConfidence);
        Assert.Equal(85, viewModels[1].ChangeConfidence);
    }

    private void SetupHttpResponse<T>(int caseId, string endpoint, HttpStatusCode statusCode, T? content)
    {
        var response = new HttpResponseMessage(statusCode);
        
        if (content != null && statusCode == HttpStatusCode.OK)
        {
            response.Content = JsonContent.Create(content);
        }

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().Contains($"/api/cases/{caseId}/{endpoint}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupFieldHistoryHttpResponse(int caseId, string fieldName, HttpStatusCode statusCode, List<CaseFieldHistoryDto>? content)
    {
        var response = new HttpResponseMessage(statusCode);
        
        if (content != null && statusCode == HttpStatusCode.OK)
        {
            response.Content = JsonContent.Create(content);
        }
        else if (statusCode == HttpStatusCode.NotFound)
        {
            response.Content = JsonContent.Create(new { });
        }

        var encodedFieldName = Uri.EscapeDataString(fieldName);
        
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().Contains($"/api/cases/{caseId}/history/{encodedFieldName}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
