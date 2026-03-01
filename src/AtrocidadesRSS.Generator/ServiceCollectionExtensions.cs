using AtrocidadesRSS.Generator.Configuration;
using AtrocidadesRSS.Generator.Services.Cases;
using AtrocidadesRSS.Generator.Services.Curation;
using AtrocidadesRSS.Generator.Services.Discovery;
using AtrocidadesRSS.Generator.Services.Export;
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
        var discovery = configuration.GetSection("Discovery");

        // Register legacy AppOptions for backward compatibility
        services.PostConfigure<AppOptions>(options =>
        {
            options.ConnectionStrings = connectionStrings.Get<ConnectionStrings>() ?? new ConnectionStrings();
            options.FilePaths = filePaths.Get<FilePaths>() ?? new FilePaths();
            options.Torrent = torrent.Get<TorrentOptions>() ?? new TorrentOptions();
        });

        // Add configuration validation for legacy options
        services.AddSingleton<IValidateOptions<AppOptions>, AppConfigurationValidator>();

        // Register new GeneratorOptions (primary configuration)
        services.Configure<GeneratorOptions>(options =>
        {
            // Bind database settings
            var dbConfig = configuration.GetSection("Database");
            if (dbConfig.Exists())
            {
                options.Database = dbConfig.Get<DatabaseOptions>() ?? new DatabaseOptions();
            }
            else
            {
                // Fall back to ConnectionStrings for backward compatibility
                var connStr = connectionStrings.Get<ConnectionStrings>();
                if (connStr != null)
                {
                    options.Database.ConnectionString = connStr.DefaultConnection;
                }
            }

            // Bind file paths
            options.FilePaths = filePaths.Get<FilePathsOptions>() ?? new FilePathsOptions();

            // Bind discovery
            options.Discovery = discovery.Get<DiscoveryOptions>() ?? new DiscoveryOptions();

            // Bind export settings
            var export = configuration.GetSection("Export");
            if (export.Exists())
            {
                options.Export = export.Get<ExportOptions>() ?? new ExportOptions();
            }

            // Bind torrent
            options.Torrent = torrent.Get<TorrentOptions>();
        });

        // Add validation for GeneratorOptions
        services.AddSingleton<IValidateOptions<GeneratorOptions>, GeneratorOptionsValidator>();

        // Register Snapshot services
        services.AddSingleton<ISnapshotVersionService, SnapshotVersionService>();
        services.Configure<SnapshotExportOptions>(snapshotOptions =>
        {
            // Map from GeneratorOptions to SnapshotExportOptions
            var genOptions = new GeneratorOptions();
            configuration.GetSection("Database").Bind(genOptions.Database);
            filePaths.Bind(genOptions.FilePaths);
            
            var export = configuration.GetSection("Export");
            if (export.Exists())
            {
                export.Bind(genOptions.Export);
            }

            // Set snapshot-specific options
            snapshotOptions.SnapshotDirectory = genOptions.FilePaths.SnapshotDirectory;
            snapshotOptions.ConnectionString = genOptions.Database.ConnectionString;
            snapshotOptions.Host = genOptions.Database.Host;
            snapshotOptions.Port = genOptions.Database.Port;
            snapshotOptions.Database = genOptions.Database.Name;
            snapshotOptions.Username = genOptions.Database.Username;
            snapshotOptions.Password = genOptions.Database.Password;
            snapshotOptions.FilePrefix = genOptions.Export.FilePrefix;
            snapshotOptions.PgDumpPath = genOptions.Export.PgDumpPath;
        });

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
        services.AddScoped<ICaseFieldHistoryService, CaseFieldHistoryService>();
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
    /// Add discovery services for automated RSS and Reddit ingestion.
    /// </summary>
    public static IServiceCollection AddDiscoveryServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Discovery options
        services.Configure<DiscoveryOptions>(configuration.GetSection("Discovery"));
        
        // Register discovery services
        services.AddScoped<IRssAggregatorService, RssAggregatorService>();
        services.AddScoped<IRedditThreadScraperService, RedditThreadScraperService>();
        services.AddScoped<IDiscoveredCaseReviewService, DiscoveredCaseReviewService>();
        
        // Register HttpClient for discovery services
        services.AddHttpClient<IRssAggregatorService, RssAggregatorService>();
        services.AddHttpClient<IRedditThreadScraperService, RedditThreadScraperService>();

        return services;
    }

    /// <summary>
    /// Add export services for SQL snapshot generation.
    /// </summary>
    public static IServiceCollection AddExportServices(this IServiceCollection services)
    {
        // Register snapshot export service
        services.AddScoped<ISnapshotExportService, SnapshotExportService>();

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
        var filePaths = new FilePathsOptions();
        filePathsSection.Bind(filePaths);
        
        EnsureDirectoryExists(filePaths.SnapshotDirectory);
        EnsureDirectoryExists(filePaths.BackupDirectory);
        EnsureDirectoryExists(filePaths.ExportDirectory);
        EnsureDirectoryExists(filePaths.TempDirectory);
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
