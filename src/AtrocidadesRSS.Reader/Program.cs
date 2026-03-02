using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using AtrocidadesRSS.Reader;
using AtrocidadesRSS.Reader.Configuration;
using AtrocidadesRSS.Reader.Services.Sync;
using AtrocidadesRSS.Reader.Services.Data;
using AtrocidadesRSS.Reader.Services.Search;
using AtrocidadesRSS.Reader.Services.Cases;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Configure Reader options with validation (only required fields)
builder.Services.AddOptions<ReaderOptions>()
    .Bind(builder.Configuration.GetSection("Torrent"))
    .Bind(builder.Configuration.GetSection("Snapshot"))
    .Bind(builder.Configuration.GetSection("LocalDb"))
    .Bind(builder.Configuration.GetSection("GeneratorHistoryApi"))
    .ValidateOnStart();

// Configure HttpClient for Generator History API
builder.Services.AddHttpClient(nameof(GeneratorHistoryApiClient));

// Register options validator
builder.Services.AddSingleton<IValidateOptions<ReaderOptions>, ReaderOptionsValidator>();

// Register sync services
builder.Services.AddSingleton<IVersionService, VersionService>();
builder.Services.AddSingleton<ITorrentSyncService, TorrentSyncService>();

// Register local case store
builder.Services.AddSingleton<ILocalCaseStore, SqliteCaseStore>();

// Register search service
builder.Services.AddSingleton<ICaseSearchService, CaseSearchService>();

// Register case details service
builder.Services.AddSingleton<ICaseDetailsService, CaseDetailsService>();

// Register case history service
builder.Services.AddSingleton<ICaseHistoryService, CaseHistoryService>();

// Register Generator history API client
builder.Services.AddSingleton<IGeneratorHistoryApiClient, GeneratorHistoryApiClient>();

await builder.Build().RunAsync();
