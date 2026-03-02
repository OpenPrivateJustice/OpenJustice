using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor;
using OpenJustice.BrazilExtractor.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Configure BrazilExtractor options with validation
builder.Services.AddOptions<BrazilExtractorOptions>()
    .Bind(builder.Configuration.GetSection("BrazilExtractor"))
    .ValidateOnStart();

// Register options validator
builder.Services.AddSingleton<IValidateOptions<BrazilExtractorOptions>, BrazilExtractorOptionsValidator>();

// Register the worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
