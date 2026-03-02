using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenJustice.BrazilExtractor.Configuration;
using OpenJustice.BrazilExtractor.Models;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;
using PdfPigCore = UglyToad.PdfPig;

namespace OpenJustice.BrazilExtractor.Services.Ocr;

/// <summary>
/// Tesseract-backed OCR service for extracting text from PDF files.
/// Uses PdfPig for text extraction with Tesseract OCR as fallback for image-based PDFs.
/// </summary>
public class TesseractOcrExtractionService : IOcrExtractionService
{
    private readonly BrazilExtractorOptions _options;
    private readonly ILogger<TesseractOcrExtractionService> _logger;
    private readonly Regex _replacementCharRegex = new(@"[\uFFFD\u0020\u0000]", RegexOptions.Compiled);
    private readonly object _failureLogLock = new();

    public TesseractOcrExtractionService(
        IOptions<BrazilExtractorOptions> options,
        ILogger<TesseractOcrExtractionService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Ensure output directory exists
        if (!Directory.Exists(_options.OcrOutputPath))
        {
            Directory.CreateDirectory(_options.OcrOutputPath);
        }

        // Log the failure log path for operator visibility
        _logger.LogInformation("OCR output path: {OcrPath}", _options.OcrOutputPath);
        _logger.LogInformation("OCR failure log path: {FailureLogPath}", _options.OcrFailureLogPath);
    }

    /// <inheritdoc />
    public async Task<OcrExtractionBatchResult> ExtractTextAsync(
        IEnumerable<string> pdfFilePaths,
        CancellationToken cancellationToken = default)
    {
        var pdfList = pdfFilePaths.ToList();
        var result = new OcrExtractionBatchResult
        {
            TotalCount = pdfList.Count,
            Results = new List<OcrExtractionResult>()
        };

        _logger.LogInformation("Starting OCR batch processing for {Count} PDF files", pdfList.Count);

        foreach (var pdfPath in pdfList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("OCR batch processing cancelled");
                break;
            }

            var extractionResult = await ExtractTextAsync(pdfPath, cancellationToken);
            result.Results.Add(extractionResult);

            if (extractionResult.Succeeded)
            {
                result.SucceededCount++;
                result.TotalPages += extractionResult.PageCount;
                result.TotalCharacters += extractionResult.CharacterCount;
                result.TotalEncodingReplacementChars += extractionResult.EncodingReplacementCharCount;
            }
            else
            {
                result.FailedCount++;
                
                // Append to failure log for operator visibility
                AppendFailureToLog(pdfPath, extractionResult);
                
                _logger.LogWarning("OCR failed for {PdfPath}: {Error}",
                    pdfPath, extractionResult.ErrorMessage);
            }
        }

        _logger.LogInformation(
            "OCR batch complete: {Succeeded}/{Total} succeeded, {Failed} failed, {Pages} total pages",
            result.SucceededCount, result.TotalCount, result.FailedCount, result.TotalPages);

        return result;
    }

    /// <inheritdoc />
    public async Task<OcrExtractionResult> ExtractTextAsync(
        string pdfFilePath,
        CancellationToken cancellationToken = default)
    {
        var result = new OcrExtractionResult
        {
            PdfFilePath = pdfFilePath
        };

        try
        {
            // Check if file exists
            if (!File.Exists(pdfFilePath))
            {
                result.Succeeded = false;
                result.ErrorMessage = $"PDF file not found: {pdfFilePath}";
                result.FailureReason = OcrFailureReason.FileNotFound;
                _logger.LogError("PDF file not found: {PdfPath}", pdfFilePath);
                return result;
            }

            // First try to extract text directly using PdfPig
            var (text, pageCount, extractedDirectly) = await ExtractTextDirectlyAsync(pdfFilePath, cancellationToken);

            result.PageCount = pageCount;

            // If no text was extracted directly (image-based PDF), try Tesseract OCR
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("No text extracted directly from {PdfPath}, attempting Tesseract OCR", pdfFilePath);
                
                // For now, mark as failed since Tesseract needs image rendering
                // In a full implementation, we would render the PDF pages to images
                result.Succeeded = false;
                result.ErrorMessage = "No text extracted - image-based PDF requires Tesseract OCR with image rendering";
                result.FailureReason = OcrFailureReason.TesseractError;
                _logger.LogWarning("Image-based PDF requires OCR rendering: {PdfPath}", pdfFilePath);
                return result;
            }

            // Normalize and clean the extracted text
            var extractedText = NormalizeText(text);

            result.ExtractedText = extractedText;
            result.CharacterCount = extractedText.Length;
            result.EncodingReplacementCharCount = CountReplacementCharacters(extractedText);
            result.Succeeded = true;

            // Save extracted text to output directory
            await SaveExtractedTextAsync(pdfFilePath, extractedText);

            _logger.LogDebug(
                "OCR succeeded for {PdfPath}: {Pages} pages, {Chars} characters, {Replacements} replacement chars (direct: {Direct})",
                pdfFilePath, result.PageCount, result.CharacterCount, result.EncodingReplacementCharCount, extractedDirectly);
        }
        catch (Exception ex)
        {
            result.Succeeded = false;
            result.ErrorMessage = $"OCR processing failed: {ex.Message}";
            result.FailureReason = DetermineFailureReason(ex);
            _logger.LogError(ex, "OCR processing failed for {PdfPath}", pdfFilePath);
        }

        return result;
    }

    /// <summary>
    /// Attempts to extract text directly from PDF using PdfPig.
    /// </summary>
    private async Task<(string text, int pageCount, bool extractedDirectly)> ExtractTextDirectlyAsync(
        string pdfPath,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = PdfPigCore.PdfDocument.Open(pdfPath);
                var pageCount = document.NumberOfPages;
                var allText = new StringBuilder();

                foreach (var page in document.GetPages())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var pageText = page.Text;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        allText.AppendLine(pageText);
                    }
                }

                var text = allText.ToString();
                var extractedDirectly = !string.IsNullOrWhiteSpace(text);

                return (text, pageCount, extractedDirectly);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract text directly from PDF: {PdfPath}", pdfPath);
                return (string.Empty, 0, false);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Normalizes extracted text to UTF-8 with stable line endings.
    /// </summary>
    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Replace null bytes
        text = text.Replace("\0", string.Empty);

        // Normalize line endings to Unix style (will be written with appropriate encoding)
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Trim excessive whitespace but preserve paragraph structure
        text = Regex.Replace(text, @"[ \t]+", " "); // Multiple spaces to single
        text = Regex.Replace(text, @"\n{3,}", "\n\n"); // Multiple newlines to double

        return text.Trim();
    }

    /// <summary>
    /// Counts encoding replacement characters in text.
    /// </summary>
    private int CountReplacementCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return _replacementCharRegex.Matches(text).Count;
    }

    /// <summary>
    /// Saves extracted text to the same directory as the PDF with same base filename.
    /// Uses Path.ChangeExtension to produce .txt from .pdf
    /// </summary>
    private async Task SaveExtractedTextAsync(string pdfPath, string text)
    {
        // Use Path.ChangeExtension to create same-base .txt filename
        var txtPath = Path.ChangeExtension(pdfPath, ".txt");

        // Write the extracted text to the same directory as the PDF
        await File.WriteAllTextAsync(txtPath, text, Encoding.UTF8);
        
        _logger.LogDebug("Saved extracted text to: {OutputPath}", txtPath);
    }

    /// <summary>
    /// Appends a failure entry to the OCR failure log file.
    /// Each entry includes: timestamp, PDF path, language, reason, exception type.
    /// </summary>
    private void AppendFailureToLog(string pdfPath, OcrExtractionResult result)
    {
        try
        {
            lock (_failureLogLock)
            {
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                var reason = result.FailureReason?.ToString() ?? "Unknown";
                var errorMessage = result.ErrorMessage ?? "No error message";
                var language = _options.OcrLanguage;

                var logEntry = $"[{timestamp}] PDF: {pdfPath} | Language: {language} | Reason: {reason} | Error: {errorMessage}{Environment.NewLine}";

                // Ensure the log file directory exists
                var logDir = Path.GetDirectoryName(_options.OcrFailureLogPath);
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                File.AppendAllText(_options.OcrFailureLogPath, logEntry);
                _logger.LogDebug("Appended OCR failure to log: {LogPath}", _options.OcrFailureLogPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append OCR failure to log for {PdfPath}", pdfPath);
        }
    }

    /// <summary>
    /// Determines the failure reason from an exception.
    /// </summary>
    private OcrFailureReason DetermineFailureReason(Exception ex)
    {
        if (ex is FileNotFoundException)
        {
            return OcrFailureReason.FileNotFound;
        }

        if (ex is TesseractException)
        {
            return OcrFailureReason.TesseractError;
        }

        // Check for PDF errors
        if (ex.Message.Contains("pdf", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("corrupt", StringComparison.OrdinalIgnoreCase))
        {
            return OcrFailureReason.CorruptPdf;
        }

        return OcrFailureReason.Unknown;
    }
}
