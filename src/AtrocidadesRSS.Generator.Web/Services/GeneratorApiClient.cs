using System.Net.Http.Json;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;

namespace AtrocidadesRSS.Generator.Web.Services;

/// <summary>
/// Typed HTTP client for communicating with the Generator API.
/// </summary>
public interface IGeneratorApiClient
{
    /// <summary>
    /// Gets all cases.
    /// </summary>
    Task<List<Case>> GetAllCasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a case by ID.
    /// </summary>
    Task<Case?> GetCaseByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new case.
    /// </summary>
    /// <returns>The created case, or null if validation fails.</returns>
    Task<(Case? Result, string? Error)> CreateCaseAsync(
        Contracts.Cases.CreateCaseRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing case.
    /// </summary>
    /// <returns>The updated case, or null if not found or validation fails.</returns>
    Task<(Case? Result, string? Error)> UpdateCaseAsync(
        int id, 
        Contracts.Cases.UpdateCaseRequest request, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of the Generator API client.
/// </summary>
public class GeneratorApiClient : IGeneratorApiClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "api/cases";

    public GeneratorApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<List<Case>> GetAllCasesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(BaseUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var cases = await response.Content.ReadFromJsonAsync<List<Case>>(cancellationToken: cancellationToken);
        return cases ?? new List<Case>();
    }

    /// <inheritdoc/>
    public async Task<Case?> GetCaseByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl}/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Case>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(Case? Result, string? Error)> CreateCaseAsync(
        Contracts.Cases.CreateCaseRequest request, 
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(BaseUrl, request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken);
            return (null, problemDetails?.Detail ?? "Erro ao criar caso");
        }
        
        var createdCase = await response.Content.ReadFromJsonAsync<Case>(cancellationToken: cancellationToken);
        return (createdCase, null);
    }

    /// <inheritdoc/>
    public async Task<(Case? Result, string? Error)> UpdateCaseAsync(
        int id, 
        Contracts.Cases.UpdateCaseRequest request, 
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"{BaseUrl}/{id}", request, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (null, "Caso não encontrado");
        }
        
        if (!response.IsSuccessStatusCode)
        {
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken);
            return (null, problemDetails?.Detail ?? "Erro ao atualizar caso");
        }
        
        var updatedCase = await response.Content.ReadFromJsonAsync<Case>(cancellationToken: cancellationToken);
        return (updatedCase, null);
    }
}

/// <summary>
/// Problem details returned from API validation errors.
/// </summary>
public class ProblemDetails
{
    public int Status { get; set; }
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public string? Type { get; set; }
}
