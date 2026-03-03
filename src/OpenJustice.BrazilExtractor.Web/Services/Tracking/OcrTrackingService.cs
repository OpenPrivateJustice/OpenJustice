using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenJustice.BrazilExtractor.Data;

namespace OpenJustice.BrazilExtractor.Services.Tracking;

/// <summary>
/// Implementation of OCR tracking service using EF Core + SQLite.
/// </summary>
public class OcrTrackingService : IOcrTrackingService
{
    private readonly IDbContextFactory<OcrTrackingDbContext> _contextFactory;
    private readonly ILogger<OcrTrackingService> _logger;

    public OcrTrackingService(
        IDbContextFactory<OcrTrackingDbContext> contextFactory,
        ILogger<OcrTrackingService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<bool> IsPageProcessedAsync(
        DateTime executionDate,
        string pdfPath,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedPath = NormalizePath(pdfPath);

        var record = await context.OcrPageRecords
            .FirstOrDefaultAsync(r =>
                r.ExecutionDate.Date == executionDate.Date &&
                r.PdfPath == normalizedPath &&
                r.PageNumber == pageNumber &&
                r.Status == OcrPageStatus.Success,
                cancellationToken);

        return record != null;
    }

    public async Task<HashSet<int>> GetProcessedPagesAsync(
        DateTime executionDate,
        string pdfPath,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedPath = NormalizePath(pdfPath);

        var pages = await context.OcrPageRecords
            .Where(r =>
                r.ExecutionDate.Date == executionDate.Date &&
                r.PdfPath == normalizedPath &&
                r.Status == OcrPageStatus.Success)
            .Select(r => r.PageNumber)
            .ToListAsync(cancellationToken);

        return new HashSet<int>(pages);
    }

    public async Task<List<OcrPageRecord>> GetPendingPagesAsync(
        DateTime executionDate,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.OcrPageRecords
            .Where(r =>
                r.ExecutionDate.Date == executionDate.Date &&
                r.Status != OcrPageStatus.Success)
            .OrderBy(r => r.PdfPath)
            .ThenBy(r => r.PageNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetPendingPageNumbersAsync(
        DateTime executionDate,
        string pdfPath,
        int totalPages,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedPath = NormalizePath(pdfPath);

        // Get already processed pages
        var processedPages = await context.OcrPageRecords
            .Where(r =>
                r.ExecutionDate.Date == executionDate.Date &&
                r.PdfPath == normalizedPath &&
                r.Status == OcrPageStatus.Success)
            .Select(r => r.PageNumber)
            .ToHashSetAsync(cancellationToken);

        // Return pages not yet processed
        var pendingPages = new List<int>();
        for (int i = 1; i <= totalPages; i++)
        {
            if (!processedPages.Contains(i))
            {
                pendingPages.Add(i);
            }
        }

        return pendingPages;
    }

    public async Task<OcrPageRecord> RecordPageStartedAsync(
        DateTime executionDate,
        string pdfPath,
        int pageNumber,
        OcrProviderType provider,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedPath = NormalizePath(pdfPath);

        // Check if record already exists
        var existing = await context.OcrPageRecords
            .FirstOrDefaultAsync(r =>
                r.ExecutionDate.Date == executionDate.Date &&
                r.PdfPath == normalizedPath &&
                r.PageNumber == pageNumber,
                cancellationToken);

        if (existing != null)
        {
            // Update existing record
            existing.Status = OcrPageStatus.Pending;
            existing.Provider = provider;
            existing.StartedAt = DateTime.UtcNow;
            existing.CompletedAt = null;
            existing.ErrorMessage = null;
            existing.UpdatedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        // Create new record
        var record = new OcrPageRecord
        {
            ExecutionDate = executionDate.Date,
            PdfPath = normalizedPath,
            PageNumber = pageNumber,
            Status = OcrPageStatus.Pending,
            Provider = provider,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.OcrPageRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Recorded page start: {PdfPath} page {PageNumber}", pdfPath, pageNumber);
        
        return record;
    }

    public async Task RecordPageSuccessAsync(
        int recordId,
        string? imageHash,
        int charactersExtracted,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var record = await context.OcrPageRecords.FindAsync([recordId], cancellationToken);
        if (record == null)
        {
            _logger.LogWarning("Record not found for ID: {RecordId}", recordId);
            return;
        }

        record.Status = OcrPageStatus.Success;
        record.ImageHash = imageHash;
        record.CharactersExtracted = charactersExtracted;
        record.CompletedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Recorded page success: {PdfPath} page {PageNumber}, {Chars} chars",
            record.PdfPath, record.PageNumber, charactersExtracted);
    }

    public async Task RecordPageFailedAsync(
        int recordId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var record = await context.OcrPageRecords.FindAsync([recordId], cancellationToken);
        if (record == null)
        {
            _logger.LogWarning("Record not found for ID: {RecordId}", recordId);
            return;
        }

        record.Status = OcrPageStatus.Failed;
        record.ErrorMessage = errorMessage;
        record.CompletedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        
        _logger.LogDebug("Recorded page failure: {PdfPath} page {PageNumber}, error: {Error}",
            record.PdfPath, record.PageNumber, errorMessage);
    }

    public async Task RecordPageSkippedAsync(
        DateTime executionDate,
        string pdfPath,
        int pageNumber,
        OcrProviderType provider,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedPath = NormalizePath(pdfPath);

        var existing = await context.OcrPageRecords
            .FirstOrDefaultAsync(r =>
                r.ExecutionDate.Date == executionDate.Date &&
                r.PdfPath == normalizedPath &&
                r.PageNumber == pageNumber,
                cancellationToken);

        if (existing != null)
        {
            existing.Status = OcrPageStatus.Skipped;
            existing.Provider = provider;
            existing.CompletedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            context.OcrPageRecords.Add(new OcrPageRecord
            {
                ExecutionDate = executionDate.Date,
                PdfPath = normalizedPath,
                PageNumber = pageNumber,
                Status = OcrPageStatus.Skipped,
                Provider = provider,
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OcrPageRecord> CreatePageRecordAsync(
        DateTime executionDate,
        string pdfPath,
        int pageNumber,
        OcrProviderType provider,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedPath = NormalizePath(pdfPath);

        var existing = await context.OcrPageRecords
            .FirstOrDefaultAsync(r =>
                r.ExecutionDate.Date == executionDate.Date &&
                r.PdfPath == normalizedPath &&
                r.PageNumber == pageNumber,
                cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        var record = new OcrPageRecord
        {
            ExecutionDate = executionDate.Date,
            PdfPath = normalizedPath,
            PageNumber = pageNumber,
            Status = OcrPageStatus.Pending,
            Provider = provider,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.OcrPageRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);
        
        return record;
    }

    public async Task<(int total, int success, int failed, int pending)> GetPdfStatusSummaryAsync(
        DateTime executionDate,
        string pdfPath,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedPath = NormalizePath(pdfPath);

        var records = await context.OcrPageRecords
            .Where(r =>
                r.ExecutionDate.Date == executionDate.Date &&
                r.PdfPath == normalizedPath)
            .ToListAsync(cancellationToken);

        return (
            total: records.Count,
            success: records.Count(r => r.Status == OcrPageStatus.Success),
            failed: records.Count(r => r.Status == OcrPageStatus.Failed),
            pending: records.Count(r => r.Status != OcrPageStatus.Success)
        );
    }

    public async Task<(int total, int success, int failed, int pending)> GetDateStatusSummaryAsync(
        DateTime executionDate,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var records = await context.OcrPageRecords
            .Where(r => r.ExecutionDate.Date == executionDate.Date)
            .ToListAsync(cancellationToken);

        return (
            total: records.Count,
            success: records.Count(r => r.Status == OcrPageStatus.Success),
            failed: records.Count(r => r.Status == OcrPageStatus.Failed),
            pending: records.Count(r => r.Status != OcrPageStatus.Success)
        );
    }

    public async Task<List<OcrPageRecord>> GetRecordsForPdfAsync(
        DateTime executionDate,
        string pdfPath,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedPath = NormalizePath(pdfPath);

        return await context.OcrPageRecords
            .Where(r =>
                r.ExecutionDate.Date == executionDate.Date &&
                r.PdfPath == normalizedPath)
            .OrderBy(r => r.PageNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("OCR Tracking database migrated");
    }

    /// <summary>
    /// Normalizes the path for consistent storage.
    /// </summary>
    private static string NormalizePath(string path)
    {
        // Convert to full path and normalize separators
        try
        {
            return Path.GetFullPath(path).Replace('\\', '/');
        }
        catch
        {
            return path.Replace('\\', '/');
        }
    }
}
