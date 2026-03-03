using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Models;
using OpenJustice.BrazilExtractor.Services.Progress;

namespace OpenJustice.BrazilExtractor.Services.Downloads;

/// <summary>
/// Implementation of PDF download service with collision-safe unique naming.
/// Uses deterministic filenames based on query date, URL hash, and sequence number.
/// Uses FileMode.CreateNew to prevent accidental overwrites.
/// </summary>
public class PdfDownloadService : IPdfDownloadService
{
    private readonly BrazilExtractorOptions _options;
    private readonly ILogger<PdfDownloadService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _downloadPath;

    public PdfDownloadService(
        IOptions<BrazilExtractorOptions> options,
        ILogger<PdfDownloadService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        // Ensure download path is configured and exists
        if (string.IsNullOrWhiteSpace(_options.DownloadPath))
        {
            throw new InvalidOperationException("DownloadPath configuration is required");
        }

        _downloadPath = Path.GetFullPath(_options.DownloadPath);
        
        if (!Directory.Exists(_downloadPath))
        {
            _logger.LogInformation("Creating download directory: {Path}", _downloadPath);
            Directory.CreateDirectory(_downloadPath);
        }

        _logger.LogInformation("PDF download service initialized. Download path: {Path}", _downloadPath);
    }

    public string DownloadPath => _downloadPath;

    public async Task<PdfDownloadBatchResult> DownloadBatchAsync(
        IReadOnlyList<TjgoPublicationPdfLink> pdfLinks,
        DateTime queryDate,
        CancellationToken cancellationToken = default)
    {
        var result = new PdfDownloadBatchResult
        {
            StartTime = DateTime.UtcNow,
            AttemptedCount = pdfLinks.Count
        };

        var succeededFiles = new List<string>();
        var failures = new List<PdfDownloadFailure>();

        // Create date-based folder structure: YYYY-MM-DD/{PDFs,TXTs}/
        var dateFolder = queryDate.ToString("yyyy-MM-dd");
        var pdfFolder = Path.Combine(_downloadPath, dateFolder, "PDFs");
        var txtFolder = Path.Combine(_downloadPath, dateFolder, "TXTs");
        
        Directory.CreateDirectory(pdfFolder);
        Directory.CreateDirectory(txtFolder);

        _logger.LogInformation(
            "Starting PDF batch download: {Count} files for query date {Date}",
            pdfLinks.Count, queryDate.ToString("yyyy-MM-dd"));
        ExtractionProgress.Report($"[Download] Iniciando download de {pdfLinks.Count} PDFs para {queryDate:yyyy-MM-dd}");

        for (int i = 0; i < pdfLinks.Count; i++)
        {
            var pdfLink = pdfLinks[i];

            try
            {
                // Generate unique filename using query date + URL hash + sequence
                var uniqueFilename = GenerateUniqueFilename(pdfLink, queryDate, i);
                var filePath = Path.Combine(pdfFolder, uniqueFilename);

                _logger.LogDebug(
                    "Downloading PDF {Index}/{Total}: {Url} -> {Filename}",
                    i + 1, pdfLinks.Count, pdfLink.NormalizedUrl, uniqueFilename);
                ExtractionProgress.Report($"[Download] ({i + 1}/{pdfLinks.Count}) Baixando {uniqueFilename}");

                // Download the file
                var downloadResult = await DownloadSingleFileAsync(
                    pdfLink.NormalizedUrl, 
                    filePath, 
                    cancellationToken);

                if (downloadResult.Success)
                {
                    succeededFiles.Add(filePath);
                    result.SucceededCount++;
                    _logger.LogDebug("Successfully downloaded: {Filename}", uniqueFilename);
                    ExtractionProgress.Report($"[Download] OK: {uniqueFilename}");
                }
                else
                {
                    failures.Add(PdfDownloadFailure.Create(
                        pdfLink.NormalizedUrl,
                        downloadResult.ErrorMessage ?? "Unknown error",
                        downloadResult.HttpStatusCode,
                        downloadResult.Exception));
                    result.FailedCount++;
                    _logger.LogWarning(
                        "Failed to download {Url}: {Error} (HTTP {Status})",
                        pdfLink.NormalizedUrl, 
                        downloadResult.ErrorMessage ?? "Unknown error",
                        downloadResult.HttpStatusCode?.ToString() ?? "N/A");
                    ExtractionProgress.Report($"[Download] FALHA: {uniqueFilename} ({downloadResult.ErrorMessage ?? "Unknown error"})");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Download cancelled for {Url}", pdfLink.NormalizedUrl);
                failures.Add(PdfDownloadFailure.Create(
                    pdfLink.NormalizedUrl,
                    "Download cancelled",
                    null,
                    "OperationCanceledException"));
                result.FailedCount++;
                ExtractionProgress.Report("[Download] Cancelado pelo usuário");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception downloading {Url}", pdfLink.NormalizedUrl);
                failures.Add(PdfDownloadFailure.Create(
                    pdfLink.NormalizedUrl,
                    ex.Message,
                    null,
                    ex.GetType().Name));
                result.FailedCount++;
                ExtractionProgress.Report($"[Download] Exceção no download: {ex.Message}");
            }
        }

        result.EndTime = DateTime.UtcNow;
        result.SucceededFiles = succeededFiles;
        result.Failures = failures;
        result.Success = result.FailedCount == 0;

        _logger.LogInformation(
            "PDF batch download completed: {Succeeded}/{Attempted} succeeded, {Failed} failed in {Duration:F2}s",
            result.SucceededCount,
            result.AttemptedCount,
            result.FailedCount,
            result.Duration.TotalSeconds);
        ExtractionProgress.Report($"[Download] Concluído: {result.SucceededCount}/{result.AttemptedCount} baixados, {result.FailedCount} falhas");

        return result;
    }

    /// <summary>
    /// Generates a unique filename using query date, URL hash, and sequence.
    /// Format: tjgo_{date}_{hash}_{sequence}.pdf
    /// </summary>
    private string GenerateUniqueFilename(TjgoPublicationPdfLink pdfLink, DateTime queryDate, int sequence)
    {
        // Create a hash of the URL for uniqueness
        var urlHash = ComputeUrlHash(pdfLink.NormalizedUrl);
        
        // Format: tjgo_2026-03-02_a1b2c3d4_001.pdf
        var dateStr = queryDate.ToString("yyyy-MM-dd");
        var sequenceStr = (sequence + 1).ToString("D3"); // 001, 002, etc.
        
        return $"tjgo_{dateStr}_{urlHash}_{sequenceStr}.pdf";
    }

    /// <summary>
    /// Computes a short hash of the URL for filename inclusion.
    /// </summary>
    private string ComputeUrlHash(string url)
    {
        // Use MD5 for a consistent hash, take first 8 characters
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return hashString[..8];
    }

    /// <summary>
    /// Downloads a single file with FileMode.CreateNew to prevent overwrites.
    /// </summary>
    private async Task<DownloadResult> DownloadSingleFileAsync(
        string url, 
        string filePath, 
        CancellationToken cancellationToken)
    {
        var result = new DownloadResult();

        try
        {
            // Check if file already exists (collision detection)
            if (File.Exists(filePath))
            {
                result.ErrorMessage = "File already exists (collision detected)";
                _logger.LogWarning("Collision detected for {FilePath}, skipping download", filePath);
                return result;
            }

            // Download with HTTP client
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            result.HttpStatusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                return result;
            }

            // Get content type to verify it's a PDF
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Content type for {Url} is {ContentType}, expected PDF",
                    url, contentType);
            }

            // Use FileMode.CreateNew to ensure atomic creation and prevent overwrites
            await using var fileStream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920, // 80KB buffer
                useAsync: true);

            await response.Content.CopyToAsync(fileStream);
            
            result.Success = true;
            _logger.LogDebug("Downloaded {Url} to {FilePath}", url, filePath);
        }
        catch (HttpRequestException ex)
        {
            result.Exception = ex.Message;
            result.ErrorMessage = $"HTTP error: {ex.Message}";
        }
        catch (IOException ex)
        {
            result.Exception = ex.Message;
            result.ErrorMessage = $"IO error: {ex.Message}";
            // Check if it's a file already exists error
            if (ex.HResult == unchecked((int)0x80070050)) // ERROR_FILE_EXISTS
            {
                result.ErrorMessage = "File already exists (IO collision)";
            }
        }
        catch (Exception ex)
        {
            result.Exception = ex.Message;
            result.ErrorMessage = $"Unexpected error: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Tracks result of a single file download.
    /// </summary>
    private class DownloadResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? HttpStatusCode { get; set; }
        public string? Exception { get; set; }
    }
}
