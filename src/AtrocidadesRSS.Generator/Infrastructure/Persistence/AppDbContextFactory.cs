using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AtrocidadesRSS.Generator.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("DATABASE_URL") 
            ?? "Host=localhost;Database=atrocidadesrss;Username=postgres;Password=postgres");
        return new AppDbContext(optionsBuilder.Options);
    }
}
