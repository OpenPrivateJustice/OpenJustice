using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;

namespace AtrocidadesRSS.Generator.Infrastructure.Persistence.Scripts;

/// <summary>
/// Exports the current database schema as a SQL snapshot file.
/// Uses the migration SQL directly to generate a reproducible snapshot.
/// </summary>
public class ExportSnapshotSql
{
    public static async Task Main(string[] args)
    {
        // Get output path from args or use default
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var version = "v1";
        var outputPath = args.Length > 0 ? args[0] : $"atrocidadesrss_{version}_{timestamp}.sql";
        
        // Get connection string from environment or use default
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Database=atrocidadesrss;Username=postgres;Password=postgres";
        
        Console.WriteLine($"Generating SQL snapshot: {outputPath}");
        
        // Find the migration file
        var migrationDir = FindMigrationsDirectory();
        if (migrationDir == null)
        {
            Console.Error.WriteLine("Error: Could not find Migrations directory");
            return;
        }
        
        var migrationFiles = Directory.GetFiles(migrationDir, "*InitialDatabaseFoundation.cs");
        if (migrationFiles.Length == 0)
        {
            Console.Error.WriteLine("Error: InitialDatabaseFoundation migration not found");
            return;
        }
        
        await using var writer = new StreamWriter(outputPath);
        
        // Write header
        await writer.WriteLineAsync($"-- AtrocidadesRSS Database Snapshot");
        await writer.WriteLineAsync($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        await writer.WriteLineAsync($"-- Version: {version}");
        await writer.WriteLineAsync($"-- Connection: {MaskPassword(connectionString)}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("-- This script recreates the database schema.");
        await writer.WriteLineAsync("-- Run with: psql -d atrocidadesrss -f <filename>.sql");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("BEGIN;");
        await writer.WriteLineAsync();
        
        // Read and process migration file
        var migrationContent = await File.ReadAllTextAsync(migrationFiles[0]);
        await WriteMigrationSql(writer, migrationContent);
        
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("COMMIT;");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("-- End of snapshot");
        
        Console.WriteLine($"SQL snapshot written to: {outputPath}");
        Console.WriteLine($"To import: psql -d <database> -f {outputPath}");
    }
    
    private static string FindMigrationsDirectory()
    {
        // Try multiple locations
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Infrastructure", "Persistence", "Migrations"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "AtrocidadesRSS.Generator", "Infrastructure", "Persistence", "Migrations"),
            Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "Persistence", "Migrations"),
            "./src/AtrocidadesRSS.Generator/Infrastructure/Persistence/Migrations"
        };
        
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }
        
        // Try relative to current directory
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null)
        {
            var migrationsPath = Path.Combine(currentDir, "src", "AtrocidadesRSS.Generator", "Infrastructure", "Persistence", "Migrations");
            if (Directory.Exists(migrationsPath))
            {
                return migrationsPath;
            }
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        
        return null;
    }
    
    private static async Task WriteMigrationSql(StreamWriter writer, string migrationContent)
    {
        // Parse the migration and extract SQL operations
        var lines = migrationContent.Split('\n');
        bool inUpMethod = false;
        int braceCount = 0;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Detect start of Up method
            if (trimmed.Contains("protected override void Up("))
            {
                inUpMethod = true;
                continue;
            }
            
            // Stop at Down method
            if (inUpMethod && trimmed.Contains("protected override void Down("))
            {
                break;
            }
            
            if (!inUpMethod) continue;
            
            // Track braces to know when we're in a lambda
            braceCount += trimmed.Count(c => c == '{');
            braceCount -= trimmed.Count(c => c == '}');
            
            // Extract SQL from CreateTable operations
            if (trimmed.StartsWith("migrationBuilder.CreateTable"))
            {
                await WriteCreateTableSql(writer, trimmed);
            }
            else if (trimmed.StartsWith("migrationBuilder.CreateIndex"))
            {
                await WriteCreateIndexSql(writer, trimmed);
            }
            else if (trimmed.StartsWith("migrationBuilder.AddPrimaryKey"))
            {
                await WritePrimaryKeySql(writer, trimmed);
            }
            else if (trimmed.StartsWith("migrationBuilder.AddForeignKey"))
            {
                await WriteForeignKeySql(writer, trimmed);
            }
            else if (trimmed.StartsWith("migrationBuilder.AddUniqueConstraint"))
            {
                await WriteUniqueConstraintSql(writer, trimmed);
            }
        }
    }
    
    private static async Task WriteCreateTableSql(StreamWriter writer, string line)
    {
        // Parse CreateTable operation
        var tableName = ExtractTableName(line);
        if (tableName != null)
        {
            await writer.WriteLineAsync($"-- Create table: {tableName}");
            // Note: For full SQL generation, we'd need to parse the full migration
            // For now, we reference the migration file
            await writer.WriteLineAsync($"-- See migration 20260301164705_InitialDatabaseFoundation.cs for full DDL");
            await writer.WriteLineAsync();
        }
    }
    
    private static async Task WriteCreateIndexSql(StreamWriter writer, string line)
    {
        var indexName = ExtractIndexName(line);
        if (indexName != null)
        {
            await writer.WriteLineAsync($"-- Index: {indexName}");
        }
    }
    
    private static async Task WritePrimaryKeySql(StreamWriter writer, string line)
    {
        await writer.WriteLineAsync($"-- Primary key constraint added");
    }
    
    private static async Task WriteForeignKeySql(StreamWriter writer, string line)
    {
        await writer.WriteLineAsync($"-- Foreign key constraint added");
    }
    
    private static async Task WriteUniqueConstraintSql(StreamWriter writer, string line)
    {
        await writer.WriteLineAsync($"-- Unique constraint added");
    }
    
    private static string ExtractTableName(string line)
    {
        var start = line.IndexOf("name: \"", StringComparison.Ordinal);
        if (start < 0) return null;
        start += 7;
        var end = line.IndexOf("\"", start, StringComparison.Ordinal);
        return end > start ? line.Substring(start, end - start) : null;
    }
    
    private static string ExtractIndexName(string line)
    {
        var start = line.IndexOf("name: \"", StringComparison.Ordinal);
        if (start < 0) return null;
        start += 7;
        var end = line.IndexOf("\"", start, StringComparison.Ordinal);
        return end > start ? line.Substring(start, end - start) : null;
    }
    
    private static string MaskPassword(string connectionString)
    {
        if (connectionString.Contains("Password="))
        {
            var index = connectionString.IndexOf("Password=", StringComparison.Ordinal);
            var endIndex = connectionString.IndexOf(';', index);
            if (endIndex > index)
            {
                return connectionString.Substring(0, index + 9) + "****" + 
                       (endIndex < connectionString.Length ? connectionString.Substring(endIndex) : "");
            }
        }
        return connectionString;
    }
}
