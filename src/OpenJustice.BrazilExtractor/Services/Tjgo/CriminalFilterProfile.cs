namespace OpenJustice.BrazilExtractor.Services.Tjgo;

/// <summary>
/// Defines a deterministic criminal filter strategy for TJGO searches.
/// This profile specifies how criminal-only filtering is applied when CriminalMode is enabled.
/// </summary>
public class CriminalFilterProfile
{
    /// <summary>
    /// The name of this profile for logging/auditing purposes.
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Whether criminal filtering is enabled for this profile.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional: Query operators or parameters to apply for criminal filtering.
    /// Based on research, TJGO form may support "tipoConsulta" or "ArquivoTipo" filtering.
    /// </summary>
    public Dictionary<string, string> QueryParameters { get; set; } = new();

    /// <summary>
    /// Optional: Text patterns to look for in results that indicate criminal vs civil cases.
    /// </summary>
    public List<string> CriminalIndicators { get; set; } = new()
    {
        "crime hediondo",
        "homicídio",
        "tráfico",
        "estupro",
        "roubo",
        "furto",
        "lesão corporal",
        "ameaça"
    };

    /// <summary>
    /// Optional: Text patterns that indicate civil-only cases to exclude when filtering.
    /// </summary>
    public List<string> CivilOnlyIndicators { get; set; } = new()
    {
        "ação civil",
        "ação de alimentos",
        "ação de divórcio",
        "ação de inventário",
        "ação de execução",
        "ação de cobranças"
    };

    /// <summary>
    /// Creates a default criminal filter profile with standard Brazilian legal indicators.
    /// </summary>
    public static CriminalFilterProfile DefaultCriminal => new()
    {
        Name = "DefaultCriminal",
        Enabled = true,
        QueryParameters = new Dictionary<string, string>
        {
            { "tipoConsulta", "campo" }
        }
    };

    /// <summary>
    /// Creates a profile that does no filtering - returns all results.
    /// </summary>
    public static CriminalFilterProfile NoFilter => new()
    {
        Name = "NoFilter",
        Enabled = false
    };

    /// <summary>
    /// Gets a profile based on the criminal mode setting.
    /// </summary>
    public static CriminalFilterProfile GetProfile(bool criminalMode)
    {
        return criminalMode ? DefaultCriminal : NoFilter;
    }

    /// <summary>
    /// Returns a description of this profile for logging purposes.
    /// </summary>
    public string GetDescription()
    {
        return $"CriminalFilterProfile[{Name}, Enabled={Enabled}]";
    }
}
