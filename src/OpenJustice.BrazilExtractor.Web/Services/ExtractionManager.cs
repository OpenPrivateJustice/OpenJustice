using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Data;
using OpenJustice.BrazilExtractor.Services.Jobs;
using OpenJustice.BrazilExtractor.Services.Ocr;
using OpenJustice.BrazilExtractor.Services.Progress;
using OpenJustice.BrazilExtractor.Services.Tracking;

namespace OpenJustice.BrazilExtractor.Web.Services;

/// <summary>
/// Service for managing extraction operations that can be controlled via UI.
/// Supports running "Run Extraction" and "OCR Only" in parallel.
/// </summary>
public class ExtractionManager
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BrazilExtractorOptions _options;
    private readonly ILogger<ExtractionManager> _logger;

    private readonly object _sync = new();
    private CancellationTokenSource? _extractionCts;
    private CancellationTokenSource? _ocrOnlyCts;
    private int _progressSubscriptionCount;

    private readonly ExtractorState _state = new();

    /// <summary>
    /// Base directory for all downloads - inside the application's base directory.
    /// </summary>
    private static string BaseDownloadPath { get; } = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "downloads"));

    public ExtractorState State => _state;
    public event Action? StateChanged;

    public ExtractionManager(
        IServiceScopeFactory scopeFactory,
        IOptions<BrazilExtractorOptions> options,
        ILogger<ExtractionManager> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;

        // Set initial provider from configuration
        if (_options.UseLlamaCppVision)
        {
            _state.SelectedOcrProvider = OcrProvider.LlamaCpp;
        }
        else if (_options.UseOpenAiVision)
        {
            _state.SelectedOcrProvider = OcrProvider.OpenAI;
        }
        else
        {
            _state.SelectedOcrProvider = OcrProvider.LlamaCpp;
        }
    }

    private static IOcrExtractionService CreateOcrService(OcrProvider provider, IServiceProvider serviceProvider)
    {
        return provider switch
        {
            OcrProvider.OpenAI => serviceProvider.GetRequiredService<OpenAiVisionOcrService>(),
            OcrProvider.LlamaCpp => serviceProvider.GetRequiredService<LlamaCppVisionOcrService>(),
            _ => throw new InvalidOperationException($"Unknown OCR provider: {provider}")
        };
    }

    public async Task RunExtractionAsync(DateTime? queryDate = null)
    {
        var date = (queryDate ?? _options.QueryDateWindowStartDate ?? DateTime.Today).Date;
        var dateText = date.ToString("yyyy-MM-dd");

        CancellationToken cancellationToken;

        lock (_sync)
        {
            if (_state.ExtractionStatus == ExtractorStatus.Running)
            {
                _logger.LogWarning("Extraction already in progress");
                return;
            }

            _extractionCts?.Dispose();
            _extractionCts = new CancellationTokenSource();
            cancellationToken = _extractionCts.Token;

            _state.ExtractionStatus = ExtractorStatus.Running;
            _state.LastRunTime = DateTime.UtcNow;
            _state.LastError = null;
            _state.LastDateQueried = dateText;
            _state.LastOcrSucceeded = 0;
            _state.LastOcrFailed = 0;
            RefreshAggregateStatusLocked();
        }

        SubscribeProgressUpdates();
        AddExtractionLog($"Starting extraction for date {dateText}...");

        try
        {
            using var progressScope = ExtractionProgress.BeginScope(ProgressWorkflow.Extraction);
            using var scope = _scopeFactory.CreateScope();

            var searchJob = scope.ServiceProvider.GetRequiredService<ITjgoSearchJob>();
            var searchResult = await searchJob.ExecuteAsync(date, cancellationToken);

            if (searchResult.Success && searchResult.PdfLinks.Count > 0)
            {
                AddExtractionLog($"Found {searchResult.PdfLinks.Count} PDF links");

                if (searchResult.DownloadResult != null)
                {
                    lock (_sync)
                    {
                        _state.LastPdfCount = searchResult.DownloadResult.SucceededCount;
                    }

                    AddExtractionLog($"Downloaded {searchResult.DownloadResult.SucceededCount}/{searchResult.DownloadResult.AttemptedCount} PDFs");
                    AddExtractionLog("Extraction finished (OCR is manual via OCR Only button)");
                }
            }
            else
            {
                AddExtractionLog($"No PDF links found or search failed: {searchResult.ErrorMessage}");
            }

            lock (_sync)
            {
                _state.ExtractionStatus = ExtractorStatus.Completed;
                _state.LastSuccessfulRun = DateTime.UtcNow;
                RefreshAggregateStatusLocked();
            }

            AddExtractionLog("Completed successfully");
        }
        catch (OperationCanceledException)
        {
            lock (_sync)
            {
                _state.ExtractionStatus = ExtractorStatus.Idle;
                RefreshAggregateStatusLocked();
            }

            AddExtractionLog("Cancelled");
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _state.ExtractionStatus = ExtractorStatus.Error;
                _state.LastError = ex.Message;
                RefreshAggregateStatusLocked();
            }

            AddExtractionLog($"Error: {ex.Message}");
            _logger.LogError(ex, "Extraction failed");
        }
        finally
        {
            lock (_sync)
            {
                _extractionCts?.Dispose();
                _extractionCts = null;
                RefreshAggregateStatusLocked();
            }

            UnsubscribeProgressUpdates();
            StateChanged?.Invoke();
        }
    }

    public async Task RunOcrOnlyAsync(DateTime? queryDate = null)
    {
        var date = (queryDate ?? _options.QueryDateWindowStartDate ?? DateTime.Today).Date;
        var dateText = date.ToString("yyyy-MM-dd");

        CancellationToken cancellationToken;
        OcrProvider provider;

        lock (_sync)
        {
            if (_state.OcrOnlyStatus == ExtractorStatus.Running)
            {
                _logger.LogWarning("OCR-only already in progress");
                return;
            }

            _ocrOnlyCts?.Dispose();
            _ocrOnlyCts = new CancellationTokenSource();
            cancellationToken = _ocrOnlyCts.Token;

            _state.OcrOnlyStatus = ExtractorStatus.Running;
            _state.LastOcrOnlyRunTime = DateTime.UtcNow;
            _state.LastOcrOnlyError = null;
            _state.LastOcrOnlyDateQueried = dateText;
            provider = _state.SelectedOcrProvider;
            RefreshAggregateStatusLocked();
        }

        SubscribeProgressUpdates();
        AddOcrOnlyLog($"Starting OCR-only for date {dateText}...");

        try
        {
            using var progressScope = ExtractionProgress.BeginScope(ProgressWorkflow.OcrOnly);
            using var scope = _scopeFactory.CreateScope();
            var trackingService = scope.ServiceProvider.GetRequiredService<IOcrTrackingService>();

            var allPdfFiles = await FindPdfsForOcrOnlyAsync(date, trackingService, cancellationToken);

            if (allPdfFiles.Count == 0)
            {
                AddOcrOnlyLog($"No eligible PDFs found for {dateText}");

                lock (_sync)
                {
                    _state.OcrOnlyStatus = ExtractorStatus.Completed;
                    _state.LastOcrOnlySuccessfulRun = DateTime.UtcNow;
                    RefreshAggregateStatusLocked();
                }

                return;
            }

            AddOcrOnlyLog($"Found {allPdfFiles.Count} PDFs to process for {dateText}");

            var providerType = provider switch
            {
                OcrProvider.OpenAI => OcrProviderType.OpenAI,
                OcrProvider.LlamaCpp => OcrProviderType.LlamaCpp,
                _ => OcrProviderType.LlamaCpp
            };

            var ocrService = CreateOcrService(provider, scope.ServiceProvider);
            var ocrResult = await ocrService.ExtractTextAsync(allPdfFiles, date, providerType, cancellationToken);

            lock (_sync)
            {
                _state.LastOcrOnlySucceeded = ocrResult.SucceededCount;
                _state.LastOcrOnlyFailed = ocrResult.FailedCount;
                _state.OcrOnlyStatus = ExtractorStatus.Completed;
                _state.LastOcrOnlySuccessfulRun = DateTime.UtcNow;
                RefreshAggregateStatusLocked();
            }

            AddOcrOnlyLog($"OCR: {ocrResult.SucceededCount} succeeded, {ocrResult.FailedCount} failed");
            AddOcrOnlyLog("Completed");
        }
        catch (OperationCanceledException)
        {
            lock (_sync)
            {
                _state.OcrOnlyStatus = ExtractorStatus.Idle;
                RefreshAggregateStatusLocked();
            }

            AddOcrOnlyLog("Cancelled");
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _state.OcrOnlyStatus = ExtractorStatus.Error;
                _state.LastOcrOnlyError = ex.Message;
                _state.LastError = ex.Message;
                RefreshAggregateStatusLocked();
            }

            AddOcrOnlyLog($"Error: {ex.Message}");
            _logger.LogError(ex, "OCR-only failed");
        }
        finally
        {
            lock (_sync)
            {
                _ocrOnlyCts?.Dispose();
                _ocrOnlyCts = null;
                RefreshAggregateStatusLocked();
            }

            UnsubscribeProgressUpdates();
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Finds PDFs that need OCR processing for a given date.
    /// Uses tracking database as primary source of truth.
    /// </summary>
    private async Task<List<string>> FindPdfsForOcrOnlyAsync(
        DateTime queryDate,
        IOcrTrackingService trackingService,
        CancellationToken cancellationToken)
    {
        var allPdfFiles = new List<string>();
        var basePath = BaseDownloadPath;

        AddOcrOnlyLog($"Scanning base path: {basePath}");

        if (!Directory.Exists(basePath))
        {
            return allPdfFiles;
        }

        var dateDir = Path.Combine(basePath, queryDate.ToString("yyyy-MM-dd"));
        AddOcrOnlyLog($"Scanning date folder: {dateDir}");

        if (!Directory.Exists(dateDir))
        {
            return allPdfFiles;
        }

        var pdfDir = Path.Combine(dateDir, "PDFs");
        if (!Directory.Exists(pdfDir))
        {
            return allPdfFiles;
        }

        var pdfs = Directory.GetFiles(pdfDir, "*.pdf");
        foreach (var pdf in pdfs)
        {
            var shouldProcess = await HasPendingOcrPagesFromDbAsync(queryDate, pdf, trackingService, cancellationToken);
            if (shouldProcess)
            {
                allPdfFiles.Add(pdf);
            }
        }

        return allPdfFiles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// DB-first pending check. Falls back to TXT only when there is no DB history for the PDF.
    /// </summary>
    private async Task<bool> HasPendingOcrPagesFromDbAsync(
        DateTime executionDate,
        string pdfPath,
        IOcrTrackingService trackingService,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await trackingService.GetPdfStatusSummaryAsync(executionDate, pdfPath, cancellationToken);

            // Primary source: DB when there is any historical data.
            if (summary.total > 0)
            {
                var hasPendingInDb = summary.pending > 0;
                if (!hasPendingInDb)
                {
                    AddOcrOnlyLog($"PDF já concluído no banco: {Path.GetFileName(pdfPath)} -> pulado");
                }

                return hasPendingInDb;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking DB pending pages for {PdfPath}; fallback to TXT heuristics", pdfPath);
        }

        // Legacy fallback for files without DB records.
        var txtPath = GetTxtPathForPdf(pdfPath);
        return HasPendingOcrPagesFromTxt(pdfPath, txtPath);
    }

    public void CancelExtraction()
    {
        _extractionCts?.Cancel();
        AddExtractionLog("Cancellation requested...");
    }

    public void CancelOcrOnly()
    {
        _ocrOnlyCts?.Cancel();
        AddOcrOnlyLog("Cancellation requested...");
    }

    /// <summary>
    /// Backward-compatible cancel method (cancels both workflows).
    /// </summary>
    public void Cancel()
    {
        CancelExtraction();
        CancelOcrOnly();
    }

    /// <summary>
    /// Changes the OCR provider for future runs.
    /// </summary>
    public void SetOcrProvider(OcrProvider provider)
    {
        OcrProvider oldProvider;

        lock (_sync)
        {
            oldProvider = _state.SelectedOcrProvider;
            _state.SelectedOcrProvider = provider;
        }

        AddExtractionLog($"OCR provider changed from {oldProvider} to {provider}");
        AddOcrOnlyLog($"OCR provider changed from {oldProvider} to {provider}");
    }

    private static bool HasPendingOcrPagesFromTxt(string pdfPath, string txtPath)
    {
        if (!File.Exists(txtPath))
        {
            return true;
        }

        var imagesDir = GetImagesDirectoryForPdf(pdfPath);
        if (!Directory.Exists(imagesDir))
        {
            return true;
        }

        var imageCount = Directory.GetFiles(imagesDir, "*.png").Length;
        if (imageCount == 0)
        {
            return true;
        }

        var processedPages = GetProcessedPagesFromTxt(txtPath);
        return processedPages.Count < imageCount;
    }

    private static HashSet<int> GetProcessedPagesFromTxt(string txtPath)
    {
        var pages = new HashSet<int>();

        if (!File.Exists(txtPath))
        {
            return pages;
        }

        var text = File.ReadAllText(txtPath);
        foreach (Match match in Regex.Matches(text, @"---\s*Página\s*(\d+)\s*---", RegexOptions.IgnoreCase))
        {
            if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var pageNumber))
            {
                pages.Add(pageNumber);
            }
        }

        return pages;
    }

    private static string GetImagesDirectoryForPdf(string pdfPath)
    {
        var pdfDir = Path.GetDirectoryName(pdfPath);
        var pdfFileName = Path.GetFileNameWithoutExtension(pdfPath);

        if (pdfDir != null && pdfDir.EndsWith("PDFs", StringComparison.OrdinalIgnoreCase))
        {
            var baseDir = Path.GetDirectoryName(pdfDir);
            return Path.Combine(baseDir!, "images", pdfFileName);
        }

        return Path.Combine(pdfDir ?? string.Empty, "images", pdfFileName);
    }

    private static string GetTxtPathForPdf(string pdfPath)
    {
        var pdfDir = Path.GetDirectoryName(pdfPath);
        var pdfFileName = Path.GetFileNameWithoutExtension(pdfPath);

        if (pdfDir != null && pdfDir.EndsWith("PDFs", StringComparison.OrdinalIgnoreCase))
        {
            var baseDir = Path.GetDirectoryName(pdfDir);
            return Path.Combine(baseDir!, "TXTs", pdfFileName + ".txt");
        }

        return Path.ChangeExtension(pdfPath, ".txt");
    }

    private void SubscribeProgressUpdates()
    {
        var shouldSubscribe = false;

        lock (_sync)
        {
            if (_progressSubscriptionCount == 0)
            {
                shouldSubscribe = true;
            }

            _progressSubscriptionCount++;
        }

        if (shouldSubscribe)
        {
            ExtractionProgress.ProgressReported += OnProgressReported;
        }
    }

    private void UnsubscribeProgressUpdates()
    {
        var shouldUnsubscribe = false;

        lock (_sync)
        {
            if (_progressSubscriptionCount == 0)
            {
                return;
            }

            _progressSubscriptionCount--;
            if (_progressSubscriptionCount == 0)
            {
                shouldUnsubscribe = true;
            }
        }

        if (shouldUnsubscribe)
        {
            ExtractionProgress.ProgressReported -= OnProgressReported;
        }
    }

    private void OnProgressReported(ProgressUpdate update)
    {
        switch (update.Workflow)
        {
            case ProgressWorkflow.Extraction:
                AddExtractionLog(update.Message);
                break;
            case ProgressWorkflow.OcrOnly:
                AddOcrOnlyLog(update.Message);
                break;
            default:
                AddSharedLog(update.Message);
                break;
        }
    }

    private void RefreshAggregateStatusLocked()
    {
        if (_state.ExtractionStatus == ExtractorStatus.Running)
        {
            _state.Status = ExtractorStatus.Running;
            return;
        }

        if (_state.OcrOnlyStatus == ExtractorStatus.Running)
        {
            _state.Status = ExtractorStatus.RunningOcr;
            return;
        }

        if (_state.ExtractionStatus == ExtractorStatus.Error || _state.OcrOnlyStatus == ExtractorStatus.Error)
        {
            _state.Status = ExtractorStatus.Error;
            return;
        }

        if (_state.ExtractionStatus == ExtractorStatus.Completed || _state.OcrOnlyStatus == ExtractorStatus.Completed)
        {
            _state.Status = ExtractorStatus.Completed;
            return;
        }

        _state.Status = ExtractorStatus.Idle;
    }

    private void AddExtractionLog(string message)
    {
        AddLogToList(_state.ExtractionLogs, $"[Extraction] {message}");
    }

    private void AddOcrOnlyLog(string message)
    {
        AddLogToList(_state.OcrOnlyLogs, $"[OCR Only] {message}");
    }

    private void AddSharedLog(string message)
    {
        AddLogToList(_state.ExtractionLogs, $"[Shared] {message}", notify: false);
        AddLogToList(_state.OcrOnlyLogs, $"[Shared] {message}");
    }

    private void AddLogToList(List<string> targetLogs, string message, bool notify = true)
    {
        var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";

        lock (_sync)
        {
            targetLogs.Add(logEntry);

            // Keep only last 500 logs per panel
            if (targetLogs.Count > 500)
            {
                targetLogs.RemoveAt(0);
            }
        }

        if (notify)
        {
            StateChanged?.Invoke();
        }
    }
}
