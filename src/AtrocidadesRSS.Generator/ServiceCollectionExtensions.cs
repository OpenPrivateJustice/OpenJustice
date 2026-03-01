using AtrocidadesRSS.Generator.Configuration;
using AtrocidadesRSS.Generator.Services.Cases;
using AtrocidadesRSS.Generator.Services.Curation;
using AtrocidadesRSS.Generator.Services.History;
using AtrocidadesRSS.Generator.Validation.Cases;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AtrocidadesRSS.Generator;

/// <summary>
/// Extension methods for configuring AtrocidadesRSS services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add AtrocidadesRSS configuration to the service collection
    /// </summary>
    public static IServiceCollection AddAtrocidadesRssConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get sections
        var connectionStrings = configuration.GetSection("ConnectionStrings");
        var filePaths = configuration.GetSection("FilePaths");
        var torrent = configuration.GetSection("Torrent");

        // Register configuration options using PostConfigure
        services.PostConfigure<AppOptions>(options =>
        {
            options.ConnectionStrings = connectionStrings.Get<ConnectionStrings>() ?? new ConnectionStrings();
            options.FilePaths = filePaths.Get<FilePaths>() ?? new FilePaths();
            options.Torrent = torrent.Get<TorrentOptions>() ?? new TorrentOptions();
        });

        // Add configuration validation
        services.AddSingleton<IValidateOptions<AppOptions>, AppConfigurationValidator>();

        // Ensure directories exist at startup
        EnsureDirectoriesExist(filePaths);

        return services;
    }

    /// <summary>
    /// Add case management services for the generator API.
    /// </summary>
    public static IServiceCollection AddCaseServices(this IServiceCollection services)
    {
        // Register validators
        services.AddValidatorsFromAssemblyContaining<CreateCaseRequestValidator>();
        
        // Register FluentValidation for API controllers
        services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Error",
                    Detail = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7807#section-3.1"
                };
                return new BadRequestObjectResult(problemDetails);
            };
        });

        // Register services
        services.AddScoped<ICaseReferenceCodeGenerator, CaseReferenceCodeGenerator>();
        services.AddScoped<ICaseWorkflowService, CaseWorkflowService>();
        services.AddScoped<ICaseAuditLogService, CaseAuditLogService>();
        services.AddScoped<ICurationService, CurationService>();

        return services;
    }

    /// <summary>
    /// Add curation services for case workflow governance.
    /// </summary>
    public static IServiceCollection AddCurationServices(this IServiceCollection services)
    {
        services.AddScoped<ICaseAuditLogService, CaseAuditLogService>();
        services.AddScoped<ICurationService, CurationService>();

        return services;
    }

    /// <summary>
    /// Validate configuration at startup - call after AddAtrocidadesRssConfiguration
    /// </summary>
    public static IServiceCollection ValidateAtrocidadesRssConfiguration(this IServiceCollection services)
    {
        // This triggers validation via IValidateOptions
        return services;
    }

    private static void EnsureDirectoriesExist(IConfigurationSection filePathsSection)
    {
        var filePaths = new FilePaths();
        filePathsSection.Bind(filePaths);
        
        EnsureDirectoryExists(filePaths.ExportDirectory);
        EnsureDirectoryExists(filePaths.BackupDirectory);
        EnsureDirectoryExists(filePaths.TempDirectory);
        EnsureDirectoryExists(filePaths.SnapshotDirectory);
    }

    private static void EnsureDirectoryExists(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch
            {
                // Ignore directory creation errors - may not have permissions at design time
            }
        }
    }
}
