using OpenJustice.Reader.Models.Search;
using OpenJustice.Reader.Services.Data;

namespace OpenJustice.Reader.Services.Search;

/// <summary>
/// Implementation of case search with fuzzy matching and filtering.
/// </summary>
public class CaseSearchService : ICaseSearchService
{
    private readonly ILocalCaseStore _caseStore;

    public CaseSearchService(ILocalCaseStore caseStore)
    {
        _caseStore = caseStore;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<LocalCase>> SearchAsync(
        CaseSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        // Convert to store filter and get results
        var filter = new CaseSearchFilter(
            Query: query.NameText,
            CrimeType: query.CrimeType,
            LocationState: query.State,
            JudicialStatus: query.JudicialStatus,
            DateFrom: query.PeriodStart,
            DateTo: query.PeriodEnd,
            Page: query.Page,
            PageSize: query.PageSize
        );

        var result = await _caseStore.SearchCasesAsync(filter, cancellationToken);

        // Apply fuzzy matching for name search if query is provided
        var cases = result.Cases.ToList();
        
        if (!string.IsNullOrWhiteSpace(query.NameText))
        {
            var searchTerm = NormalizeForFuzzy(query.NameText);
            cases = cases
                .Where(c => FuzzyMatch(c.AccusedName, searchTerm) || FuzzyMatch(c.VictimName, searchTerm))
                .ToList();
        }

        // Apply additional filters (AND conditions)
        if (!string.IsNullOrWhiteSpace(query.CrimeType))
        {
            cases = cases
                .Where(c => c.CrimeType?.Equals(query.CrimeType, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.State))
        {
            cases = cases
                .Where(c => c.LocationState?.Equals(query.State, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.JudicialStatus))
        {
            cases = cases
                .Where(c => c.JudicialStatus?.Equals(query.JudicialStatus, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        if (query.PeriodStart.HasValue)
        {
            cases = cases
                .Where(c => c.CrimeDate.HasValue && c.CrimeDate.Value >= query.PeriodStart.Value)
                .ToList();
        }

        if (query.PeriodEnd.HasValue)
        {
            cases = cases
                .Where(c => c.CrimeDate.HasValue && c.CrimeDate.Value <= query.PeriodEnd.Value)
                .ToList();
        }

        // Get total count before sorting for proper pagination
        var totalCount = cases.Count;

        // Apply sorting
        cases = query.SortDirection == SortDirection.Ascending
            ? cases.OrderBy(GetSortSelector(query.SortField)).ToList()
            : cases.OrderByDescending(GetSortSelector(query.SortField)).ToList();

        // Apply pagination
        var skip = (query.Page - 1) * query.PageSize;
        var pagedCases = cases.Skip(skip).Take(query.PageSize).ToList();

        return new PagedResult<LocalCase>
        {
            Items = pagedCases,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetCrimeTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _caseStore.GetCrimeTypesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetJudicialStatusesAsync(CancellationToken cancellationToken = default)
    {
        return await _caseStore.GetJudicialStatusesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetStatesAsync(CancellationToken cancellationToken = default)
    {
        return await _caseStore.GetStatesAsync(cancellationToken);
    }

    /// <summary>
    /// Normalizes text for fuzzy matching.
    /// </summary>
    private static string NormalizeForFuzzy(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text
            .ToLowerInvariant()
            .Trim()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Replace("á", "a")
            .Replace("à", "a")
            .Replace("ã", "a")
            .Replace("â", "a")
            .Replace("é", "e")
            .Replace("è", "e")
            .Replace("ê", "e")
            .Replace("í", "i")
            .Replace("ì", "i")
            .Replace("î", "i")
            .Replace("ó", "o")
            .Replace("ò", "o")
            .Replace("õ", "o")
            .Replace("ô", "o")
            .Replace("ú", "u")
            .Replace("ù", "u")
            .Replace("û", "u")
            .Replace("ç", "c");
    }

    /// <summary>
    /// Performs fuzzy matching between search term and target string.
    /// Uses contains, starts-with, and Levenshtein distance for matching.
    /// </summary>
    private static bool FuzzyMatch(string? target, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(searchTerm))
            return false;

        var normalizedTarget = NormalizeForFuzzy(target);

        // Exact contains
        if (normalizedTarget.Contains(searchTerm))
            return true;

        // Starts with (for first name searches)
        if (normalizedTarget.StartsWith(searchTerm))
            return true;

        // Word starts with (matches first letters of each word)
        var words = normalizedTarget.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(w => w.StartsWith(searchTerm)))
            return true;

        // Levenshtein distance for typos (allow distance of 1-2 characters)
        if (searchTerm.Length >= 3)
        {
            var distance = LevenshteinDistance(normalizedTarget, searchTerm);
            // Allow up to 2 character difference, but scale with length
            var maxDistance = Math.Max(2, searchTerm.Length / 4);
            if (distance <= maxDistance)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
            return string.IsNullOrEmpty(target) ? 0 : target.Length;

        if (string.IsNullOrEmpty(target))
            return source.Length;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        var distance = new int[sourceLength + 1, targetLength + 1];

        for (var i = 0; i <= sourceLength; i++)
            distance[i, 0] = i;

        for (var j = 0; j <= targetLength; j++)
            distance[0, j] = j;

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[sourceLength, targetLength];
    }

    /// <summary>
    /// Gets the sort selector function for a sort field.
    /// </summary>
    private static Func<LocalCase, IComparable> GetSortSelector(CaseSortField field)
    {
        return field switch
        {
            CaseSortField.ReferenceCode => c => c.ReferenceCode ?? "",
            CaseSortField.VictimName => c => c.VictimName ?? "",
            CaseSortField.AccusedName => c => c.AccusedName ?? "",
            CaseSortField.ConfidenceScore => c => c.ConfidenceScore,
            CaseSortField.CreatedAt => c => c.CreatedAt,
            CaseSortField.CrimeDate => c => c.CrimeDate ?? DateTime.MinValue,
            _ => c => c.CrimeDate ?? DateTime.MinValue
        };
    }
}
