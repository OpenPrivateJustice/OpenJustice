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
/// OCR extraction service using OpenAI Vision API.
/// Converts PDF pages to PNG and sends each page as image input to Chat Completions.
/// </summary>
public class OpenAiVisionOcrService : IOcrExtractionService
{
    private readonly BrazilExtractorOptions _options;
    private readonly ILogger<OpenAiVisionOcrService> _logger;
    private readonly IOcrTrackingService _trackingService;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenAiVisionOcrService(
        IOptions<BrazilExtractorOptions> options,
        ILogger<OpenAiVisionOcrService> logger,
        IOcrTrackingService trackingService,
        IHttpClientFactory? httpClientFactory = null)
    {
        _options = options.Value;
        _logger = logger;
        _trackingService = trackingService;
        _apiKey = _options.OpenAiApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        _httpClient = httpClientFactory?.CreateClient("OpenAiVision") ?? new HttpClient();
        _httpClient.BaseAddress = new Uri("https://api.openai.com");
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(30, _options.LlamaCppRequestTimeoutSeconds));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("OpenAI API key not configured. Set OPENAI_API_KEY or BrazilExtractor:OpenAiApiKey.");
        }
        else
        {
            _logger.LogInformation("OpenAI Vision OCR initialized with model: {Model}", _options.OpenAiVisionModel);
        }
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

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            result.Results = pdfFiles.Select(f => new OcrExtractionResult
            {
                PdfFilePath = f,
                Succeeded = false,
                ErrorMessage = "OpenAI API key not configured",
                FailureReason = OcrFailureReason.OcrProviderError
            }).ToList();
            result.FailedCount = pdfFiles.Count;
            return result;
        }

        foreach (var pdfPath in pdfFiles)
        {
            try
            {
                var ocrResult = await ExtractTextFromPdfAsync(pdfPath, executionDate, provider, cancellationToken);
                result.Results.Add(ocrResult);

                if (ocrResult.Succeeded)
                {
                    result.SucceededCount++;
                    result.TotalCharacters += ocrResult.CharacterCount;
                    result.TotalPages += ocrResult.PageCount;
                }
                else
                {
                    result.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Results.Add(new OcrExtractionResult
                {
                    PdfFilePath = pdfPath,
                    Succeeded = false,
                    ErrorMessage = ex.Message,
                    FailureReason = OcrFailureReason.Unknown
                });
                _logger.LogError(ex, "OpenAI OCR error for {Path}", pdfPath);
            }
        }

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
            ExtractionProgress.Report($"[OCR][OpenAI] Iniciando processamento: {pdfName}");

            var imagePaths = await ConvertPdfToImagesAsync(pdfPath, cancellationToken);
            result.PageCount = imagePaths.Count;

            if (imagePaths.Count == 0)
            {
                result.Succeeded = false;
                result.ErrorMessage = "Failed to convert PDF to images";
                result.FailureReason = OcrFailureReason.ImageConversionError;
                return result;
            }

            var txtPath = GetOutputPath(pdfPath);
            if (!File.Exists(txtPath))
            {
                await File.WriteAllTextAsync(txtPath, string.Empty, Encoding.UTF8, cancellationToken);
                ExtractionProgress.Report($"[OCR][OpenAI] {pdfName}: TXT inicializado em {txtPath}");
            }
            else
            {
                ExtractionProgress.Report($"[OCR][OpenAI] {pdfName}: retomando OCR a partir do TXT existente");
            }

            var pendingPages = await _trackingService.GetPendingPageNumbersAsync(
                executionDate,
                pdfPath,
                imagePaths.Count,
                cancellationToken);

            var txtProcessedPages = await GetProcessedPagesFromTxtAsync(txtPath, cancellationToken);
            var skippedPages = 0;
            var hasExistingContent = new FileInfo(txtPath).Length > 0;

            for (int i = 0; i < imagePaths.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var pageNumber = i + 1;
                var isProcessedInDb = !pendingPages.Contains(pageNumber);
                var isProcessedInTxt = txtProcessedPages.Contains(pageNumber);

                if (isProcessedInDb)
                {
                    skippedPages++;
                    ExtractionProgress.Report($"[OCR][OpenAI] {pdfName}: página {pageNumber} já processada no banco, pulando");
                    continue;
                }

                if (isProcessedInTxt)
                {
                    var compatibilityRecord = await _trackingService.CreatePageRecordAsync(
                        executionDate,
                        pdfPath,
                        pageNumber,
                        provider,
                        cancellationToken);

                    await _trackingService.RecordPageSuccessAsync(
                        compatibilityRecord.Id,
                        null,
                        0,
                        cancellationToken);

                    skippedPages++;
                    ExtractionProgress.Report($"[OCR][OpenAI] {pdfName}: página {pageNumber} já estava no TXT (compatibilidade), pulando");
                    continue;
                }

                ExtractionProgress.Report($"[OCR][OpenAI] {pdfName}: enviando página {pageNumber}/{imagePaths.Count}");

                var record = await _trackingService.RecordPageStartedAsync(
                    executionDate,
                    pdfPath,
                    pageNumber,
                    provider,
                    cancellationToken);

                string? imageHash = null;
                try
                {
                    var imageBytes = await File.ReadAllBytesAsync(imagePaths[i], cancellationToken);
                    imageHash = ComputeSha256Hash(imageBytes);
                }
                catch
                {
                    // no-op: hash is optional
                }

                var pageText = await ExtractTextFromImageAsync(imagePaths[i], cancellationToken);
                var pageSection = !string.IsNullOrWhiteSpace(pageText)
                    ? $"--- Página {pageNumber} ---\n{pageText}"
                    : $"--- Página {pageNumber} ---\n[Failed to extract text]";

                if (hasExistingContent)
                {
                    pageSection = "\n\n" + pageSection;
                }

                await File.AppendAllTextAsync(txtPath, pageSection, Encoding.UTF8, cancellationToken);
                hasExistingContent = true;

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    await _trackingService.RecordPageSuccessAsync(record.Id, imageHash, pageText.Length, cancellationToken);
                }
                else
                {
                    await _trackingService.RecordPageFailedAsync(record.Id, "OCR returned empty text", cancellationToken);
                }
            }

            if (skippedPages > 0)
            {
                ExtractionProgress.Report($"[OCR][OpenAI] {pdfName}: retomada concluída, {skippedPages} páginas já prontas foram puladas");
            }

            result.ExtractedText = await File.ReadAllTextAsync(txtPath, cancellationToken);
            result.CharacterCount = result.ExtractedText.Length;

            if (result.CharacterCount == 0)
            {
                result.Succeeded = false;
                result.ErrorMessage = "OpenAI vision OCR returned empty text";
                result.FailureReason = OcrFailureReason.OcrProviderError;
                return result;
            }

            ExtractionProgress.Report($"[OCR][OpenAI] {pdfName}: TXT salvo incrementalmente em {txtPath}");
            result.Succeeded = true;
            return result;
        }
        catch (Exception ex)
        {
            result.Succeeded = false;
            result.ErrorMessage = ex.Message;
            result.FailureReason = OcrFailureReason.Unknown;
            _logger.LogError(ex, "Error processing PDF with OpenAI Vision: {Path}", pdfPath);
            return result;
        }
    }

    private async Task<string?> ExtractTextFromImageAsync(string imagePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return null;
        }

        var imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        var imageBase64 = Convert.ToBase64String(imageBytes);

        var request = new
        {
            model = _options.OpenAiVisionModel,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Extract ALL text from this court document image. Return ONLY the extracted text, without summaries or explanations. Preserve Portuguese text exactly." },
                        new { type = "image_url", image_url = new { url = $"data:image/png;base64,{imageBase64}" } }
                    }
                }
            },
            max_tokens = _options.LlamaCppMaxTokens,
            temperature = 0.1
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json")
        };

        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("OpenAI Vision request failed: {Status} - {Error}", response.StatusCode, error);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var chatResponse = JsonSerializer.Deserialize<LlamaCppChatResponse>(body, _jsonOptions);
        return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
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

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pdftoppm",
                Arguments = $"-png -r 150 \"{pdfPath}\" \"{Path.Combine(imagesDir, "page")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var stdErr = await process.StandardError.ReadToEndAsync();
            _logger.LogError("pdftoppm failed for {PdfPath}: {StdErr}", pdfPath, stdErr);
            return new List<string>();
        }

        return Directory.GetFiles(imagesDir, "*.png")
            .OrderBy(GetPageNumberFromImagePath)
            .ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private static string GetOutputPath(string pdfPath)
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
