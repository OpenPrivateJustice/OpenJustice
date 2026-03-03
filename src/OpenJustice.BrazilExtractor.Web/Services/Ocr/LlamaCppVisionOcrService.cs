using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Data;
using OpenJustice.BrazilExtractor.Models;
using OpenJustice.BrazilExtractor.Services.Progress;
using OpenJustice.BrazilExtractor.Services.Tracking;

namespace OpenJustice.BrazilExtractor.Services.Ocr;

/// <summary>
/// OCR extraction service using llama.cpp server with vision capabilities.
/// Converts PDF to images and sends to vision model via Chat Completions API.
/// </summary>
public class LlamaCppVisionOcrService : IOcrExtractionService
{
    private readonly BrazilExtractorOptions _options;
    private readonly ILogger<LlamaCppVisionOcrService> _logger;
    private readonly IOcrTrackingService _trackingService;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public LlamaCppVisionOcrService(
        IOptions<BrazilExtractorOptions> options,
        ILogger<LlamaCppVisionOcrService> logger,
        IOcrTrackingService trackingService,
        IHttpClientFactory? httpClientFactory = null)
    {
        _options = options.Value;
        _logger = logger;
        _trackingService = trackingService;

        _httpClient = httpClientFactory?.CreateClient("LlamaCpp") ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.LlamaCppRequestTimeoutSeconds);

        var baseUrl = _options.LlamaCppBaseUrl.TrimEnd('/');
        _httpClient.BaseAddress = new Uri(baseUrl);

        if (!string.IsNullOrEmpty(_options.LlamaCppApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.LlamaCppApiKey}");
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogInformation("LlamaCpp Vision OCR initialized with base URL: {BaseUrl}, model: {Model}",
            _options.LlamaCppBaseUrl, _options.LlamaCppVisionModel);
    }

    public async Task<OcrExtractionBatchResult> ExtractTextAsync(
        IEnumerable<string> pdfFilePaths,
        DateTime executionDate,
        OcrProviderType provider,
        CancellationToken cancellationToken = default)
    {
        var result = new OcrExtractionBatchResult();
        var pdfFiles = pdfFilePaths.ToList();
        result.TotalCount = pdfFiles.Count;

        _logger.LogInformation("Starting LlamaCpp Vision OCR batch processing for {Count} PDF files on {Date}",
            pdfFiles.Count, executionDate.ToString("yyyy-MM-dd"));

        foreach (var pdfPath in pdfFiles)
        {
            try
            {
                _logger.LogDebug("Processing PDF: {Path}", pdfPath);

                var ocrResult = await ExtractTextFromPdfAsync(pdfPath, executionDate, provider, cancellationToken);
                result.Results.Add(ocrResult);

                if (ocrResult.Succeeded)
                {
                    result.SucceededCount++;
                    result.TotalCharacters += ocrResult.CharacterCount;
                    result.TotalPages += ocrResult.PageCount;
                    _logger.LogInformation("OCR succeeded for {Path}: {CharCount} characters extracted",
                        pdfPath, ocrResult.CharacterCount);
                }
                else
                {
                    result.FailedCount++;
                    _logger.LogWarning("OCR failed for {Path}: {Reason}", pdfPath, ocrResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                var failedResult = new OcrExtractionResult
                {
                    PdfFilePath = pdfPath,
                    Succeeded = false,
                    ErrorMessage = ex.Message
                };
                result.Results.Add(failedResult);
                _logger.LogError(ex, "OCR error for {Path}", pdfPath);
            }
        }

        _logger.LogInformation("LlamaCpp Vision OCR batch complete: {Succeeded}/{Total} succeeded, {Chars} total characters",
            result.SucceededCount, pdfFiles.Count, result.TotalCharacters);

        return result;
    }

    public Task<OcrExtractionResult> ExtractTextAsync(
        string pdfFilePath,
        DateTime executionDate,
        OcrProviderType provider,
        CancellationToken cancellationToken = default)
    {
        return ExtractTextFromPdfAsync(pdfFilePath, executionDate, provider, cancellationToken);
    }

    private async Task<OcrExtractionResult> ExtractTextFromPdfAsync(
        string pdfPath,
        DateTime executionDate,
        OcrProviderType provider,
        CancellationToken cancellationToken)
    {
        var result = new OcrExtractionResult
        {
            PdfFilePath = pdfPath
        };

        try
        {
            var pdfName = Path.GetFileName(pdfPath);
            ExtractionProgress.Report($"[OCR] Iniciando processamento: {pdfName}");
            ExtractionProgress.Report($"[OCR] Convertendo PDF em imagens PNG: {pdfName}");

            var imagePaths = await ConvertPdfToImagesAsync(pdfPath, cancellationToken);
            result.PageCount = imagePaths.Count;

            if (imagePaths.Count == 0)
            {
                result.Succeeded = false;
                result.ErrorMessage = "Failed to convert PDF to images";
                result.FailureReason = OcrFailureReason.ImageConversionError;
                ExtractionProgress.Report($"[OCR] Falha ao converter PDF em imagens: {pdfName}");
                return result;
            }

            ExtractionProgress.Report($"[OCR] {pdfName}: {imagePaths.Count} imagens geradas");

            var txtPath = GetOutputPath(pdfPath);
            if (!File.Exists(txtPath))
            {
                await File.WriteAllTextAsync(txtPath, string.Empty, Encoding.UTF8, cancellationToken);
                ExtractionProgress.Report($"[OCR] {pdfName}: TXT inicializado em {txtPath}");
            }
            else
            {
                ExtractionProgress.Report($"[OCR] {pdfName}: retomando OCR a partir do TXT existente");
            }

            var pendingPages = await _trackingService.GetPendingPageNumbersAsync(
                executionDate,
                pdfPath,
                imagePaths.Count,
                cancellationToken);

            var txtProcessedPages = await GetProcessedPagesFromTxtAsync(txtPath, cancellationToken);
            var skippedPages = 0;
            var maxPages = _options.LlamaCppMaxPagesPerPdf;
            var pagesToProcess = maxPages > 0 ? Math.Min(imagePaths.Count, maxPages) : imagePaths.Count;
            var hasExistingContent = new FileInfo(txtPath).Length > 0;

            for (int i = 0; i < pagesToProcess; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var imagePath = imagePaths[i];
                var pageNum = i + 1;

                var isProcessedInDb = !pendingPages.Contains(pageNum);
                var isProcessedInTxt = txtProcessedPages.Contains(pageNum);

                if (isProcessedInDb)
                {
                    skippedPages++;
                    ExtractionProgress.Report($"[OCR] {pdfName}: página {pageNum} já processada no banco, pulando");
                    continue;
                }

                if (isProcessedInTxt)
                {
                    var compatibilityRecord = await _trackingService.CreatePageRecordAsync(
                        executionDate,
                        pdfPath,
                        pageNum,
                        provider,
                        cancellationToken);

                    await _trackingService.RecordPageSuccessAsync(
                        compatibilityRecord.Id,
                        null,
                        0,
                        cancellationToken);

                    skippedPages++;
                    ExtractionProgress.Report($"[OCR] {pdfName}: página {pageNum} já estava no TXT (compatibilidade), pulando");
                    continue;
                }

                ExtractionProgress.Report($"[OCR] {pdfName}: enviando página {pageNum}/{pagesToProcess} para llama.cpp");

                var record = await _trackingService.RecordPageStartedAsync(
                    executionDate,
                    pdfPath,
                    pageNum,
                    provider,
                    cancellationToken);

                string? imageHash = null;
                try
                {
                    var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
                    imageHash = ComputeSha256Hash(imageBytes);
                }
                catch
                {
                    // no-op: hash is optional
                }

                var pageText = await ExtractTextFromImageAsync(imagePath, pageNum, cancellationToken);
                var pageSection = !string.IsNullOrWhiteSpace(pageText)
                    ? $"--- Página {pageNum} ---\n{pageText}"
                    : $"--- Página {pageNum} ---\n[Failed to extract text]";

                if (hasExistingContent)
                {
                    pageSection = "\n\n" + pageSection;
                }

                await File.AppendAllTextAsync(txtPath, pageSection, Encoding.UTF8, cancellationToken);
                hasExistingContent = true;

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    await _trackingService.RecordPageSuccessAsync(record.Id, imageHash, pageText.Length, cancellationToken);
                    ExtractionProgress.Report($"[OCR] {pdfName}: página {pageNum} processada ({pageText.Length} chars) e adicionada ao TXT");
                }
                else
                {
                    await _trackingService.RecordPageFailedAsync(record.Id, "OCR returned empty text", cancellationToken);
                    ExtractionProgress.Report($"[OCR] {pdfName}: falha na página {pageNum}; marcador adicionado ao TXT");
                }
            }

            if (maxPages > 0 && imagePaths.Count > maxPages)
            {
                var marker = $"--- Páginas {maxPages + 1} a {imagePaths.Count} (não processadas) ---";
                var currentText = await File.ReadAllTextAsync(txtPath, cancellationToken);
                if (!currentText.Contains(marker, StringComparison.Ordinal))
                {
                    var notProcessedSection = $"\n\n{marker}\n[Limitado a {maxPages} páginas por configuração]";
                    await File.AppendAllTextAsync(txtPath, notProcessedSection, Encoding.UTF8, cancellationToken);
                }

                for (int pageNum = maxPages + 1; pageNum <= imagePaths.Count; pageNum++)
                {
                    await _trackingService.RecordPageSkippedAsync(executionDate, pdfPath, pageNum, provider, cancellationToken);
                }
            }

            if (skippedPages > 0)
            {
                ExtractionProgress.Report($"[OCR] {pdfName}: retomada concluída, {skippedPages} páginas já prontas foram puladas");
            }

            result.ExtractedText = await File.ReadAllTextAsync(txtPath, cancellationToken);
            result.CharacterCount = result.ExtractedText.Length;

            if (result.CharacterCount > 0)
            {
                result.Succeeded = true;
                ExtractionProgress.Report($"[OCR] {pdfName}: TXT salvo incrementalmente em {txtPath}");
            }
            else
            {
                result.Succeeded = false;
                result.ErrorMessage = "Vision OCR failed to extract text from PDF";
                result.FailureReason = OcrFailureReason.OcrProviderError;
                ExtractionProgress.Report($"[OCR] {pdfName}: OCR sem texto extraído");
            }
        }
        catch (Exception ex)
        {
            result.Succeeded = false;
            result.ErrorMessage = $"Error processing PDF: {ex.Message}";
            result.FailureReason = OcrFailureReason.Unknown;
            _logger.LogError(ex, "Error processing PDF: {Path}", pdfPath);
            ExtractionProgress.Report($"[OCR] Falha em {Path.GetFileName(pdfPath)}: {ex.Message}");
        }

        return result;
    }

    private async Task<List<string>> ConvertPdfToImagesAsync(string pdfPath, CancellationToken cancellationToken)
    {
        var pdfDir = Path.GetDirectoryName(pdfPath);
        var pdfFileName = Path.GetFileNameWithoutExtension(pdfPath);

        string imagesDir;
        if (pdfDir != null && pdfDir.EndsWith("PDFs", StringComparison.OrdinalIgnoreCase))
        {
            var baseDir = Path.GetDirectoryName(pdfDir);
            imagesDir = Path.Combine(baseDir!, "images", pdfFileName);
        }
        else
        {
            imagesDir = Path.Combine(Path.GetDirectoryName(pdfPath)!, "images", pdfFileName);
        }

        if (Directory.Exists(imagesDir))
        {
            Directory.Delete(imagesDir, recursive: true);
        }

        Directory.CreateDirectory(imagesDir);

        var outputPrefix = Path.Combine(imagesDir, "page");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pdftoppm",
                Arguments = $"-png -r 150 \"{pdfPath}\" \"{outputPrefix}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            await process.WaitForExitAsync(cancellationToken);

            var stdOut = await process.StandardOutput.ReadToEndAsync();
            var stdErr = await process.StandardError.ReadToEndAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError(
                    "pdftoppm failed for {PdfPath}. ExitCode: {ExitCode}. StdOut: {StdOut}. StdErr: {StdErr}",
                    pdfPath,
                    process.ExitCode,
                    stdOut,
                    stdErr);

                ExtractionProgress.Report($"[OCR] pdftoppm falhou em {Path.GetFileName(pdfPath)} (exit code {process.ExitCode})");
                return new List<string>();
            }

            return Directory.GetFiles(imagesDir, "*.png")
                .OrderBy(GetPageNumberFromImagePath)
                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert PDF to images");
            ExtractionProgress.Report($"[OCR] Erro ao converter {Path.GetFileName(pdfPath)} em imagens: {ex.Message}");
            return new List<string>();
        }
    }

    private static int GetPageNumberFromImagePath(string imagePath)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);
        var lastDash = fileNameWithoutExtension.LastIndexOf('-');

        if (lastDash >= 0 && int.TryParse(fileNameWithoutExtension[(lastDash + 1)..], out var pageNumber))
        {
            return pageNumber;
        }

        return int.MaxValue;
    }

    private static async Task<HashSet<int>> GetProcessedPagesFromTxtAsync(string txtPath, CancellationToken cancellationToken)
    {
        var processedPages = new HashSet<int>();

        if (!File.Exists(txtPath))
        {
            return processedPages;
        }

        var text = await File.ReadAllTextAsync(txtPath, cancellationToken);
        foreach (Match match in Regex.Matches(text, @"---\s*Página\s*(\d+)\s*---", RegexOptions.IgnoreCase))
        {
            if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var pageNumber))
            {
                processedPages.Add(pageNumber);
            }
        }

        return processedPages;
    }

    private async Task<string?> ExtractTextFromImageAsync(string imagePath, int pageNumber, CancellationToken cancellationToken)
    {
        try
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var imageBase64 = Convert.ToBase64String(imageBytes);

            var request = new
            {
                model = _options.LlamaCppVisionModel,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = "Extract ALL text from this court document image. Return ONLY the extracted text, without any descriptions, summaries, or explanations. Preserve the original Portuguese text exactly." },
                            new { type = "image_url", image_url = new { url = $"data:image/png;base64,{imageBase64}" } }
                        }
                    }
                },
                max_tokens = _options.LlamaCppMaxTokens,
                temperature = 0.1
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("LlamaCpp request failed: {Status} - {Error}", response.StatusCode, errorBody);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var chatResponse = JsonSerializer.Deserialize<LlamaCppChatResponse>(responseJson, _jsonOptions);
            return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from image for page {Page}", pageNumber);
            return null;
        }
    }

    private string GetOutputPath(string pdfPath)
    {
        var directory = Path.GetDirectoryName(pdfPath);
        var filename = Path.GetFileNameWithoutExtension(pdfPath);

        string txtPath;

        if (directory != null && directory.Contains("PDFs"))
        {
            var dateFolder = Path.GetDirectoryName(directory);
            txtPath = Path.Combine(dateFolder!, "TXTs", filename + ".txt");
        }
        else
        {
            txtPath = Path.ChangeExtension(pdfPath, ".txt");
        }

        var txtDir = Path.GetDirectoryName(txtPath);
        if (!string.IsNullOrEmpty(txtDir) && !Directory.Exists(txtDir))
        {
            Directory.CreateDirectory(txtDir);
        }

        return txtPath;
    }

    public async Task AppendFailureToLogAsync(string pdfPath, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] FAILED: {pdfPath} | Reason: {reason}{Environment.NewLine}";
            await File.AppendAllTextAsync(_options.OcrFailureLogPath, logEntry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write OCR failure log");
        }
    }

    private static string ComputeSha256Hash(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Response model for llama.cpp chat completions API.
/// </summary>
public class LlamaCppChatResponse
{
    public string? Id { get; set; }
    public string? Object { get; set; }
    public long Created { get; set; }
    public string? Model { get; set; }
    public List<LlamaCppChoice>? Choices { get; set; }
    public LlamaCppUsage? Usage { get; set; }
}

public class LlamaCppChoice
{
    public int Index { get; set; }
    public LlamaCppMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}

public class LlamaCppMessage
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public class LlamaCppUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}
