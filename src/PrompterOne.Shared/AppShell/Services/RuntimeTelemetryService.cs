using Microsoft.JSInterop;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Services;

public sealed class RuntimeTelemetryService(IJSRuntime jsRuntime, RuntimeTelemetryOptions options) : IDisposable, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private readonly RuntimeTelemetryOptions _options = options;
    private bool _initializationAttempted;
    private Task<IJSObjectReference?>? _moduleTask;

    public async Task InitializeAsync()
    {
        if (_initializationAttempted)
        {
            return;
        }

        _initializationAttempted = true;

        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync(
            RuntimeTelemetryInteropMethodNames.Initialize,
            new
            {
                clarityProjectId = _options.ClarityProjectId,
                googleAnalyticsMeasurementId = _options.GoogleAnalyticsMeasurementId,
                hostEnabled = _options.HostEnabled,
                sentryConfigured = _options.SentryConfigured
            });
    }

    public Task TrackNavigationActionAsync(
        string eventName,
        AppShellScreen sourceScreen,
        AppShellScreen targetScreen,
        bool hasScriptContext)
    {
        var payload = new Dictionary<string, object?>
        {
            [AppRuntimeTelemetry.Parameters.ScriptLoaded] = hasScriptContext,
            [AppRuntimeTelemetry.Parameters.SourceScreen] = AppRuntimeTelemetry.GetPageName(sourceScreen),
            [AppRuntimeTelemetry.Parameters.TargetScreen] = AppRuntimeTelemetry.GetPageName(targetScreen)
        };

        return TrackEventAsync(eventName, payload);
    }

    public Task TrackPageViewAsync(AppShellScreen screen, string title, bool hasScriptContext)
    {
        var payload = new Dictionary<string, object?>
        {
            [AppRuntimeTelemetry.Parameters.PagePath] = AppRuntimeTelemetry.GetRoutePath(screen),
            [AppRuntimeTelemetry.Parameters.PageTitle] = string.IsNullOrWhiteSpace(title)
                ? AppRuntimeTelemetry.GetDefaultTitle(screen)
                : title,
            [AppRuntimeTelemetry.Parameters.ScreenName] = AppRuntimeTelemetry.GetPageName(screen),
            [AppRuntimeTelemetry.Parameters.ScriptLoaded] = hasScriptContext
        };

        return TrackEventAsync(AppRuntimeTelemetry.Events.PageView, payload, RuntimeTelemetryInteropMethodNames.TrackPageView);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask is null)
        {
            return;
        }

        var module = await _moduleTask;
        if (module is not null)
        {
            await module.DisposeAsync();
        }
    }

    public void Dispose()
    {
    }

    private Task TrackEventAsync(string eventName, IReadOnlyDictionary<string, object?> payload) =>
        TrackEventAsync(eventName, payload, RuntimeTelemetryInteropMethodNames.TrackEvent);

    private async Task TrackEventAsync(
        string eventName,
        IReadOnlyDictionary<string, object?> payload,
        string methodName)
    {
        await InitializeAsync();

        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync(methodName, eventName, payload);
    }

    private Task<IJSObjectReference?> GetModuleAsync() =>
        _moduleTask ??= ImportModuleAsync();

    private async Task<IJSObjectReference?> ImportModuleAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<IJSObjectReference>(
                RuntimeTelemetryInteropMethodNames.JSImportMethodName,
                RuntimeTelemetryInteropMethodNames.ModulePath);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (JSException)
        {
            return null;
        }
    }
}
