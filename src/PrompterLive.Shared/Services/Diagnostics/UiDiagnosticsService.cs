using Microsoft.Extensions.Logging;

namespace PrompterLive.Shared.Services.Diagnostics;

public sealed class UiDiagnosticsService(ILogger<UiDiagnosticsService> logger)
{
    private const string FatalTitle = "Unexpected Error";
    private const string FatalMessage = "PrompterLive hit an unexpected error. Try the screen again or return to the library.";
    private const string RecoverableTitle = "Action Failed";
    private const string FailureLogTemplate = "UI operation {Operation} failed.";
    private const string StartLogTemplate = "Starting UI operation {Operation}.";
    private const string SuccessLogTemplate = "Completed UI operation {Operation}.";
    private const string FatalLogTemplate = "Unhandled UI exception in {Operation}.";

    private readonly ILogger<UiDiagnosticsService> _logger = logger;

    public UiDiagnosticEntry? Current { get; private set; }

    public event EventHandler? Changed;

    public async Task<bool> RunAsync(
        string operation,
        string message,
        Func<Task> action,
        bool clearRecoverableOnSuccess = true)
    {
        _logger.LogInformation(StartLogTemplate, operation);

        try
        {
            await action();

            _logger.LogInformation(SuccessLogTemplate, operation);
            if (clearRecoverableOnSuccess && Current is { IsFatal: false })
            {
                Clear();
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("UI operation {Operation} was cancelled.", operation);
            throw;
        }
        catch (Exception exception)
        {
            ReportRecoverable(operation, message, exception, alreadyLogged: false);
            return false;
        }
    }

    public void ReportRecoverable(string operation, string message, Exception exception, bool alreadyLogged = true)
    {
        if (!alreadyLogged)
        {
            _logger.LogError(exception, FailureLogTemplate, operation);
        }

        SetCurrent(
            new UiDiagnosticEntry(
                Title: RecoverableTitle,
                Message: message,
                Operation: operation,
                Detail: exception.Message,
                IsFatal: false,
                OccurredAtUtc: DateTimeOffset.UtcNow));
    }

    public void ReportRecoverable(string operation, string message, string detail)
    {
        _logger.LogWarning("Recoverable UI issue in {Operation}: {Detail}", operation, detail);
        SetCurrent(
            new UiDiagnosticEntry(
                Title: RecoverableTitle,
                Message: message,
                Operation: operation,
                Detail: detail,
                IsFatal: false,
                OccurredAtUtc: DateTimeOffset.UtcNow));
    }

    public void ReportFatal(string operation, Exception exception)
    {
        _logger.LogCritical(exception, FatalLogTemplate, operation);
        SetCurrent(
            new UiDiagnosticEntry(
                Title: FatalTitle,
                Message: FatalMessage,
                Operation: operation,
                Detail: exception.Message,
                IsFatal: true,
                OccurredAtUtc: DateTimeOffset.UtcNow));
    }

    public void Clear()
    {
        if (Current is null)
        {
            return;
        }

        Current = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ClearRecoverable(string operation)
    {
        if (Current is not { IsFatal: false } current)
        {
            return;
        }

        if (!string.Equals(current.Operation, operation, StringComparison.Ordinal))
        {
            return;
        }

        Clear();
    }

    private void SetCurrent(UiDiagnosticEntry next)
    {
        if (Current == next)
        {
            return;
        }

        Current = next;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
