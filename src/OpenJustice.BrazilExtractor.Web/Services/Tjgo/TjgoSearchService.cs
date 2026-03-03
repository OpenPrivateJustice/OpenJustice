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
        // Font Awesome download icons - more specific for TJGO portal
        "a i.fa-download",
        "a i.fa-file-pdf-o",
        "a i.fa-file-pdf",
        "a:has(i.fa-download)",
        "a:has(i.fa-file-pdf-o)",
        // Generic patterns as fallback
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
        
        var queryStartTime = DateTime.UtcNow;
        
        _logger.LogInformation("Executing TJGO search for date {Date} (Criminal: {Criminal})",
            query.FormattedDate, query.CriminalMode);
        
        _logger.LogInformation("Applying criminal filter profile: {Profile}", 
            query.CriminalFilter.GetDescription());

        IBrowser? browser = null;
        IBrowserContext? context = null;
        IPage? page = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Create browser with headless mode from config
            browser = await _browserFactory.CreateBrowserAsync(_options.HeadlessMode);
            context = await _browserFactory.CreateContextAsync(browser);
            page = await _browserFactory.CreatePageAsync(context);

            using var cancellationRegistration = cancellationToken.Register(() =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (page != null)
                        {
                            await page.CloseAsync();
                        }
                    }
                    catch
                    {
                        // Ignore best-effort cancellation cleanup errors.
                    }

                    try
                    {
                        if (context != null)
                        {
                            await context.CloseAsync();
                        }
                    }
                    catch
                    {
                        // Ignore best-effort cancellation cleanup errors.
                    }

                    try
                    {
                        if (browser != null)
                        {
                            await browser.CloseAsync();
                        }
                    }
                    catch
                    {
                        // Ignore best-effort cancellation cleanup errors.
                    }
                });
            });

            // Navigate to ConsultaPublicacao
            _logger.LogDebug("Navigating to {Url}", _options.ConsultaPublicacaoUrl);
            await page.GotoAsync(_options.ConsultaPublicacaoUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60000
            });
            cancellationToken.ThrowIfCancellationRequested();

            // Apply criminal filter profile query parameters if available
            if (query.CriminalFilter?.Enabled == true && query.CriminalFilter.QueryParameters.Count > 0)
            {
                foreach (var param in query.CriminalFilter.QueryParameters)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger.LogDebug("Applying query parameter: {Key}={Value}", param.Key, param.Value);
                    try
                    {
                        // Support both ID (#id) and name [name='id'] selectors
                        ILocator locator;
                        if (param.Key.StartsWith("#"))
                        {
                            // ID selector
                            locator = page.Locator(param.Key);
                        }
                        else
                        {
                            // Name selector
                            locator = page.Locator($"[name='{param.Key}']");
                        }
                        
                        if (await locator.CountAsync() > 0)
                        {
                            // Check if it's a radio button - use CheckAsync, otherwise use FillAsync
                            var tagName = await locator.First.EvaluateAsync<string>("el => el.type");
                            if (tagName == "radio")
                            {
                                await locator.CheckAsync();
                            }
                            else
                            {
                                await locator.FillAsync(param.Value);
                            }
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
            cancellationToken.ThrowIfCancellationRequested();

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
            cancellationToken.ThrowIfCancellationRequested();

            var resultUrl = page.Url;
            _logger.LogInformation("Form submitted, result URL: {Url}", resultUrl);

            // Check for result state markers - look for common result indicators
            var hasResults = await CheckForResultsAsync(page);
            var recordCount = await TryGetRecordCountAsync(page);

            _logger.LogInformation("Search completed: hasResults={HasResults}, recordCount={Count}",
                hasResults, recordCount);

            // ============================================
            // FULL PAGINATION TRAVERSAL - Task 1
            // ============================================
            // Extract PDF links from all result pages by traversing pagination
            var paginationResult = await ExtractPdfLinksWithPaginationAsync(page, resultUrl, cancellationToken);

            _logger.LogInformation(
                "PDF link extraction (full pagination): {TotalPages} pages traversed, {TotalSeen} total candidates, {Unique} unique links, capped={WasCapped}",
                paginationResult.PagesTraversed,
                paginationResult.TotalSeen,
                paginationResult.Unique.Count,
                paginationResult.WasCapped);

            var queryEndTime = DateTime.UtcNow;
            
            // Create result with aggregated multi-page links and pagination telemetry
            return TjgoSearchResult.SuccessfulWithPdfLinks(
                resultUrl,
                recordCount,
                paginationResult.Unique,
                paginationResult.TotalSeen,
                _options.MaxResultsPerQuery,
                query,
                pageIndex: 0,
                pagesTraversed: paginationResult.PagesTraversed,
                finalPageIndex: paginationResult.FinalPageIndex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("TJGO search cancelled by user request");
            throw;
        }
        catch (PlaywrightException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(ex, "TJGO search interrupted due to cancellation");
            throw new OperationCanceledException("Search cancelled", ex, cancellationToken);
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
            // Clean up browser resources (best effort)
            if (page != null)
            {
                try
                {
                    await page.CloseAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            if (context != null)
            {
                try
                {
                    await context.CloseAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            if (browser != null)
            {
                try
                {
                    await browser.CloseAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    /// <summary>
    /// Extracts PDF links from all result pages by traversing pagination.
    /// Implements full pagination loop with robust next-page detection and cadence enforcement.
    /// </summary>
    private async Task<PaginationHarvestResult> ExtractPdfLinksWithPaginationAsync(
        IPage page, 
        string baseUrl, 
        CancellationToken cancellationToken)
    {
        var allLinks = new List<TjgoPublicationPdfLink>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int totalSeen = 0;
        int pageIndex = 0;
        int pagesTraversed = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pagesTraversed++;
            _logger.LogDebug("Processing result page {CurrentPage} (total traversed: {Total})", 
                pageIndex + 1, pagesTraversed);

            // Extract PDF links from current page
            var pageResult = await ExtractPdfLinksFromCurrentPageAsync(page, baseUrl, pageIndex, seenUrls);
            
            totalSeen += pageResult.NewLinksExtracted;
            allLinks.AddRange(pageResult.Unique);
            
            _logger.LogDebug(
                "Page {Page}: {NewLinks} new links (cumulative unique: {Unique}, total seen: {Total})",
                pageIndex + 1, pageResult.NewLinksExtracted, allLinks.Count, totalSeen);

            // Check if we've reached the max results cap (0 = unlimited)
            if (_options.MaxResultsPerQuery > 0 && allLinks.Count >= _options.MaxResultsPerQuery)
            {
                _logger.LogInformation("Reached max results cap: {Max}. Stopping pagination traversal.", _options.MaxResultsPerQuery);
                break;
            }

            // Try to find and click the next page button
            var nextPageLocator = await FindNextPageLocatorAsync(page);
            
            if (nextPageLocator == null)
            {
                _logger.LogDebug("No next page found, pagination complete after {Pages} pages", pagesTraversed);
                break;
            }

            // Wait QueryIntervalSeconds before each pagination navigation (EXTR-07)
            _logger.LogDebug("Waiting {Interval}s before navigating to next page", _options.QueryIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.QueryIntervalSeconds), cancellationToken);

            // Click the next page button
            try
            {
                await nextPageLocator.ClickAsync();
                
                // Wait for page to load after navigation
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                
                // Small additional wait for dynamic content
                await Task.Delay(1000, cancellationToken);
                
                pageIndex++;
                _logger.LogDebug("Navigated to page {PageIndex}", pageIndex + 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to click next page button, ending pagination");
                break;
            }
        }

        return new PaginationHarvestResult
        {
            Unique = allLinks,
            TotalSeen = totalSeen,
            WasCapped = _options.MaxResultsPerQuery > 0 && allLinks.Count >= _options.MaxResultsPerQuery,
            PagesTraversed = pagesTraversed,
            FinalPageIndex = pageIndex
        };
    }

    /// <summary>
    /// Finds the next page locator using multiple UI patterns.
    /// </summary>
    private async Task<ILocator?> FindNextPageLocatorAsync(IPage page)
    {
        // Multiple selector patterns for next-page detection
        var nextPageSelectors = new[]
        {
            // Common next/forward arrow patterns
            "a[rel='next']",
            "a.next",
            "a.nextPage",
            "a.pagination-next",
            "a[title*='Próxima']",
            "a[title*='próxima']",
            "a[title*='Next']",
            "a[title*='next']",
            // Arrow symbols
            "a:has-text('>')",
            "a:has-text('>>')",
            "a:has-text('»')",
            "a:has-text('›')",
            // Numbered pagination - look for active page's next sibling
            ".pagination a:not(.active)",
            ".paginator a:not(.active)",
            ".pager a:not(.active)",
            // Portuguese labels
            "a:has-text('Próxima')",
            "a:has-text('Próximo')",
            "a:has-text('Avançar')",
            // Generic patterns
            "nav.pagination a",
            ".pagination nav a"
        };

        foreach (var selector in nextPageSelectors)
        {
            try
            {
                var locator = page.Locator(selector);
                var count = await locator.CountAsync();
                
                if (count > 0)
                {
                    // For numbered pagination, find the link after the current page
                    if (selector.Contains(".pagination") || selector.Contains(".paginator") || selector.Contains(".pager"))
                    {
                        // Try to find the next page number link
                        var currentPageLinks = page.Locator(".pagination .active, .paginator .active, .pager .active");
                        if (await currentPageLinks.CountAsync() > 0)
                        {
                            // Get the next sibling link
                            var allLinks = page.Locator(selector);
                            var totalLinks = await allLinks.CountAsync();
                            for (int i = 0; i < totalLinks; i++)
                            {
                                var link = allLinks.Nth(i);
                                var isActive = await link.EvaluateAsync<bool>("el => el.classList.contains('active')");
                                if (!isActive)
                                {
                                    // Make sure this isn't a "previous" link
                                    var text = await link.TextContentAsync();
                                    if (text != null && !text.ToLowerInvariant().Contains("ante"))
                                    {
                                        _logger.LogDebug("Found next page via numbered pagination: selector={Selector}", selector);
                                        return link;
                                    }
                                }
                            }
                        }
                    }

                    // Return the first matching locator
                    _logger.LogDebug("Found next page: selector={Selector}, count={Count}", selector, count);
                    return locator.First;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Selector {Selector} not found or error", selector);
            }
        }

        // If no explicit next button, try to find page number links
        try
        {
            // Look for numbered pagination (page 2, 3, etc.)
            var numberedPattern = page.Locator("a[href*='page'], a[href*='pagina'], .pagination a:not([href*='page'])");
            var count = await numberedPattern.CountAsync();
            
            if (count > 1) // More than 1 means there's a page 2+
            {
                // Find the first non-active numbered link
                for (int i = 0; i < count; i++)
                {
                    var link = numberedPattern.Nth(i);
                    var text = await link.TextContentAsync();
                    if (text != null && int.TryParse(text.Trim(), out int pageNum) && pageNum > 1)
                    {
                        _logger.LogDebug("Found next page via numbered link: {PageNum}", pageNum);
                        return link;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Could not find numbered pagination");
        }

        return null;
    }

    /// <summary>
    /// Extracts PDF links from the current page (single-page extraction without pagination).
    /// </summary>
    private async Task<PdfLinkHarvestResult> ExtractPdfLinksAsync(IPage page, string baseUrl)
    {
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return await ExtractPdfLinksFromCurrentPageAsync(page, baseUrl, 0, seenUrls);
    }

    /// <summary>
    /// Extracts PDF links from the current page, avoiding duplicates from previous pages.
    /// </summary>
    private async Task<PdfLinkHarvestResult> ExtractPdfLinksFromCurrentPageAsync(
        IPage page, 
        string baseUrl, 
        int sourcePageIndex,
        HashSet<string> previouslySeenUrls)
    {
        var links = new List<TjgoPublicationPdfLink>();
        int totalSeen = 0;
        int newLinksExtracted = 0;

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
                            
                            // Skip duplicates by normalized URL (across all pages)
                            if (previouslySeenUrls.Contains(normalizedUrl))
                            {
                                _logger.LogDebug("Skipping duplicate URL: {Url}", normalizedUrl);
                                continue;
                            }

                            previouslySeenUrls.Add(normalizedUrl);
                            newLinksExtracted++;

                            // Try to get display text
                            var displayText = await TryGetDisplayTextAsync(element);

                            links.Add(TjgoPublicationPdfLink.Create(
                                normalizedUrl,
                                href,
                                links.Count,
                                sourcePageIndex,
                                displayText));

                            // Check if we've hit the cap (0 = unlimited)
                            if (_options.MaxResultsPerQuery > 0 && links.Count >= _options.MaxResultsPerQuery)
                            {
                                _logger.LogDebug("Reached max results cap while scanning current page: {Max}", _options.MaxResultsPerQuery);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Error extracting link at index {Index}", i);
                        }
                    }

                    if (_options.MaxResultsPerQuery > 0 && links.Count >= _options.MaxResultsPerQuery)
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

                var maxGenericScan = _options.MaxResultsPerQuery > 0
                    ? Math.Min(genericCount, _options.MaxResultsPerQuery * 3)
                    : genericCount;

                for (int i = 0; i < maxGenericScan; i++)
                {
                    try
                    {
                        var element = genericAnchors.Nth(i);
                        var href = await element.GetAttributeAsync("href");
                        
                        if (string.IsNullOrWhiteSpace(href) || !IsPdfLink(href))
                            continue;

                        totalSeen++;

                        var normalizedUrl = NormalizeUrl(href, baseUrl);
                        
                        if (previouslySeenUrls.Contains(normalizedUrl))
                            continue;

                        previouslySeenUrls.Add(normalizedUrl);
                        newLinksExtracted++;
                        
                        var displayText = await TryGetDisplayTextAsync(element);

                        links.Add(TjgoPublicationPdfLink.Create(
                            normalizedUrl,
                            href,
                            links.Count,
                            sourcePageIndex,
                            displayText));

                        if (_options.MaxResultsPerQuery > 0 && links.Count >= _options.MaxResultsPerQuery)
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
            WasCapped = _options.MaxResultsPerQuery > 0 && links.Count >= _options.MaxResultsPerQuery,
            NewLinksExtracted = newLinksExtracted
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
        
        // Skip external links - only accept tjgo.jus.br main domain or projudi subdomain
        if (hrefLower.StartsWith("http"))
        {
            // Only accept specific allowed domains
            bool isAllowed = hrefLower.Contains("tjgo.jus.br") && 
                           (hrefLower.Contains("projudi") || 
                            hrefLower.Contains("consulta") ||
                            !hrefLower.Contains("docs.") && !hrefLower.Contains("www."));
            
            if (!isAllowed)
            {
                return false;
            }
        }
        
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

    /// <summary>
    /// Number of new links extracted from this page (since last page).
    /// </summary>
    public int NewLinksExtracted { get; set; }
}

/// <summary>
/// Result of full pagination harvesting operation.
/// Aggregates links across all traversed pages.
/// </summary>
internal class PaginationHarvestResult
{
    /// <summary>
    /// Unique PDF links after de-duplication across all pages.
    /// </summary>
    public List<TjgoPublicationPdfLink> Unique { get; set; } = new();

    /// <summary>
    /// Total number of candidates seen (before de-dup) across all pages.
    /// </summary>
    public int TotalSeen { get; set; }

    /// <summary>
    /// Whether the result was capped at MaxResultsPerQuery.
    /// </summary>
    public bool WasCapped { get; set; }

    /// <summary>
    /// Total number of pages traversed during pagination.
    /// </summary>
    public int PagesTraversed { get; set; }

    /// <summary>
    /// The final page index (0-based) that was reached.
    /// </summary>
    public int FinalPageIndex { get; set; }
}
