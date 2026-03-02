using System.IO;
using Microsoft.Extensions.Options;

namespace OpenJustice.BrazilExtractor.Configuration;

/// <summary>
/// Validates BrazilExtractor configuration options at startup.
/// </summary>
public class BrazilExtractorOptionsValidator : IValidateOptions<BrazilExtractorOptions>
{
    public ValidateOptionsResult Validate(string? name, BrazilExtractorOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.TjgoUrl))
        {
            errors.Add("BrazilExtractor:TjgoUrl is required.");
        }
        else if (!Uri.TryCreate(options.TjgoUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("BrazilExtractor:TjgoUrl must be a valid HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(options.DownloadPath))
        {
            errors.Add("BrazilExtractor:DownloadPath is required.");
        }

        // DateWindowDays is deprecated - no longer validated
        // The extractor now performs single-day queries only via QueryDateWindowStartDate

        if (options.MaxResultsPerQuery < 1)
        {
            errors.Add("BrazilExtractor:MaxResultsPerQuery must be at least 1.");
        }

        if (options.QueryIntervalSeconds < 1)
        {
            errors.Add("BrazilExtractor:QueryIntervalSeconds must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(options.Profile))
        {
            errors.Add("BrazilExtractor:Profile is required.");
        }

        // OCR configuration validation
        if (string.IsNullOrWhiteSpace(options.OcrOutputPath))
        {
            errors.Add("BrazilExtractor:OcrOutputPath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.TessdataPath))
        {
            errors.Add("BrazilExtractor:TessdataPath is required.");
        }

        if (string.IsNullOrWhiteSpace(options.OcrLanguage))
        {
            errors.Add("BrazilExtractor:OcrLanguage is required.");
        }
        else
        {
            // Validate supported languages (Tesseract 5 supports: por, eng, spa, etc.)
            var supportedLanguages = new[] { "por", "eng", "spa", "fra", "deu", "ita" };
            if (!supportedLanguages.Contains(options.OcrLanguage.ToLowerInvariant()))
            {
                errors.Add($"BrazilExtractor:OcrLanguage '{options.OcrLanguage}' is not in the supported list: {string.Join(", ", supportedLanguages)}.");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.TesseractExecutablePath))
        {
            if (!File.Exists(options.TesseractExecutablePath))
            {
                errors.Add($"BrazilExtractor:TesseractExecutablePath '{options.TesseractExecutablePath}' does not exist.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
