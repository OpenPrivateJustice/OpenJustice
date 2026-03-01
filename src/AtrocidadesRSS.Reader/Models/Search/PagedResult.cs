namespace AtrocidadesRSS.Reader.Models.Search;

/// <summary>
/// Generic paged result wrapper.
/// </summary>
public record PagedResult<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Total number of items matching the query.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Current page number (1-indexed).
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 20) => new()
    {
        Items = Array.Empty<T>(),
        TotalCount = 0,
        Page = page,
        PageSize = pageSize
    };
}
