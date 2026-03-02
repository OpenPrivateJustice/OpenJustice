using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Models;
using OpenJustice.BrazilExtractor.Services.Browser;

namespace OpenJustice.BrazilExtractor.Services.Tjgo;

/// <summary>
/// Service for executing TJGO ConsultaPublicacao search operations.
/// Implements navigation, form fill, submit, and post-submit assertions.
/// </summary>
public class TjgoSearchService : ITjgoSearchService
{
    private readonly IPlaywrightBrowserFactory _browserFactory;
    private readonly BrazilExtractorOptions _options;
    private readonly ILogger<TjgoSearchService> _logger;

    // Patterns that indicate a PDF link in TJGO portal
    private static readonly string[] PdfLinkPatterns = new[]
    {
        ".pdf",
        "/download",
        "downloadPdf",
        "downloadArquivo",
        "exibirPdf",
        "visualizarPdf"
    };

    // Known PDF link CSS selectors from TJGO portal
    private static readonly string[] PdfLinkSelectors = new[]
    {
        "a[href*='.pdf']",
        "a[href*='/download']",
        "a[href*='downloadPdf']",
        "a[href*='downloadArquivo']",
        "a[href*='exibirPdf']",
        "a[href*='visualizarPdf']",
        "a.download",
        "a[download]",
        "a[class*='pdf']",
        "a[class*='download']",
        "a[title*='PDF']",
        "a[title*='pdf']"
    };

    public TjgoSearchService(
        IPlaywrightBrowserFactory browserFactory,
        IOptions<BrazilExtractorOptions> options,
        ILogger<TjgoSearchService> logger)
    {
        _browserFactory = browserFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TjgoSearchResult> ExecuteSearchAsync(TjgoSearchQuery query, CancellationToken cancellationToken = default)
    {
        // Ensure criminal filter profile is set
        query.CriminalFilter ??= CriminalFilterProfile.GetProfile(query.CriminalMode);
        
        _logger.LogInformation("Executing TJGO search for date {Date} (Criminal: {Criminal})",
            query.FormattedDate, query.CriminalMode);
        
        _logger.LogInformation("Applying criminal filter profile: {Profile}", 
            query.CriminalFilter.GetDescription());

        IBrowser? browser = null;
        IBrowserContext? context = null;

        try
        {
            // Create browser with headless mode from config
            browser = await _browserFactory.CreateBrowserAsync(_options.HeadlessMode);
            context = await _browserFactory.CreateContextAsync(browser);

            var page = await _browserFactory.CreatePageAsync(context);

            // Navigate to ConsultaPublicacao
            _logger.LogDebug("Navigating to {Url}", _options.ConsultaPublicacaoUrl);
            await page.GotoAsync(_options.ConsultaPublicacaoUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            // Apply criminal filter profile query parameters if available
            if (query.CriminalFilter?.Enabled == true && query.CriminalFilter.QueryParameters.Count > 0)
            {
                foreach (var param in query.CriminalFilter.QueryParameters)
                {
                    _logger.LogDebug("Applying query parameter: {Key}={Value}", param.Key, param.Value);
                    try
                    {
                        // Try to find and set query parameters on the form
                        var locator = page.Locator($"[name='{param.Key}']");
                        if (await locator.CountAsync() > 0)
                        {
                            await locator.FillAsync(param.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not apply query parameter {Key}", param.Key);
                    }
                }
            }

            // Fill date fields with single-day semantics
            // Both DataInicial and DataFinal are set to the same value for single-day queries
            _logger.LogDebug("Filling date fields: DataInicial={Date}, DataFinal={Date}", 
                query.FormattedDate, query.FormattedDate);

            await page.Locator("#DataInicial").FillAsync(query.FormattedDate);
            await page.Locator("#DataFinal").FillAsync(query.FormattedDate);

            // Verify date values were set correctly
            var dataInicialValue = await page.Locator("#DataInicial").InputValueAsync();
            var dataFinalValue = await page.Locator("#DataFinal").InputValueAsync();

            _logger.LogDebug("Verified date fields: DataInicial={Value}, DataFinal={Value}",
                dataInicialValue, dataFinalValue);

            if (dataInicialValue != query.FormattedDate || dataFinalValue != query.FormattedDate)
            {
                _logger.LogWarning("Date field verification failed: expected {Expected}, got {ActualInicial}/{ActualFinal}",
                    query.FormattedDate, dataInicialValue, dataFinalValue);
            }

            // Wait for form to be ready and submit
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            
            // Submit the form
            _logger.LogDebug("Submitting form");
            await page.Locator("#formLocalizarBotao").ClickAsync();

            // Wait for navigation after submit
            await page.WaitForURLAsync("**/*", new PageWaitForURLOptions
            {
                Timeout = 30000
            });

            var resultUrl = page.Url;
            _logger.LogInformation("Form submitted, result URL: {Url}", resultUrl);

            // Check for result state markers - look for common result indicators
            var hasResults = await CheckForResultsAsync(page);
            var recordCount = await TryGetRecordCountAsync(page);

            _logger.LogInformation("Search completed: hasResults={HasResults}, recordCount={Count}",
                hasResults, recordCount);

            // Extract PDF links from result page
            var pdfLinks = await ExtractPdfLinksAsync(page, resultUrl);

            _logger.LogInformation("PDF link extraction: {Count} total candidates, {Unique} unique after dedup, capped={WasCapped}",
                pdfLinks.TotalSeen, pdfLinks.Unique.Count, pdfLinks.WasCapped);

            return TjgoSearchResult.SuccessfulWithPdfLinks(
                resultUrl,
                recordCount,
                pdfLinks.Unique,
                pdfLinks.TotalSeen,
                _options.MaxResultsPerQuery,
                query);
        }
        catch (PlaywrightException ex)
        {
            _logger.LogError(ex, "Playwright error during TJGO search");
            return TjgoSearchResult.Failed($"Playwright error: {ex.Message}", query);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(ex, "Timeout during TJGO search");
            return TjgoSearchResult.Failed("Search timeout", query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during TJGO search");
            return TjgoSearchResult.Failed($"Error: {ex.Message}", query);
        }
        finally
        {
            // Clean up browser resources
            if (context != null)
            {
                await context.CloseAsync();
            }
            if (browser != null)
            {
                await browser.CloseAsync();
            }
        }
    }

    /// <summary>
    /// Extracts PDF publication links from the result page.
    /// </summary>
    private async Task<PdfLinkHarvestResult> ExtractPdfLinksAsync(IPage page, string baseUrl)
    {
        var links = new List<TjgoPublicationPdfLink>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalSeen = 0;
        int sourcePageIndex = 0;

        try
        {
            // Try each selector pattern to find PDF links
            foreach (var selector in PdfLinkSelectors)
            {
                var anchorElements = page.Locator(selector);
                var count = await anchorElements.CountAsync();

                if (count > 0)
                {
                    _logger.LogDebug("Selector {Selector} found {Count} elements", selector, count);

                    for (int i = 0; i < count; i++)
                    {
                        var element = anchorElements.Nth(i);
                        
                        try
                        {
                            var href = await element.GetAttributeAsync("href");
                            if (string.IsNullOrWhiteSpace(href))
                                continue;

                            // Skip non-PDF links (double-check pattern)
                            if (!IsPdfLink(href))
                                continue;

                            totalSeen++;

                            // Normalize URL to absolute
                            var normalizedUrl = NormalizeUrl(href, baseUrl);
                            
                            // Skip duplicates by normalized URL
                            if (seenUrls.Contains(normalizedUrl))
                            {
                                _logger.LogDebug("Skipping duplicate URL: {Url}", normalizedUrl);
                                continue;
                            }

                            seenUrls.Add(normalizedUrl);

                            // Try to get display text
                            var displayText = await TryGetDisplayTextAsync(element);

                            links.Add(TjgoPublicationPdfLink.Create(
                                normalizedUrl,
                                href,
                                links.Count,
                                sourcePageIndex,
                                displayText));

                            // Check if we've hit the cap
                            if (links.Count >= _options.MaxResultsPerQuery)
                            {
                                _logger.LogDebug("Reached max results cap: {Max}", _options.MaxResultsPerQuery);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error extracting link at index {Index}", i);
                        }
                    }

                    if (links.Count >= _options.MaxResultsPerQuery)
                        break;
                }
            }

            // If no specific selectors worked, try a broader approach
            if (links.Count == 0)
            {
                _logger.LogDebug("No PDF links found with specific selectors, trying generic anchor search");
                var genericAnchors = page.Locator("a[href]");
                var genericCount = await genericAnchors.CountAsync();
                
                _logger.LogDebug("Found {Count} total anchor elements on result page", genericCount);

                for (int i = 0; i < Math.Min(genericCount, _options.MaxResultsPerQuery * 3); i++)
                {
                    try
                    {
                        var element = genericAnchors.Nth(i);
                        var href = await element.GetAttributeAsync("href");
                        
                        if (string.IsNullOrWhiteSpace(href) || !IsPdfLink(href))
                            continue;

                        totalSeen++;

                        var normalizedUrl = NormalizeUrl(href, baseUrl);
                        
                        if (seenUrls.Contains(normalizedUrl))
                            continue;

                        seenUrls.Add(normalizedUrl);
                        var displayText = await TryGetDisplayTextAsync(element);

                        links.Add(TjgoPublicationPdfLink.Create(
                            normalizedUrl,
                            href,
                            links.Count,
                            sourcePageIndex,
                            displayText));

                        if (links.Count >= _options.MaxResultsPerQuery)
                            break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error in generic anchor scan at index {Index}", i);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PDF link extraction");
        }

        return new PdfLinkHarvestResult
        {
            Unique = links,
            TotalSeen = totalSeen,
            WasCapped = links.Count >= _options.MaxResultsPerQuery
        };
    }

    /// <summary>
    /// Checks if the href appears to be a PDF link based on patterns.
    /// </summary>
    private static bool IsPdfLink(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return false;

        var hrefLower = href.ToLowerInvariant();
        
        foreach (var pattern in PdfLinkPatterns)
        {
            if (hrefLower.Contains(pattern.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Normalizes a relative URL to absolute using the base URL.
    /// </summary>
    private static string NormalizeUrl(string href, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(href))
            return string.Empty;

        try
        {
            // Already absolute
            if (Uri.TryCreate(href, UriKind.Absolute, out var absoluteUri))
                return absoluteUri.ToString();

            // Try to resolve relative URL
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                var resolved = new Uri(baseUri, href);
                return resolved.ToString();
            }
        }
        catch
        {
            // Return original if normalization fails
        }

        return href;
    }

    /// <summary>
    /// Attempts to extract display text from an anchor element.
    /// </summary>
    private static async Task<string?> TryGetDisplayTextAsync(ILocator element)
    {
        try
        {
            // Try direct text content
            var text = await element.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                return text.Trim();

            // Try title attribute
            var title = await element.GetAttributeAsync("title");
            if (!string.IsNullOrWhiteSpace(title))
                return title.Trim();

            // Try aria-label
            var ariaLabel = await element.GetAttributeAsync("aria-label");
            if (!string.IsNullOrWhiteSpace(ariaLabel))
                return ariaLabel.Trim();
        }
        catch
        {
            // Ignore extraction errors
        }

        return null;
    }

    private async Task<bool> CheckForResultsAsync(IPage page)
    {
        // Check for common result indicators on the page
        var resultSelectors = new[]
        {
            ".resultado",
            "#resultado",
            ".listagem",
            "#listagem",
            "table.resultado",
            ".tbody tr"
        };

        foreach (var selector in resultSelectors)
        {
            var count = await page.Locator(selector).CountAsync();
            if (count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<int> TryGetRecordCountAsync(IPage page)
    {
        try
        {
            // Try to find record count in common locations
            var countSelectors = new[]
            {
                ".totalRegistros",
                "#totalRegistros",
                ".resultado .header .total",
                "[class*='quantidade']"
            };

            foreach (var selector in countSelectors)
            {
                var element = page.Locator(selector).First;
                if (await element.CountAsync() > 0)
                {
                    var text = await element.TextContentAsync();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // Try to extract number from text
                        var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
                        if (match.Success && int.TryParse(match.Value, out int count))
                        {
                            return count;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract record count");
        }

        return 0;
    }
}

/// <summary>
/// Result of PDF link harvesting operation.
/// </summary>
internal class PdfLinkHarvestResult
{
    /// <summary>
    /// Unique PDF links after de-duplication.
    /// </summary>
    public List<TjgoPublicationPdfLink> Unique { get; set; } = new();

    /// <summary>
    /// Total number of candidates seen (before de-dup).
    /// </summary>
    public int TotalSeen { get; set; }

    /// <summary>
    /// Whether the result was capped at MaxResultsPerQuery.
    /// </summary>
    public bool WasCapped { get; set; }
}
