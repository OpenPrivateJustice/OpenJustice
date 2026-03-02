using OpenJustice.Generator;
using OpenJustice.Generator.Configuration;
using OpenJustice.Generator.Web.Components;
using OpenJustice.Generator.Web.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add OpenJustice configuration with fail-fast validation
builder.Services.AddOpenJusticeConfiguration(builder.Configuration);

// Add options validation
builder.Services.AddOptions<GeneratorOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register HTTP client for API calls
builder.Services.AddHttpClient<IGeneratorApiClient, GeneratorApiClient>(client =>
{
    // Configure the base URL - in development, use the API endpoint
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000");
});

// Register typed HTTP client
builder.Services.AddScoped<IGeneratorApiClient, GeneratorApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
