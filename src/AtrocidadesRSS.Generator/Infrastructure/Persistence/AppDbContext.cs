using Microsoft.EntityFrameworkCore;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;

namespace AtrocidadesRSS.Generator.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CrimeType> CrimeTypes => Set<CrimeType>();
    public DbSet<CaseType> CaseTypes => Set<CaseType>();
    public DbSet<JudicialStatus> JudicialStatuses => Set<JudicialStatus>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Evidence> Evidences => Set<Evidence>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<CaseTag> CaseTags => Set<CaseTag>();
    public DbSet<CaseFieldHistory> CaseFieldHistories => Set<CaseFieldHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Case Entity Configuration
        modelBuilder.Entity<Case>(entity =>
        {
            entity.ToTable("Cases", table => table.HasCheckConstraint(
                "CK_Cases_VictimConfidence", 
                "VictimConfidence >= 0 AND VictimConfidence <= 100"));
            
            entity.ToTable("Cases", table => table.HasCheckConstraint(
                "CK_Cases_AccusedConfidence", 
                "AccusedConfidence >= 0 AND AccusedConfidence <= 100"));
            
            entity.ToTable("Cases", table => table.HasCheckConstraint(
                "CK_Cases_CrimeConfidence", 
                "CrimeConfidence >= 0 AND CrimeConfidence <= 100"));
            
            entity.ToTable("Cases", table => table.HasCheckConstraint(
                "CK_Cases_JudicialConfidence", 
                "JudicialConfidence >= 0 AND JudicialConfidence <= 100"));

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            // Reference Code - unique identifier
            entity.Property(e => e.ReferenceCode)
                .IsRequired()
                .HasMaxLength(50);

            // Required fields
            entity.Property(e => e.RegistrationDate).IsRequired();
            entity.Property(e => e.LastUpdated).IsRequired();
            entity.Property(e => e.NumberOfVictims).IsRequired();
            entity.Property(e => e.NumberOfAccused).IsRequired();
            entity.Property(e => e.IsSensitiveContent).HasDefaultValue(false);
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Foreign Keys
            entity.HasOne(e => e.CrimeType)
                .WithMany(c => c.Cases)
                .HasForeignKey(e => e.CrimeTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CaseType)
                .WithMany(c => c.Cases)
                .HasForeignKey(e => e.CaseTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.JudicialStatus)
                .WithMany(j => j.Cases)
                .HasForeignKey(e => e.JudicialStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-many relationships
            entity.HasMany(e => e.Sources)
                .WithOne(s => s.Case)
                .HasForeignKey(s => s.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Evidences)
                .WithOne(e => e.Case)
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CaseTags)
                .WithOne(ct => ct.Case)
                .HasForeignKey(ct => ct.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.FieldHistories)
                .WithOne(f => f.Case)
                .HasForeignKey(f => f.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for common queries
            entity.HasIndex(e => e.ReferenceCode).IsUnique();
            entity.HasIndex(e => e.CrimeDate);
            entity.HasIndex(e => e.CrimeLocationState);
            entity.HasIndex(e => e.CrimeLocationCity);
            entity.HasIndex(e => e.VictimName);
            entity.HasIndex(e => e.AccusedName);
            entity.HasIndex(e => e.JudicialStatusId);

            // Composite indexes for multi-column filter queries (DB-16)
            entity.HasIndex(e => new { e.CrimeTypeId, e.JudicialStatusId })
                .HasDatabaseName("IX_Cases_CrimeTypeId_JudicialStatusId");
            entity.HasIndex(e => new { e.CrimeLocationState, e.CrimeDate })
                .HasDatabaseName("IX_Cases_CrimeLocationState_CrimeDate");
        });

        // CrimeType Entity Configuration
        modelBuilder.Entity<CrimeType>(entity =>
        {
            entity.ToTable("CrimeTypes", table => table.HasCheckConstraint(
                "CK_CrimeTypes_Confidence", 
                "Confidence >= 0 AND Confidence <= 100"));
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // CaseType Entity Configuration (modality - tentativa/consumado)
        modelBuilder.Entity<CaseType>(entity =>
        {
            entity.ToTable("CaseTypes", table => table.HasCheckConstraint(
                "CK_CaseTypes_Confidence", 
                "Confidence >= 0 AND Confidence <= 100"));
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // JudicialStatus Entity Configuration
        modelBuilder.Entity<JudicialStatus>(entity =>
        {
            entity.ToTable("JudicialStatuses", table => table.HasCheckConstraint(
                "CK_JudicialStatuses_Confidence", 
                "Confidence >= 0 AND Confidence <= 100"));
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Source Entity Configuration
        modelBuilder.Entity<Source>(entity =>
        {
            entity.ToTable("Sources", table => table.HasCheckConstraint(
                "CK_Sources_Confidence", 
                "Confidence >= 0 AND Confidence <= 100"));
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.SourceName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OriginalLink).HasMaxLength(2048);
            entity.HasIndex(e => e.CaseId);
        });

        // Evidence Entity Configuration
        modelBuilder.Entity<Evidence>(entity =>
        {
            entity.ToTable("Evidence", table => table.HasCheckConstraint(
                "CK_Evidence_Confidence", 
                "Confidence >= 0 AND Confidence <= 100"));
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.EvidenceType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Link).HasMaxLength(2048);
            entity.HasIndex(e => e.CaseId);
        });

        // Tag Entity Configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tags");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        // CaseTag (Many-to-Many Join Table) Configuration
        modelBuilder.Entity<CaseTag>(entity =>
        {
            entity.ToTable("CaseTags");
            entity.HasKey(ct => new { ct.CaseId, ct.TagId });
            entity.HasIndex(e => e.TagId);
        });

        // CaseFieldHistory Entity Configuration
        modelBuilder.Entity<CaseFieldHistory>(entity =>
        {
            entity.ToTable("CaseFieldHistory", table => table.HasCheckConstraint(
                "CK_CaseFieldHistory_Confidence", 
                "ChangeConfidence >= 0 AND ChangeConfidence <= 100"));
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.CaseId);
            entity.HasIndex(e => e.ChangedAt);
        });
    }
}

