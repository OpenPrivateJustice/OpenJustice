using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Services.Browser;
using OpenJustice.BrazilExtractor.Services.Jobs;
using OpenJustice.BrazilExtractor.Services.Tjgo;

var builder = Host.CreateApplicationBuilder(args);

// Configure BrazilExtractor options with validation
builder.Services.AddOptions<BrazilExtractorOptions>()
    .Bind(builder.Configuration.GetSection("BrazilExtractor"))
    .ValidateOnStart();

// Register options validator
builder.Services.AddSingleton<IValidateOptions<BrazilExtractorOptions>, BrazilExtractorOptionsValidator>();

// Register Playwright browser factory (singleton for lifecycle management)
builder.Services.AddSingleton<IPlaywrightBrowserFactory, PlaywrightBrowserFactory>();

// Register TJGO services
builder.Services.AddScoped<ITjgoSearchService, TjgoSearchService>();
builder.Services.AddScoped<ITjgoSearchJob, TjgoSearchJob>();

// Register the worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
