using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using AtrocidadesRSS.Reader.Configuration;
using AtrocidadesRSS.Reader.Models.Cases;

namespace AtrocidadesRSS.Reader.Services.Cases;

/// <summary>
/// HTTP client implementation for fetching case history from Generator API.
/// Handles authentication via Bearer token and explicit 401 redirect handling.
/// </summary>
public class GeneratorHistoryApiClient : IGeneratorHistoryApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeneratorHistoryApiOptions _options;
    private readonly ILogger<GeneratorHistoryApiClient> _logger;

    /// <summary>
    /// JSON serializer options for parsing history responses.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Creates a new GeneratorHistoryApiClient instance.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory for creating configured HttpClient.</param>
    /// <param name="options">Generator history API configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public GeneratorHistoryApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<GeneratorHistoryApiOptions> options,
        ILogger<GeneratorHistoryApiClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Create HTTP client with base address from configuration
        _httpClient = httpClientFactory.CreateClient(nameof(GeneratorHistoryApiClient));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/'));
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);

        // Always add Authorization header
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);

        _logger.LogInformation(
            "GeneratorHistoryApiClient initialized with BaseUrl: {BaseUrl}",
            _options.BaseUrl);
    }

    /// <inheritdoc/>
    public async Task<List<CaseFieldHistoryViewModel>> GetCaseHistoryAsync(
        int caseId,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/cases/{caseId}/history";
        _logger.LogDebug("Fetching case history from {Url}", url);

        return await ExecuteWithAuthHandlingAsync(url, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<CaseFieldHistoryViewModel>> GetFieldHistoryAsync(
        int caseId,
        string fieldName,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/cases/{caseId}/history/{fieldName}";
        _logger.LogDebug("Fetching field history from {Url}", url);

        return await ExecuteWithAuthHandlingAsync(url, cancellationToken);
    }

    /// <summary>
    /// Executes an HTTP GET request with authentication handling.
    /// </summary>
    private async Task<List<CaseFieldHistoryViewModel>> ExecuteWithAuthHandlingAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("History not found at {Url}", url);
                return new List<CaseFieldHistoryViewModel>();
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Authentication failed for {Url}. Redirecting to login.", url);
                throw new GeneratorHistoryApiUnauthorizedException(_options.LoginUrl);
            }

            response.EnsureSuccessStatusCode();

            // Deserialize the response into Generator DTO format, then map to view model
            var historyDtos = await response.Content.ReadFromJsonAsync<List<CaseFieldHistoryDto>>(
                JsonOptions,
                cancellationToken);

            return MapToViewModels(historyDtos ?? new List<CaseFieldHistoryDto>());
        }
        catch (GeneratorHistoryApiUnauthorizedException)
        {
            // Re-throw authentication errors to be handled by the caller
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching history from {Url}", url);
            throw;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == CancellationToken.None)
        {
            _logger.LogError(ex, "Request timeout fetching history from {Url}", url);
            throw new TimeoutException(
                $"Request to {url} timed out after {_options.RequestTimeoutSeconds} seconds.",
                ex);
        }
    }

    /// <summary>
    /// Maps Generator DTOs to Reader view models.
    /// Preserves ChangedAt, field values, and ChangeConfidence.
    /// </summary>
    private static List<CaseFieldHistoryViewModel> MapToViewModels(
        List<CaseFieldHistoryDto> dtos)
    {
        return dtos.Select(dto => new CaseFieldHistoryViewModel
        {
            Id = dto.Id,
            CaseId = dto.CaseId,
            FieldName = dto.FieldName,
            OldValue = dto.OldValue,
            NewValue = dto.NewValue,
            ChangedAt = dto.ChangedAt,
            CuratorId = dto.CuratorId,
            ChangeReason = dto.ChangeReason,
            ChangeConfidence = dto.ChangeConfidence,
            CreatedAt = dto.CreatedAt
        }).ToList();
    }
}

/// <summary>
/// Data Transfer Object for case field history from Generator API.
/// Mirrors the structure returned by Generator's CaseHistoryController.
/// </summary>
internal class CaseFieldHistoryDto
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? CuratorId { get; set; }
    public string? ChangeReason { get; set; }
    public int ChangeConfidence { get; set; }
    public DateTime CreatedAt { get; set; }
}
