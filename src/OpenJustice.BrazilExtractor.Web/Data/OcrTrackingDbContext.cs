using Microsoft.EntityFrameworkCore;

namespace OpenJustice.BrazilExtractor.Data;

/// <summary>
/// Database context for OCR tracking persistence using SQLite.
/// </summary>
public class OcrTrackingDbContext : DbContext
{
    public OcrTrackingDbContext(DbContextOptions<OcrTrackingDbContext> options) 
        : base(options)
    {
    }

    /// <summary>
    /// Tracks OCR processing status per page.
    /// </summary>
    public DbSet<OcrPageRecord> OcrPageRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Primary key
        modelBuilder.Entity<OcrPageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Index for efficient queries by document + page (most common query pattern)
            entity.HasIndex(e => new { e.ExecutionDate, e.PdfPath, e.PageNumber })
                .IsUnique()
                .HasDatabaseName("IX_OcrPageRecords_CompositeKey");

            // Index for queries by execution date (for "what pages for day D?")
            entity.HasIndex(e => e.ExecutionDate)
                .HasDatabaseName("IX_OcrPageRecords_ExecutionDate");

            // Index for queries by status (for "what's pending?")
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_OcrPageRecords_Status");

            // Index for queries by PDF path (for "what pages for PDF X?")
            entity.HasIndex(e => e.PdfPath)
                .HasDatabaseName("IX_OcrPageRecords_PdfPath");

            // Composite index for pending pages by date + status
            entity.HasIndex(e => new { e.ExecutionDate, e.Status })
                .HasDatabaseName("IX_OcrPageRecords_Pending");
        });
    }
}
