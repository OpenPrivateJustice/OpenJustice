using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenJustice.BrazilExtractor.Data;

/// <summary>
/// Design-time factory for EF Core migrations.
/// </summary>
public class OcrTrackingDbContextFactory : IDesignTimeDbContextFactory<OcrTrackingDbContext>
{
    public OcrTrackingDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var dbPath = Path.Combine(basePath, "data", "ocr_tracking.db");

        var optionsBuilder = new DbContextOptionsBuilder<OcrTrackingDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new OcrTrackingDbContext(optionsBuilder.Options);
    }
}
