namespace OpenJustice.Reader.Models.Search;

/// <summary>
/// Search query parameters for case lookup.
/// </summary>
public class CaseSearchQuery
{
    /// <summary>
    /// Text to search in accused or victim names (fuzzy matching).
    /// </summary>
    public string? NameText { get; set; }

    /// <summary>
    /// Filter by crime type.
    /// </summary>
    public string? CrimeType { get; set; }

    /// <summary>
    /// Filter by state (location).
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Start of period for crime date filter.
    /// </summary>
    public DateTime? PeriodStart { get; set; }

    /// <summary>
    /// End of period for crime date filter.
    /// </summary>
    public DateTime? PeriodEnd { get; set; }

    /// <summary>
    /// Filter by judicial status.
    /// </summary>
    public string? JudicialStatus { get; set; }

    /// <summary>
    /// Field to sort by.
    /// </summary>
    public CaseSortField SortField { get; set; } = CaseSortField.CrimeDate;

    /// <summary>
    /// Sort direction.
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;

    /// <summary>
    /// Page number (1-indexed).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Creates a copy with updated page (resets to page 1).
    /// </summary>
    public CaseSearchQuery WithResetPage() 
    {
        var copy = Clone();
        copy.Page = 1;
        return copy;
    }

    /// <summary>
    /// Creates a copy with the specified page number.
    /// </summary>
    public CaseSearchQuery WithPage(int page)
    {
        var copy = Clone();
        copy.Page = page;
        return copy;
    }

    /// <summary>
    /// Creates a copy with updated sort field and direction.
    /// </summary>
    public CaseSearchQuery WithSort(CaseSortField field, SortDirection direction)
    {
        var copy = Clone();
        copy.SortField = field;
        copy.SortDirection = direction;
        return copy;
    }

    private CaseSearchQuery Clone() => new()
    {
        NameText = NameText,
        CrimeType = CrimeType,
        State = State,
        PeriodStart = PeriodStart,
        PeriodEnd = PeriodEnd,
        JudicialStatus = JudicialStatus,
        SortField = SortField,
        SortDirection = SortDirection,
        Page = Page,
        PageSize = PageSize
    };
}

/// <summary>
/// Sortable fields for case search results.
/// </summary>
public enum CaseSortField
{
    CrimeDate,
    ReferenceCode,
    VictimName,
    AccusedName,
    ConfidenceScore,
    CreatedAt,
    CrimeType,
    State,
    JudicialStatus
}

/// <summary>
/// Sort direction.
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}
