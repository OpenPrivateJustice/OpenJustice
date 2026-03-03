using Microsoft.Playwright;

namespace OpenJustice.BrazilExtractor.Services.Browser;

/// <summary>
/// Factory for creating and managing Playwright browser instances.
/// Provides centralized Playwright + Chromium lifecycle management.
/// </summary>
public class PlaywrightBrowserFactory : IPlaywrightBrowserFactory, IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private bool _disposed;

    /// <summary>
    /// Gets the Playwright instance.
    /// </summary>
    public IPlaywright Playwright => _playwright;

    /// <summary>
    /// Initializes the browser factory with a Playwright instance.
    /// </summary>
    public PlaywrightBrowserFactory()
    {
        _playwright = Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates a new browser instance with deterministic options.
    /// </summary>
    /// <param name="headless">Whether to run the browser in headless mode.</param>
    /// <returns>A configured browser instance.</returns>
    public async Task<IBrowser> CreateBrowserAsync(bool headless = true)
    {
        return await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-blink-features=AutomationControlled"
            }
        });
    }

    /// <summary>
    /// Creates a new browser context with default settings.
    /// </summary>
    /// <param name="browser">The browser to create the context from.</param>
    /// <returns>A configured browser context.</returns>
    public async Task<IBrowserContext> CreateContextAsync(IBrowser browser)
    {
        return await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
    }

    /// <summary>
    /// Creates a new page within the given context.
    /// </summary>
    /// <param name="context">The browser context.</param>
    /// <returns>A configured page.</returns>
    public async Task<IPage> CreatePageAsync(IBrowserContext context)
    {
        return await context.NewPageAsync();
    }

    /// <summary>
    /// Disposes the Playwright instance.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _playwright.Dispose();
            _disposed = true;
        }
        await ValueTask.CompletedTask;
    }
}
