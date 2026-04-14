using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PrompterOne.Core.Localization;
using PrompterOne.Shared.Components.Diagnostics;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;
using PrompterOne.Shared.Tests;
using Sentry;

namespace PrompterOne.Web.Tests;

[NotInParallel]
public sealed class DiagnosticsTests : BunitContext
{
    private const string ExceptionEventName = "exception";
    private const string ExceptionIsFatalParameter = "is_fatal";
    private const string ExceptionMessageParameter = "exception_message";
    private const string ExceptionOperationParameter = "operation";
    private const string ExceptionTypeParameter = "exception_type";
    private const string RuntimeTelemetryInitializeIdentifier = "initializeRuntimeTelemetry";
    private const string RuntimeTelemetryTrackEventIdentifier = "trackRuntimeTelemetryEvent";

    [Test]
    public async Task UiDiagnosticsService_LogsRecoverableFailureAndStoresBannerEntry()
    {
        Services.AddLocalization();
        var logProvider = new RecordingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(logProvider);
        });

        var diagnostics = new UiDiagnosticsService(
            loggerFactory.CreateLogger<UiDiagnosticsService>(),
            Services.GetRequiredService<IStringLocalizer<SharedResource>>());

        var completed = await diagnostics.RunAsync(
            "Library load",
            "Unable to load the library right now.",
            () => Task.FromException(new InvalidOperationException("Forced diagnostics failure.")));

        Assert.False(completed);
        Assert.NotNull(diagnostics.Current);
        Assert.False(diagnostics.Current!.IsFatal);
        Assert.Equal("Library load", diagnostics.Current.Operation);
        Assert.Equal("Forced diagnostics failure.", diagnostics.Current.Detail);
        Assert.Contains(
            logProvider.Entries,
            entry => entry.Level == LogLevel.Information &&
                entry.Message.Contains("Starting UI operation", StringComparison.Ordinal));
        Assert.Contains(
            logProvider.Entries,
            entry => entry.Level == LogLevel.Error &&
                entry.Message.Contains("UI operation Library load failed.", StringComparison.Ordinal));
    }

    [Test]
    public void DiagnosticsBanner_RendersRecoverableMessageAndDismissesIt()
    {
        using var cultureScope = new CultureScope(AppCultureCatalog.EnglishCultureName);
        var harness = TestHarnessFactory.Create(this);
        var diagnostics = Services.GetRequiredService<UiDiagnosticsService>();
        diagnostics.ReportRecoverable("Settings save", "Unable to save settings.", "Forced diagnostics failure.");

        var cut = Render<DiagnosticsBanner>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(Text(UiTextKey.DiagnosticsRecoverableTitle), cut.Markup);
            Assert.Contains("Unable to save settings.", cut.Markup);
            Assert.Contains("Forced diagnostics failure.", cut.Markup);
        });

        cut.FindByTestId(UiTestIds.Diagnostics.Dismiss).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Null(diagnostics.Current);
            Assert.DoesNotContain(Text(UiTextKey.DiagnosticsRecoverableTitle), cut.Markup);
        });
        GC.KeepAlive(harness);
    }

    [Test]
    public void LoggingErrorBoundary_ShowsFatalFallbackAndLogsCriticalError()
    {
        using var cultureScope = new CultureScope(AppCultureCatalog.EnglishCultureName);
        var logProvider = new RecordingLoggerProvider();
        _ = TestHarnessFactory.Create(
            this,
            configureLogging: builder => builder.AddProvider(logProvider));

        var cut = Render<LoggingErrorBoundary>(parameters => parameters
            .AddChildContent<ThrowingDiagnosticsComponent>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(Text(UiTextKey.DiagnosticsFatalTitle), cut.Markup);
            Assert.Contains(Text(UiTextKey.DiagnosticsFatalMessage), cut.Markup);
            Assert.Contains("Forced boundary failure.", cut.Markup);
            Assert.Contains(Text(UiTextKey.DiagnosticsRetry), cut.Markup);
            Assert.Contains(Text(UiTextKey.DiagnosticsLibrary), cut.Markup);
        });

        var diagnostics = Services.GetRequiredService<UiDiagnosticsService>();
        Assert.NotNull(diagnostics.Current);
        Assert.True(diagnostics.Current!.IsFatal);
        Assert.Contains(
            logProvider.Entries,
                entry => entry.Level == LogLevel.Critical &&
                entry.Message.Contains("Unhandled UI exception reached the global error boundary.", StringComparison.Ordinal));
    }

    [Test]
    public async Task UiDiagnosticsService_TracksRecoverableExceptions_ThroughRuntimeTelemetry()
    {
        using var cultureScope = new CultureScope(AppCultureCatalog.EnglishCultureName);
        var harness = TestHarnessFactory.Create(
            this,
            runtimeTelemetryOptions: CreateTelemetryOptions());
        var diagnostics = Services.GetRequiredService<UiDiagnosticsService>();

        var completed = await diagnostics.RunAsync(
            "Library load",
            "Unable to load the library right now.",
            () => Task.FromException(new InvalidOperationException("Forced diagnostics failure.")));

        Assert.False(completed);

        var payload = WaitForTelemetryEventPayload(harness.JsRuntime, ExceptionEventName);

        Assert.Equal("Library load", payload.GetProperty(ExceptionOperationParameter).GetString());
        Assert.Equal("Forced diagnostics failure.", payload.GetProperty(ExceptionMessageParameter).GetString());
        Assert.Equal(nameof(InvalidOperationException), payload.GetProperty(ExceptionTypeParameter).GetString());
        Assert.False(payload.GetProperty(ExceptionIsFatalParameter).GetBoolean());
        Assert.Contains(
            harness.JsRuntime.InvocationRecords,
            record => string.Equals(record.Identifier, RuntimeTelemetryInitializeIdentifier, StringComparison.Ordinal));
    }

    [Test]
    public async Task UiDiagnosticsService_DoesNotSendBrowserInjectedForcedFailuresToSentry()
    {
        using var cultureScope = new CultureScope(AppCultureCatalog.EnglishCultureName);
        var harness = TestHarnessFactory.Create(
            this,
            runtimeTelemetryOptions: CreateTelemetryOptions());
        var diagnostics = Services.GetRequiredService<UiDiagnosticsService>();

        var completed = await diagnostics.RunAsync(
            "Library create folder",
            "Unable to create this folder.",
            () => Task.FromException(new JSException("Forced diagnostics failure from browser test.")));

        Assert.False(completed);
        Assert.Empty(harness.SentryClient.Exceptions);

        var payload = WaitForTelemetryEventPayload(harness.JsRuntime, ExceptionEventName);

        Assert.Equal("Library create folder", payload.GetProperty(ExceptionOperationParameter).GetString());
        Assert.Equal("Forced diagnostics failure from browser test.", payload.GetProperty(ExceptionMessageParameter).GetString());
        Assert.Equal(nameof(JSException), payload.GetProperty(ExceptionTypeParameter).GetString());
    }

    [Test]
    public void LoggingErrorBoundary_TracksFatalExceptions_ThroughRuntimeTelemetry()
    {
        using var cultureScope = new CultureScope(AppCultureCatalog.EnglishCultureName);
        var harness = TestHarnessFactory.Create(
            this,
            runtimeTelemetryOptions: CreateTelemetryOptions());

        var cut = Render<LoggingErrorBoundary>(parameters => parameters
            .AddChildContent<ThrowingDiagnosticsComponent>());

        cut.WaitForAssertion(() =>
        {
            var payload = WaitForTelemetryEventPayload(harness.JsRuntime, ExceptionEventName);
            var diagnostics = Services.GetRequiredService<UiDiagnosticsService>();

            Assert.NotNull(diagnostics.Current);
            Assert.True(payload.GetProperty(ExceptionIsFatalParameter).GetBoolean());
            Assert.Equal(diagnostics.Current!.Operation, payload.GetProperty(ExceptionOperationParameter).GetString());
            Assert.Equal("Forced boundary failure.", payload.GetProperty(ExceptionMessageParameter).GetString());
            Assert.Equal(nameof(InvalidOperationException), payload.GetProperty(ExceptionTypeParameter).GetString());
        });
    }

    [Test]
    public void RuntimeTelemetrySuppressionPolicy_SuppressesBrowserInjectedSentryLoggerEvents()
    {
        var sentryEvent = new SentryEvent(
            new JSException("Forced LiveKit connect failure from browser test."));

        Assert.True(RuntimeTelemetrySuppressionPolicy.ShouldSuppressOutboundSentry(sentryEvent));
    }

    private string Text(UiTextKey key) =>
        Services.GetRequiredService<IStringLocalizer<SharedResource>>()[key.ToString()];

    private static JsonElement WaitForTelemetryEventPayload(TestJsRuntime jsRuntime, string eventName)
    {
        var invocation = jsRuntime.InvocationRecords.LastOrDefault(record =>
            string.Equals(record.Identifier, RuntimeTelemetryTrackEventIdentifier, StringComparison.Ordinal)
            && record.Arguments.Count >= 2
            && string.Equals(record.Arguments[0]?.ToString(), eventName, StringComparison.Ordinal));

        Assert.NotNull(invocation);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(invocation.Arguments[1]));
        return document.RootElement.Clone();
    }

    private static RuntimeTelemetryOptions CreateTelemetryOptions() =>
        new(
            GoogleAnalyticsMeasurementId: "G-test-measurement",
            ClarityProjectId: "clarity-test-project",
            SentryDsn: "https://public@example.ingest.sentry.io/1",
            HostEnabled: true);

    private sealed class ThrowingDiagnosticsComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            throw new InvalidOperationException("Forced boundary failure.");
        }
    }
}
