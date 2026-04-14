using Microsoft.JSInterop;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Services;

public sealed class RuntimeTelemetryService(
    IJSRuntime jsRuntime,
    RuntimeTelemetryOptions options,
    ISentryRuntimeClient sentryClient) : IDisposable, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private readonly RuntimeTelemetryOptions _options = options;
    private readonly ISentryRuntimeClient _sentryClient = sentryClient;
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
                contract = new
                {
                    eventsCollection = AppRuntimeTelemetry.Harness.EventsCollection,
                    harnessGlobalName = AppRuntimeTelemetry.Harness.GlobalName,
                    initializationsCollection = AppRuntimeTelemetry.Harness.InitializationsCollection,
                    pageViewsCollection = AppRuntimeTelemetry.Harness.PageViewsCollection,
                    runtimeAllowVendorLoadsProperty = AppRuntimeTelemetry.Harness.RuntimeAllowVendorLoadsProperty,
                    runtimeGlobalName = AppRuntimeTelemetry.Harness.RuntimeGlobalName,
                    runtimeHarnessEnabledProperty = AppRuntimeTelemetry.Harness.RuntimeHarnessEnabledProperty,
                    runtimeWasmDebugEnabledProperty = AppRuntimeTelemetry.Harness.RuntimeWasmDebugEnabledProperty,
                    vendorLoadsCollection = AppRuntimeTelemetry.Harness.VendorLoadsCollection
                },
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

    public async Task<SentryId> TrackExceptionAsync(Exception exception, string operation, bool isFatal)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        var payload = new Dictionary<string, object?>
        {
            [AppRuntimeTelemetry.Parameters.ExceptionMessage] = exception.Message,
            [AppRuntimeTelemetry.Parameters.ExceptionType] = exception.GetType().Name,
            [AppRuntimeTelemetry.Parameters.IsFatal] = isFatal,
            [AppRuntimeTelemetry.Parameters.Operation] = operation
        };

        var sentryEventId = CaptureSentryException(exception, payload, isFatal);
        await TrackJsTelemetryAsync(
            RuntimeTelemetryInteropMethodNames.TrackEvent,
            AppRuntimeTelemetry.Events.Exception,
            payload);
        return sentryEventId;
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
        CaptureSentryTelemetry(eventName, payload, methodName);
        await TrackJsTelemetryAsync(methodName, eventName, payload);
    }

    private void CaptureSentryTelemetry(
        string eventName,
        IReadOnlyDictionary<string, object?> payload,
        string methodName)
    {
        if (!_options.HostEnabled || !_options.SentryConfigured)
        {
            return;
        }

        var telemetryKind = string.Equals(
            methodName,
            RuntimeTelemetryInteropMethodNames.TrackPageView,
            StringComparison.Ordinal)
            ? AppRuntimeTelemetry.Events.PageView
            : RuntimeTelemetryInteropMethodNames.TrackEvent;

        _sentryClient.CaptureMessage(
            eventName,
            scope =>
            {
                scope.SetTag("runtime.telemetry.kind", telemetryKind);
                scope.SetTag("runtime.telemetry.method", methodName);

                foreach (var entry in payload)
                {
                    scope.SetExtra(entry.Key, entry.Value ?? string.Empty);

                    if (entry.Value is string tagValue && !string.IsNullOrWhiteSpace(tagValue))
                    {
                        scope.SetTag($"runtime.telemetry.{entry.Key}", tagValue);
                    }
                }
            },
            SentryLevel.Info);
    }

    private SentryId CaptureSentryException(
        Exception exception,
        IReadOnlyDictionary<string, object?> payload,
        bool isFatal)
    {
        if (!_options.HostEnabled || !_options.SentryConfigured)
        {
            return SentryId.Empty;
        }

        if (RuntimeTelemetrySuppressionPolicy.ShouldSuppressOutboundSentry(exception))
        {
            return SentryId.Empty;
        }

        return _sentryClient.CaptureException(
            exception,
            scope =>
            {
                scope.SetTag("runtime.telemetry.is_fatal", isFatal.ToString().ToLowerInvariant());
                scope.SetTag("runtime.telemetry.kind", AppRuntimeTelemetry.Events.Exception);

                ApplyScopePayload(scope, payload);
            });
    }

    private static void ApplyScopePayload(Scope scope, IReadOnlyDictionary<string, object?> payload)
    {
        foreach (var entry in payload)
        {
            scope.SetExtra(entry.Key, entry.Value ?? string.Empty);

            if (entry.Value is string tagValue && !string.IsNullOrWhiteSpace(tagValue))
            {
                scope.SetTag($"runtime.telemetry.{entry.Key}", tagValue);
            }
        }
    }

    private async Task TrackJsTelemetryAsync(
        string methodName,
        string eventName,
        IReadOnlyDictionary<string, object?> payload)
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
