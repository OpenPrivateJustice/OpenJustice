using System.Threading;

namespace OpenJustice.BrazilExtractor.Services.Progress;

public enum ProgressWorkflow
{
    Unknown,
    Extraction,
    OcrOnly
}

public sealed record ProgressUpdate(ProgressWorkflow Workflow, string Message);

/// <summary>
/// Shared progress event hub for extraction workflow (download + OCR).
/// Supports workflow scoping so UI can split logs by operation.
/// </summary>
public static class ExtractionProgress
{
    private static readonly AsyncLocal<ProgressWorkflow> CurrentWorkflow = new();

    public static event Action<ProgressUpdate>? ProgressReported;

    public static IDisposable BeginScope(ProgressWorkflow workflow)
    {
        var previous = CurrentWorkflow.Value;
        CurrentWorkflow.Value = workflow;
        return new ProgressScope(previous);
    }

    public static void Report(string message)
    {
        ProgressReported?.Invoke(new ProgressUpdate(CurrentWorkflow.Value, message));
    }

    private sealed class ProgressScope : IDisposable
    {
        private readonly ProgressWorkflow _previous;
        private bool _disposed;

        public ProgressScope(ProgressWorkflow previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CurrentWorkflow.Value = _previous;
            _disposed = true;
        }
    }
}
