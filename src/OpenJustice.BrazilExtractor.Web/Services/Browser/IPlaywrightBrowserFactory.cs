using Microsoft.Playwright;

namespace OpenJustice.BrazilExtractor.Services.Browser;

/// <summary>
/// Factory for creating and managing Playwright browser instances.
/// </summary>
public interface IPlaywrightBrowserFactory
{
    /// <summary>
    /// Gets a configured Playwright instance.
    /// </summary>
    IPlaywright Playwright { get; }

    /// <summary>
    /// Creates a new browser instance with configured options.
    /// </summary>
    /// <param name="headless">Whether to run the browser in headless mode.</param>
    /// <returns>A disposable browser instance.</returns>
    Task<IBrowser> CreateBrowserAsync(bool headless = true);

    /// <summary>
    /// Creates a new browser context with default settings.
    /// </summary>
    /// <param name="browser">The browser to create the context from.</param>
    /// <returns>A disposable browser context.</returns>
    Task<IBrowserContext> CreateContextAsync(IBrowser browser);

    /// <summary>
    /// Creates a new page within the given context.
    /// </summary>
    /// <param name="context">The browser context.</param>
    /// <returns>A disposable page.</returns>
    Task<IPage> CreatePageAsync(IBrowserContext context);
}
