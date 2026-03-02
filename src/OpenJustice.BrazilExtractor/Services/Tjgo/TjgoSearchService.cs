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
        _logger.LogInformation("Executing TJGO search for date {Date} (Criminal: {Criminal})",
            query.FormattedDate, query.CriminalMode);

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

            return TjgoSearchResult.Successful(resultUrl, recordCount, query);
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
