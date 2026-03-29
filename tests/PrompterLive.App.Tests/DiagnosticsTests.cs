using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrompterLive.Shared.Components.Diagnostics;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Services.Diagnostics;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class DiagnosticsTests : BunitContext
{
    [Fact]
    public async Task UiDiagnosticsService_LogsRecoverableFailureAndStoresBannerEntry()
    {
        var logProvider = new RecordingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(logProvider);
        });

        var diagnostics = new UiDiagnosticsService(loggerFactory.CreateLogger<UiDiagnosticsService>());

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

    [Fact]
    public void DiagnosticsBanner_RendersRecoverableMessageAndDismissesIt()
    {
        var harness = TestHarnessFactory.Create(this);
        var diagnostics = Services.GetRequiredService<UiDiagnosticsService>();
        diagnostics.ReportRecoverable("Settings save", "Unable to save settings.", "Forced diagnostics failure.");

        var cut = Render<DiagnosticsBanner>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Action Failed", cut.Markup);
            Assert.Contains("Unable to save settings.", cut.Markup);
            Assert.Contains("Forced diagnostics failure.", cut.Markup);
        });

        cut.FindByTestId(UiTestIds.Diagnostics.Dismiss).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Null(diagnostics.Current);
            Assert.DoesNotContain("Action Failed", cut.Markup);
        });
        GC.KeepAlive(harness);
    }

    [Fact]
    public void LoggingErrorBoundary_ShowsFatalFallbackAndLogsCriticalError()
    {
        var logProvider = new RecordingLoggerProvider();
        _ = TestHarnessFactory.Create(
            this,
            configureLogging: builder => builder.AddProvider(logProvider));

        var cut = Render<LoggingErrorBoundary>(parameters => parameters
            .AddChildContent<ThrowingDiagnosticsComponent>());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Unexpected Error", cut.Markup);
            Assert.Contains("PrompterLive hit an unexpected error.", cut.Markup);
            Assert.Contains("Forced boundary failure.", cut.Markup);
        });

        var diagnostics = Services.GetRequiredService<UiDiagnosticsService>();
        Assert.NotNull(diagnostics.Current);
        Assert.True(diagnostics.Current!.IsFatal);
        Assert.Contains(
            logProvider.Entries,
            entry => entry.Level == LogLevel.Critical &&
                entry.Message.Contains("Unhandled UI exception reached the global error boundary.", StringComparison.Ordinal));
    }

    private sealed class ThrowingDiagnosticsComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            throw new InvalidOperationException("Forced boundary failure.");
        }
    }
}
