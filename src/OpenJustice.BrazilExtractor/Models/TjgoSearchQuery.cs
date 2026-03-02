namespace OpenJustice.BrazilExtractor.Models;

/// <summary>
/// Represents a TJGO search query with single-day date semantics.
/// </summary>
public class TjgoSearchQuery
{
    /// <summary>
    /// The date for single-day queries. Both DataInicial and DataFinal will be set to this value.
    /// </summary>
    public DateTime QueryDate { get; set; }

    /// <summary>
    /// Whether to search for criminal cases (true) or civil cases (false).
    /// </summary>
    public bool CriminalMode { get; set; }

    /// <summary>
    /// Creates a query for a specific single day.
    /// </summary>
    /// <param name="queryDate">The date to search for.</param>
    /// <param name="criminalMode">Whether to search criminal cases.</param>
    public static TjgoSearchQuery ForSingleDay(DateTime queryDate, bool criminalMode = true)
    {
        return new TjgoSearchQuery
        {
            QueryDate = queryDate.Date,
            CriminalMode = criminalMode
        };
    }

    /// <summary>
    /// Gets the formatted date string for TJGO portal (dd/MM/yyyy).
    /// </summary>
    public string FormattedDate => QueryDate.ToString("dd/MM/yyyy");
}
