using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PrompterLive.Shared.Services;

namespace PrompterLive.Shared.Services.Diagnostics;

public sealed class ShellDiagnosticsInterop(
    IJSRuntime jsRuntime,
    ILogger<ShellDiagnosticsInterop> logger) : IAsyncDisposable
{
    private const string AttachLogMessage = "Attached app shell diagnostics logger bridge.";
    private const string EmptyDetailPlaceholder = "(no detail)";
    private const string ReportShellErrorLogTemplate = "App shell error reported from {Source}. Detail: {Detail}";
    private const string ReportShellErrorMethodName = "ReportShellError";
    private const string UnknownSource = "unknown";

    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private readonly ILogger<ShellDiagnosticsInterop> _logger = logger;

    private DotNetObjectReference<ShellDiagnosticsInterop>? _bridgeReference;
    private bool _isAttached;

    public async Task AttachAsync()
    {
        if (_isAttached)
        {
            return;
        }

        _bridgeReference = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync(AppJsInterop.AttachShellDiagnosticsLoggerMethod, _bridgeReference);
        _isAttached = true;
        _logger.LogInformation(AttachLogMessage);
    }

    [JSInvokable(ReportShellErrorMethodName)]
    public Task ReportShellError(string source, string detail)
    {
        _logger.LogError(
            ReportShellErrorLogTemplate,
            NormalizeSource(source),
            NormalizeDetail(detail));

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _bridgeReference?.Dispose();
        _bridgeReference = null;
        return ValueTask.CompletedTask;
    }

    private static string NormalizeDetail(string detail) =>
        string.IsNullOrWhiteSpace(detail)
            ? EmptyDetailPlaceholder
            : detail.Trim();

    private static string NormalizeSource(string source) =>
        string.IsNullOrWhiteSpace(source)
            ? UnknownSource
            : source.Trim();
}
