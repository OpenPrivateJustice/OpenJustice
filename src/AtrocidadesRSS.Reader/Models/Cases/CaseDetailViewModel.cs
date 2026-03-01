namespace AtrocidadesRSS.Reader.Models.Cases;

/// <summary>
/// View model for displaying full case details.
/// Includes all fields, sources, evidence, judicial information, and metadata.
/// </summary>
public class CaseDetailViewModel
{
    // Core case information
    public int Id { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public DateTime? CrimeDate { get; set; }
    public string? CrimeType { get; set; }
    public string? CaseType { get; set; }
    public string? VictimName { get; set; }
    public string? AccusedName { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationState { get; set; }
    public string? JudicialStatus { get; set; }
    public string? Description { get; set; }
    public int ConfidenceScore { get; set; }
    public bool IsVerified { get; set; }
    public bool IsSensitiveContent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Sources associated with this case
    public IReadOnlyList<CaseSourceViewModel> Sources { get; set; } = Array.Empty<CaseSourceViewModel>();

    // Evidence associated with this case
    public IReadOnlyList<CaseEvidenceViewModel> Evidence { get; set; } = Array.Empty<CaseEvidenceViewModel>();

    // Judicial information
    public JudicialInfoViewModel? JudicialInfo { get; set; }

    // Tags
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    // Metadata
    public CaseMetadataViewModel? Metadata { get; set; }
}

/// <summary>
/// Source information for a case.
/// </summary>
public class CaseSourceViewModel
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string? Url { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public int ConfidenceScore { get; set; }
}

/// <summary>
/// Evidence item for a case.
/// </summary>
public class CaseEvidenceViewModel
{
    public int Id { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? Url { get; set; }
    public DateTime? EvidenceDate { get; set; }
    public string? SubmittedBy { get; set; }
    public int ConfidenceScore { get; set; }
}

/// <summary>
/// Judicial process information.
/// </summary>
public class JudicialInfoViewModel
{
    public string? ProcessNumber { get; set; }
    public string? Court { get; set; }
    public string? Judge { get; set; }
    public DateTime? FilingDate { get; set; }
    public DateTime? JudgmentDate { get; set; }
    public string? Outcome { get; set; }
    public string? Sentence { get; set; }
    public decimal? PrisonSentenceYears { get; set; }
    public decimal? FineAmount { get; set; }
    public string? AppealOutcome { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Case metadata including registration and verification info.
/// </summary>
public class CaseMetadataViewModel
{
    public DateTime RegisteredAt { get; set; }
    public string? RegisteredBy { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public string? LastVerifiedBy { get; set; }
    public int Version { get; set; }
    public string? DataQualityNotes { get; set; }
    public IReadOnlyList<string> RelatedCaseCodes { get; set; } = Array.Empty<string>();
}
