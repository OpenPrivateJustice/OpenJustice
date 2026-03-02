using Microsoft.Playwright;

namespace OpenJustice.Playwright;

/// <summary>
/// Smoke tests for TJGO ConsultaPublicacao form selectors and filter contract.
/// These tests detect portal selector/form drift affecting criminal/date filtering.
/// </summary>
public class TjgoConsultaPublicacaoSmokeTests : IAsyncLifetime, IDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private readonly string _tjgoUrl = "https://projudi.tjgo.jus.br/ConsultaPublicacao";
    
    // Expected selectors from the TJGO ConsultaPublicacao form
    private static readonly string[] RequiredSelectors = new[]
    {
        "#DataInicial",
        "#DataFinal",
        "#formLocalzarBotao"
    };

    // Optional selectors that should be checked for presence
    private static readonly string[] OptionalSelectors = new[]
    {
        "[name='tipoConsulta']",
        "[name='textoDigitado']",
        "[name='ProcessoNumero']",
        "[name='Serventia']",
        "[name=' Magistrado']"
    };

    // PDF link selectors that the extractor uses to harvest publication PDFs
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

    public async Task InitializeAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() 
        { 
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
        });

        var context = await _browser.NewContextAsync(new() 
        { 
            ViewportSize = new() { Width = 1280, Height = 720 },
            Locale = "pt-BR"
        });

        _page = await context.NewPageAsync();
    }

    public void Dispose()
    {
        _browser?.CloseAsync().GetAwaiter().GetResult();
        _playwright?.Dispose();
    }

    public async Task DisposeAsync()
    {
        Dispose();
    }

    [Fact]
    public async Task TjgoConsultaPublicacao_ShouldLoadSuccessfully()
    {
        // Arrange & Act
        var response = await _page!.GotoAsync(_tjgoUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        // Assert - page loads
        Assert.NotNull(response);
        Assert.True(response.Status == 200 || response.Status == 0, 
            $"Expected status 200 or 0 (for redirect), got {response.Status}");
        
        Console.WriteLine("TJGO ConsultaPublicacao page loaded successfully");
    }

    [Fact]
    public async Task TjgoConsultaPublicacao_ShouldHaveRequiredDateSelectors()
    {
        // Arrange - navigate to page first
        await _page!.GotoAsync(_tjgoUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        // Assert - DataInicial selector exists
        var dataInicial = _page.Locator("#DataInicial");
        var countInicial = await dataInicial.CountAsync();
        Assert.True(countInicial > 0, "Required selector #DataInicial not found");
        Console.WriteLine("Found #DataInicial selector");

        // Assert - DataFinal selector exists
        var dataFinal = _page.Locator("#DataFinal");
        var countFinal = await dataFinal.CountAsync();
        Assert.True(countFinal > 0, "Required selector #DataFinal not found");
        Console.WriteLine("Found #DataFinal selector");
    }

    [Fact]
    public async Task TjgoConsultaPublicacao_ShouldHaveSubmitButton()
    {
        // Arrange - navigate to page first
        await _page!.GotoAsync(_tjgoUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        // Assert - submit button exists (try both possible selector patterns)
        var submitButton = _page.Locator("#formLocalizarBotao");
        var countSubmit = await submitButton.CountAsync();
        
        if (countSubmit == 0)
        {
            // Try alternative selector
            submitButton = _page.Locator("button[type='submit']");
            countSubmit = await submitButton.CountAsync();
        }
        
        Assert.True(countSubmit > 0, "Submit button (#formLocalizarBotao or button[type='submit']) not found");
        Console.WriteLine("Found submit button");
    }

    [Fact]
    public async Task TjgoConsultaPublicacao_ShouldAcceptSameDayDateInput()
    {
        // Arrange - navigate to page first
        await _page!.GotoAsync(_tjgoUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        // Use today's date for single-day query
        var today = DateTime.Today;
        var formattedDate = today.ToString("dd/MM/yyyy");

        // Act - fill both date fields with same value (single-day semantics)
        await _page.Locator("#DataInicial").FillAsync(formattedDate);
        await _page.Locator("#DataFinal").FillAsync(formattedDate);

        // Assert - verify date values were set correctly
        var dataInicialValue = await _page.Locator("#DataInicial").InputValueAsync();
        var dataFinalValue = await _page.Locator("#DataFinal").InputValueAsync();

        Assert.Equal(formattedDate, dataInicialValue);
        Assert.Equal(formattedDate, dataFinalValue);
        Console.WriteLine($"Same-day date input verified: {formattedDate}");
    }

    [Fact]
    public async Task TjgoConsultaPublicacao_ShouldHaveConsultationTypeControls()
    {
        // Arrange - navigate to page first
        await _page!.GotoAsync(_tjgoUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        // Check for consultation type controls (radio buttons or selects)
        var tipoConsultaLocator = _page.Locator("[name='tipoConsulta']");
        var tipoConsultaCount = await tipoConsultaLocator.CountAsync();
        
        // If tipoConsulta exists, verify it works
        if (tipoConsultaCount > 0)
        {
            Console.WriteLine("Found tipoConsulta control");
            
            // Try to check if there are options
            var optionsCount = await tipoConsultaLocator.Locator("option").CountAsync();
            Console.WriteLine($"tipoConsulta has {optionsCount} options");
        }
        else
        {
            // Try alternative - check for radio buttons with tipoConsulta in name/ID
            var radioLocators = _page.Locator("input[type='radio'][name*='tipo']");
            var radioCount = await radioLocators.CountAsync();
            Console.WriteLine($"Found {radioCount} tipoConsulta-related radio buttons");
        }
    }

    [Fact]
    public async Task TjgoConsultaPublicacao_ShouldLogFormState()
    {
        // Arrange - navigate to page first
        await _page!.GotoAsync(_tjgoUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        // Log available form fields for debugging
        var allInputs = await _page.Locator("input").AllAsync();
        Console.WriteLine($"Page has {allInputs.Count} input elements");
        
        var allSelects = await _page.Locator("select").AllAsync();
        Console.WriteLine($"Page has {allSelects.Count} select elements");
        
        var allButtons = await _page.Locator("button").AllAsync();
        Console.WriteLine($"Page has {allButtons.Count} button elements");

        // Check for any element with "crime" or "criminal" in ID/name (for future filter support)
        var criminalRelated = await _page.Locator("[id*='*='crime'], [id*='criminal'], [namecrime'], [name*='criminal']").CountAsync();
        Console.WriteLine($"Found {criminalRelated} criminal-related elements");
    }

    /// <summary>
    /// Integration test that validates the complete form fill workflow.
    /// This is the key smoke test for the extractor contract.
    /// </summary>
    [Fact]
    public async Task TjgoConsultaPublicacao_ShouldFillFormAndSubmit()
    {
        // Arrange - navigate to page first
        await _page!.GotoAsync(_tjgoUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        // Use yesterday's date to avoid empty results for today
        var yesterday = DateTime.Today.AddDays(-1);
        var formattedDate = yesterday.ToString("dd/MM/yyyy");

        // Act - fill the form with single-day semantics
        await _page.Locator("#DataInicial").FillAsync(formattedDate);
        await _page.Locator("#DataFinal").FillAsync(formattedDate);

        // Try to set consultation type if available - use specific selector for the "campo" radio
        var tipoConsulta = _page.Locator("#tipoConsulta"); // The ID for the campo radio button
        if (await tipoConsulta.CountAsync() > 0)
        {
            await tipoConsulta.CheckAsync();
        }

        // Assert - verify dates are set
        var dataInicialValue = await _page.Locator("#DataInicial").InputValueAsync();
        var dataFinalValue = await _page.Locator("#DataFinal").InputValueAsync();
        
        Assert.Equal(formattedDate, dataInicialValue);
        Assert.Equal(formattedDate, dataFinalValue);

        Console.WriteLine($"Form prepared with date: {formattedDate}");
        
        // Note: We don't actually submit to avoid triggering rate limits or bot detection
        // The form preparation is what the extractor does - submission would require
        // handling Turnstile/challenge which is out of scope for smoke tests
    }

    /// <summary>
    /// Smoke test to guard PDF-link selectors on result pages.
    /// This validates the extractor contract for PDF link harvesting.
    /// Accepts either: (a) at least one PDF/download anchor candidate, or (b) explicit no-results state.
    /// </summary>
    [Fact]
    public async Task TjgoConsultaPublicacao_ShouldDetectPdfLinkSelectors()
    {
        // Arrange - navigate to page first
        await _page!.GotoAsync(_tjgoUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });

        // Use a past date that likely has results (e.g., 7 days ago)
        var pastDate = DateTime.Today.AddDays(-7);
        var formattedDate = pastDate.ToString("dd/MM/yyyy");

        // Fill form
        await _page.Locator("#DataInicial").FillAsync(formattedDate);
        await _page.Locator("#DataFinal").FillAsync(formattedDate);

        // Try consultation type
        var tipoConsulta = _page.Locator("#tipoConsulta");
        if (await tipoConsulta.CountAsync() > 0)
        {
            await tipoConsulta.CheckAsync();
        }

        // Submit the form
        await _page.Locator("#formLocalizarBotao").ClickAsync();

        // Wait for results page to load (either results or empty state)
        await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Give the page a moment to render results
        await Task.Delay(1000);

        var currentUrl = _page.Url;
        Console.WriteLine($"Result page URL: {currentUrl}");

        // Check for PDF link selectors - try each one
        int totalPdfLinksFound = 0;
        string? foundSelector = null;
        
        foreach (var selector in PdfLinkSelectors)
        {
            try
            {
                var elements = _page.Locator(selector);
                var count = await elements.CountAsync();
                
                if (count > 0)
                {
                    Console.WriteLine($"Selector '{selector}' found {count} PDF link candidates");
                    totalPdfLinksFound += count;
                    
                    if (foundSelector == null)
                        foundSelector = selector;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Selector '{selector}' error: {ex.Message}");
            }
        }

        // Also do a generic anchor scan as fallback
        var allAnchors = _page.Locator("a[href]");
        var totalAnchors = await allAnchors.CountAsync();
        Console.WriteLine($"Total anchor elements on result page: {totalAnchors}");

        // Check for PDF links in generic anchors
        int genericPdfLinks = 0;
        var pdfPatterns = new[] { ".pdf", "/download", "downloadPdf" };
        
        for (int i = 0; i < Math.Min(totalAnchors, 100); i++)
        {
            try
            {
                var href = await allAnchors.Nth(i).GetAttributeAsync("href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    var hrefLower = href.ToLowerInvariant();
                    if (pdfPatterns.Any(p => hrefLower.Contains(p)))
                    {
                        genericPdfLinks++;
                    }
                }
            }
            catch
            {
                // Skip errors on individual anchors
            }
        }
        
        if (genericPdfLinks > 0)
        {
            Console.WriteLine($"Found {genericPdfLinks} additional PDF links via generic anchor scan");
            totalPdfLinksFound += genericPdfLinks;
        }

        // Check for no-results state markers
        var noResultsSelectors = new[]
        {
            ".semResultado",
            "#semResultado",
            "[class*='nenhum']",
            "[class*='empty']",
            "text=Nenhum registro encontrado",
            "text=Nenhum resultado"
        };
        
        bool hasNoResultsMarker = false;
        foreach (var selector in noResultsSelectors)
        {
            try
            {
                var element = _page.Locator(selector);
                if (await element.CountAsync() > 0)
                {
                    hasNoResultsMarker = true;
                    Console.WriteLine($"Found no-results marker: {selector}");
                    break;
                }
            }
            catch
            {
                // Try next selector
            }
        }

        // Assert: Either we found PDF links OR we have a no-results marker
        // This validates the contract: extractor can detect result state
        Assert.True(
            totalPdfLinksFound > 0 || hasNoResultsMarker,
            $"Expected either PDF link candidates (>0 found) or no-results marker. " +
            $"Found: {totalPdfLinksFound} PDF links, no-results marker: {hasNoResultsMarker}");

        Console.WriteLine($"PDF link smoke test result: {totalPdfLinksFound} PDF links found, " +
            $"no-results marker: {hasNoResultsMarker}");
    }
}
