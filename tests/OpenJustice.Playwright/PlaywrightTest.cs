using Microsoft.Playwright;
using Xunit;

namespace OpenJustice.Playwright;

public class ReaderTests : IAsyncLifetime, IDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private readonly string _readerUrl = "http://localhost:5280";

    public async Task InitializeAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() 
        { 
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
        });

        var context = await _browser.NewContextAsync(new() 
        { 
            BaseURL = _readerUrl,
            ViewportSize = new() { Width = 1280, Height = 720 }
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
    public async Task Reader_ShouldLoadHomePage()
    {
        await _page!.GotoAsync("/");
        await Task.Delay(2000);
        Console.WriteLine("Page loaded successfully");
    }

    [Fact]
    public async Task Reader_ShouldDisplayNavigationMenu()
    {
        await _page!.GotoAsync("/");
        
        var navMenu = await _page.QuerySelectorAsync("nav");
        Assert.NotNull(navMenu);
        Console.WriteLine("Navigation menu found");
    }

    [Fact]
    public async Task Reader_ShouldCaptureScreenshot()
    {
        await _page!.GotoAsync("/");
        
        await _page.ScreenshotAsync(new() 
        { 
            Path = "/tmp/reader-homepage.png",
            FullPage = true
        });

        Assert.True(File.Exists("/tmp/reader-homepage.png"));
        Console.WriteLine("Screenshot saved successfully");
    }
}