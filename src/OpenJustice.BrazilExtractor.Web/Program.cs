using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Data;
using OpenJustice.BrazilExtractor.Services.Browser;
using OpenJustice.BrazilExtractor.Services.Downloads;
using OpenJustice.BrazilExtractor.Services.Jobs;
using OpenJustice.BrazilExtractor.Services.Ocr;
using OpenJustice.BrazilExtractor.Services.Tjgo;
using OpenJustice.BrazilExtractor.Services.Tracking;
using OpenJustice.BrazilExtractor.Web;
using OpenJustice.BrazilExtractor.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure BrazilExtractor options
builder.Services.AddOptions<BrazilExtractorOptions>()
    .Bind(builder.Configuration.GetSection("BrazilExtractor"))
    .ValidateOnStart();

// Register options validator
builder.Services.AddSingleton<IValidateOptions<BrazilExtractorOptions>, BrazilExtractorOptionsValidator>();

// Register Playwright browser factory (singleton for lifecycle management)
builder.Services.AddSingleton<IPlaywrightBrowserFactory, PlaywrightBrowserFactory>();

// Register TJGO services (scoped for web requests)
builder.Services.AddScoped<ITjgoSearchService, TjgoSearchService>();
builder.Services.AddScoped<ITjgoSearchJob, TjgoSearchJob>();

// Register PDF download service
builder.Services.AddSingleton<IPdfDownloadService, PdfDownloadService>();

// Register OCR services (resolved by provider selection at runtime)
builder.Services.AddTransient<LlamaCppVisionOcrService>();
builder.Services.AddTransient<OpenAiVisionOcrService>();

// Register OCR tracking service with EF Core + SQLite
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "data", "ocr_tracking.db");
var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
{
    Directory.CreateDirectory(dbDirectory);
}

builder.Services.AddDbContextFactory<OcrTrackingDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IOcrTrackingService, OcrTrackingService>();

// Register ExtractionManager
builder.Services.AddSingleton<ExtractionManager>();

// Ensure options services are available
builder.Services.AddOptions();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<OcrTrackingDbContext>>();
    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
    await dbContext.Database.MigrateAsync();
    app.Logger.LogInformation("OCR Tracking database migrated at: {DbPath}", dbPath);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<OpenJustice.BrazilExtractor.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
