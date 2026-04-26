using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PrompterOne.Core.Abstractions;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Services.Diagnostics;

namespace PrompterOne.Shared.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable, IAsyncDisposable
{
    private const string AppHeaderCssClass = "app-header";
    private const string AppHeaderLibraryCssClass = "app-header-library";
    private const string AppHeaderReaderMutedCssClass = "app-header--reader-muted";
    private const string GoLiveWidgetCssClass = "app-live-widget";
    private const string GoLiveWidgetReaderMutedCssClass = "app-live-widget--reader-muted";
    private const string GoLiveWidgetIdleElapsed = "00:00:00";
    private const string GoLiveSessionStateRequestFailureLogMessage = "Failed to request active Go Live session state.";
    private const string RouteChangedLogTemplate = "Route changed to {Location}.";
    private static readonly TimeSpan GoLiveWidgetRefreshInterval = TimeSpan.FromSeconds(1);

    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private AiSpotlightService AiSpotlight { get; set; } = null!;
    [Inject] private AiSpotlightHotkeyInterop AiSpotlightHotkeys { get; set; } = null!;
    [Inject] private AppShellService Shell { get; set; } = null!;
    [Inject] private BrowserConnectivityService Connectivity { get; set; } = null!;
    [Inject] private BrowserThemeService ThemeService { get; set; } = null!;
    [Inject] private GoLiveSessionService GoLiveSession { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private ILogger<MainLayout> Logger { get; set; } = null!;
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;
    [Inject] private MediaRuntimeContractService MediaRuntimeContracts { get; set; } = null!;
    [Inject] private ThemeRuntimeContractService ThemeRuntimeContracts { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private RuntimeTelemetryService RuntimeTelemetry { get; set; } = null!;

    private CancellationTokenSource? _goLiveWidgetRefreshCancellationTokenSource;
    private Task? _goLiveWidgetRefreshTask;

    private AppShellState ShellState => Shell.State;

    private GoLiveSessionState GoLiveSessionState => GoLiveSession.State;

    private bool IsLibraryScreen => ShellState.Screen == AppShellScreen.Library;

    private bool ShowAppHeader => ShellState.Screen != AppShellScreen.GoLive;

    private bool ShowBackButton => ShellState.Screen != AppShellScreen.Library;

    private bool ShowLibraryActions => ShellState.Screen == AppShellScreen.Library;

    private bool IsLibraryRootBreadcrumb =>
        string.IsNullOrWhiteSpace(ShellState.BreadcrumbLabel)
        || string.Equals(ShellState.BreadcrumbLabel, Text(UiTextKey.LibraryAllScripts), StringComparison.Ordinal);

    private string LibraryBreadcrumbCurrentLabel => IsLibraryRootBreadcrumb
        ? Text(UiTextKey.LibraryAllScripts)
        : ShellState.BreadcrumbLabel;

    private bool ShowImportAction =>
        ShellState.Screen is AppShellScreen.Library or AppShellScreen.Editor;

    private bool ShowLearnAction => ShellState.Screen == AppShellScreen.Editor;

    private bool ShowSaveFileAction => ShellState.Screen == AppShellScreen.Editor;

    private bool ShowLearnWpmBadge => ShellState.Screen == AppShellScreen.Learn;

    private bool ShowReadAction => ShellState.Screen == AppShellScreen.Editor;

    private bool HasTrackedScriptContext => !string.IsNullOrWhiteSpace(ShellState.ScriptId);

    private bool ShowHeaderSubtitle => !string.IsNullOrWhiteSpace(HeaderSubtitle);

    private bool IsTeleprompterPlaybackChromeMuted =>
        ShellState.Screen == AppShellScreen.Teleprompter && ShellState.TeleprompterPlaybackActive;

    private string HeaderSubtitle => ShellState.Screen switch
    {
        AppShellScreen.Teleprompter => ShellState.Subtitle,
        AppShellScreen.Learn => ShellState.Subtitle,
        _ => string.Empty
    };

    private string HeaderTitle => ShellState.Screen switch
    {
        AppShellScreen.GoLive => Text(UiTextKey.HeaderGoLive),
        AppShellScreen.Settings => Text(UiTextKey.HeaderSettings),
        _ => ShellState.Title
    };

    private string GoLiveIndicatorCopy => GoLiveIndicatorState switch
    {
        GoLiveIndicatorStates.Recording => Text(UiTextKey.HeaderGoLiveIndicatorRecording),
        GoLiveIndicatorStates.Streaming => Text(UiTextKey.HeaderGoLiveIndicatorStreaming),
        _ => Text(UiTextKey.HeaderGoLiveIndicatorReady)
    };

    private string GoLiveIndicatorState => GoLiveSessionState.IsRecordingActive
        ? GoLiveIndicatorStates.Recording
        : GoLiveSessionState.IsStreamActive
            ? GoLiveIndicatorStates.Streaming
            : GoLiveIndicatorStates.Idle;

    private string GoLiveIndicatorStatus => GoLiveSessionState.HasActiveSession
        ? GoLiveIndicatorCopy
        : string.Empty;

    private DateTimeOffset? GoLiveStartedAt => GoLiveSessionState.IsRecordingActive
        ? GoLiveSessionState.RecordingStartedAt ?? GoLiveSessionState.StreamStartedAt
        : GoLiveSessionState.StreamStartedAt;

    private string GoLiveWidgetDetail => string.IsNullOrWhiteSpace(GoLiveSessionState.PrimaryMicrophoneLabel)
        ? GoLiveWidgetStateLabel
        : GoLiveSessionState.PrimaryMicrophoneLabel;

    private string GoLiveWidgetElapsed => FormatSessionElapsed(GoLiveStartedAt);

    private string GoLiveWidgetStateLabel => (GoLiveSessionState.IsStreamActive, GoLiveSessionState.IsRecordingActive) switch
    {
        (true, true) => Text(UiTextKey.HeaderGoLiveWidgetLiveRecording),
        (true, false) => Text(UiTextKey.HeaderGoLiveWidgetLive),
        (false, true) => Text(UiTextKey.HeaderGoLiveWidgetRecording),
        _ => GoLiveIndicatorCopy
    };

    private string GoLiveWidgetTitle => !string.IsNullOrWhiteSpace(GoLiveSessionState.ActiveSourceLabel)
        ? GoLiveSessionState.ActiveSourceLabel
        : !string.IsNullOrWhiteSpace(GoLiveSessionState.SelectedSourceLabel)
            ? GoLiveSessionState.SelectedSourceLabel
            : Text(UiTextKey.HeaderGoLive);

    private bool ShowGoLiveWidget => !ShowOnboarding && GoLiveSessionState.HasActiveSession && ShellState.Screen != AppShellScreen.GoLive;

    private string GoLiveRoute => GoLiveSessionState.HasActiveSession
        ? string.IsNullOrWhiteSpace(GoLiveSessionState.ScriptId)
            ? AppRoutes.GoLive
            : AppRoutes.GoLiveWithId(GoLiveSessionState.ScriptId)
        : Shell.GetGoLiveRoute();

    private string ImportActionButtonTestId => IsLibraryScreen
        ? UiTestIds.Header.LibraryOpenScriptButton
        : UiTestIds.Header.EditorImportScriptButton;

    private string ImportActionIconTestId => IsLibraryScreen
        ? UiTestIds.Header.LibraryOpenScriptIcon
        : UiTestIds.Header.EditorImportScriptIcon;

    private string ImportActionInputDomId => IsLibraryScreen
        ? UiDomIds.AppShell.LibraryOpenScriptInput
        : UiDomIds.AppShell.EditorImportScriptInput;

    private string ImportActionInputTestId => IsLibraryScreen
        ? UiTestIds.Header.LibraryOpenScriptInput
        : UiTestIds.Header.EditorImportScriptInput;

    private string ImportActionSurfaceTestId => IsLibraryScreen
        ? UiTestIds.Header.LibraryOpenScript
        : UiTestIds.Header.EditorImportScript;

    private string BuildAppHeaderCssClass() =>
        BuildClassList(
            AppHeaderCssClass,
            IsLibraryScreen ? AppHeaderLibraryCssClass : null,
            IsTeleprompterPlaybackChromeMuted ? AppHeaderReaderMutedCssClass : null);

    private string BuildGoLiveWidgetCssClass() =>
        BuildClassList(
            GoLiveWidgetCssClass,
            IsTeleprompterPlaybackChromeMuted ? GoLiveWidgetReaderMutedCssClass : null);

    private static string FormatSessionElapsed(DateTimeOffset? startedAt)
    {
        if (startedAt is null)
        {
            return GoLiveWidgetIdleElapsed;
        }

        var elapsed = DateTimeOffset.UtcNow - startedAt.Value;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        return elapsed.TotalHours >= 1
            ? elapsed.ToString(@"hh\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture)
            : elapsed.ToString(@"mm\:ss", System.Globalization.CultureInfo.InvariantCulture).Insert(0, "00:");
    }

    protected override void OnInitialized()
    {
        Navigation.LocationChanged += HandleLocationChanged;
        Shell.StateChanged += HandleShellStateChanged;
        GoLiveSession.StateChanged += HandleGoLiveSessionChanged;
        Shell.TrackNavigation(Navigation.Uri);
        SyncShellStateWithCurrentRoute(Navigation.Uri);
        PublishRouteAiContext();
        UpdateGoLiveWidgetRefreshLoop();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await Connectivity.StartAsync();
        await GoLiveSession.StartCrossTabSyncAsync();
        await MediaRuntimeContracts.InitializeAsync();
        await ThemeRuntimeContracts.InitializeAsync();
        await ThemeService.InitializeAsync();
        await Bootstrapper.EnsureReadyAsync();
        await InitializeOnboardingAsync();
        await AiSpotlightHotkeys.InitializeAsync();
        await RuntimeTelemetry.InitializeAsync();
        await TrackCurrentPageViewAsync();
    }

    private void HandleShellStateChanged() => InvokeAsync(StateHasChanged);

    private void HandleGoLiveSessionChanged()
    {
        UpdateGoLiveWidgetRefreshLoop();
        _ = InvokeAsync(StateHasChanged);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        Logger.LogInformation(RouteChangedLogTemplate, e.Location);
        Shell.TrackNavigation(e.Location);
        SyncShellStateWithCurrentRoute(e.Location);
        PublishRouteAiContext();
        RequestGoLiveSessionStateInBackground();
        SyncOnboardingStepWithCurrentRoute(e.Location);
        _ = HandleOnboardingLocationChangedAsync(e.Location);
        _ = InvokeAsync(TrackCurrentPageViewAsync);
        StateHasChanged();
    }

    private void RequestGoLiveSessionStateInBackground()
    {
        _ = RequestGoLiveSessionStateAsync();
    }

    private async Task RequestGoLiveSessionStateAsync()
    {
        try
        {
            await GoLiveSession.RequestCrossTabStateAsync();
        }
        catch (Exception exception)
        {
            Logger.LogDebug(exception, GoLiveSessionStateRequestFailureLogMessage);
        }
    }

    private async Task HandleOnboardingLocationChangedAsync(string location)
    {
        if (!IsOnboardingReopenRequested(location) || ShowOnboarding)
        {
            return;
        }

        await ShowOnboardingAsync();
    }

    private void SyncShellStateWithCurrentRoute(string uri)
    {
        var currentScriptId = ResolveQueryValue(uri, AppRoutes.ScriptIdQueryKey);
        var currentPath = new Uri(uri).AbsolutePath.TrimEnd('/');

        switch (currentPath)
        {
            case "":
            case AppRoutes.Root:
            case AppRoutes.Library:
                Shell.ShowLibrary(string.IsNullOrWhiteSpace(ShellState.BreadcrumbLabel)
                    ? Text(UiTextKey.LibraryAllScripts)
                    : ShellState.BreadcrumbLabel);
                break;
            case AppRoutes.Editor:
                Shell.ShowEditor(ShellState.Title, currentScriptId);
                break;
            case AppRoutes.Learn:
            case AppRoutes.Prep:
                Shell.ShowLearn(ShellState.Title, ShellState.Subtitle, ShellState.WpmLabel, currentScriptId);
                break;
            case AppRoutes.Teleprompter:
                Shell.ShowTeleprompter(ShellState.Title, ShellState.Subtitle, currentScriptId);
                break;
            case AppRoutes.GoLive:
                Shell.ShowGoLive(Text(UiTextKey.HeaderGoLive), string.Empty, currentScriptId);
                break;
            case AppRoutes.Settings:
                Shell.ShowSettings();
                break;
            default:
                Shell.ShowLibrary(Text(UiTextKey.LibraryAllScripts));
                break;
        }
    }

    private string Text(UiTextKey key) => Localizer[key.ToString()];

    private static string ResolveQueryValue(string uri, string key)
    {
        var parsedUri = new Uri(uri);
        var query = parsedUri.Query;
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var trimmedQuery = query.TrimStart('?');
        var pairs = trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || !string.Equals(parts[0], key, StringComparison.Ordinal))
            {
                continue;
            }

            return parts.Length > 1
                ? Uri.UnescapeDataString(parts[1])
                : string.Empty;
        }

        return string.Empty;
    }

    private Task HandleHomeClickAsync()
    {
        Navigation.NavigateTo(AppRoutes.Library);
        return Task.CompletedTask;
    }

    private Task HandleBackClickAsync()
    {
        Navigation.NavigateTo(Shell.GetBackNavigationRoute(), replace: true);
        return Task.CompletedTask;
    }

    private async Task HandleCreateScriptClickAsync()
    {
        await Bootstrapper.EnsureReadyAsync();
        await SessionService.NewAsync();
        await RuntimeTelemetry.TrackNavigationActionAsync(
            AppRuntimeTelemetry.Events.CreateScript,
            ShellState.Screen,
            AppShellScreen.Editor,
            hasScriptContext: false);
        Navigation.NavigateTo(AppRoutes.Editor);
    }

    private async Task HandleOpenLearnClickAsync()
    {
        await RuntimeTelemetry.TrackNavigationActionAsync(
            AppRuntimeTelemetry.Events.OpenLearn,
            ShellState.Screen,
            AppShellScreen.Learn,
            HasTrackedScriptContext);
        Navigation.NavigateTo(Shell.GetLearnRoute());
    }

    private async Task HandleOpenGoLiveClickAsync()
    {
        await RuntimeTelemetry.TrackNavigationActionAsync(
            AppRuntimeTelemetry.Events.OpenGoLive,
            ShellState.Screen,
            AppShellScreen.GoLive,
            HasTrackedScriptContext);
        Navigation.NavigateTo(GoLiveRoute);
    }

    private async Task HandleOpenReadClickAsync()
    {
        await RuntimeTelemetry.TrackNavigationActionAsync(
            AppRuntimeTelemetry.Events.OpenRead,
            ShellState.Screen,
            AppShellScreen.Teleprompter,
            HasTrackedScriptContext);
        Navigation.NavigateTo(Shell.GetTeleprompterRoute());
    }

    private Task TrackCurrentPageViewAsync() =>
        RuntimeTelemetry.TrackPageViewAsync(ShellState.Screen, HeaderTitle, HasTrackedScriptContext);

    private Task HandleLibrarySearchInputAsync(ChangeEventArgs args)
    {
        Shell.UpdateLibrarySearch(args.Value?.ToString() ?? string.Empty);
        return Task.CompletedTask;
    }

    private Task HandleOpenAiSpotlightAsync()
    {
        PublishRouteAiContext();
        AiSpotlight.Open();
        return Task.CompletedTask;
    }

    private void PublishRouteAiContext() =>
        AiSpotlight.SetRouteContext(ShellState.Screen, Navigation.Uri, HeaderTitle);

    private void UpdateGoLiveWidgetRefreshLoop()
    {
        if (GoLiveSessionState.HasActiveSession)
        {
            StartGoLiveWidgetRefreshLoop();
            return;
        }

        StopGoLiveWidgetRefreshLoop();
    }

    private void StartGoLiveWidgetRefreshLoop()
    {
        if (_goLiveWidgetRefreshTask is not null)
        {
            return;
        }

        _goLiveWidgetRefreshCancellationTokenSource = new CancellationTokenSource();
        _goLiveWidgetRefreshTask = RefreshGoLiveWidgetAsync(_goLiveWidgetRefreshCancellationTokenSource.Token);
    }

    private async Task RefreshGoLiveWidgetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(GoLiveWidgetRefreshInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void StopGoLiveWidgetRefreshLoop()
    {
        if (_goLiveWidgetRefreshCancellationTokenSource is null)
        {
            return;
        }

        _goLiveWidgetRefreshCancellationTokenSource.Cancel();
        _goLiveWidgetRefreshCancellationTokenSource.Dispose();
        _goLiveWidgetRefreshCancellationTokenSource = null;
        _goLiveWidgetRefreshTask = null;
    }

    private static string BuildClassList(params string?[] classNames) =>
        string.Join(' ', classNames.Where(className => !string.IsNullOrWhiteSpace(className)));

    public async ValueTask DisposeAsync()
    {
        await AiSpotlightHotkeys.DisposeAsync();
        Dispose();
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
        Shell.StateChanged -= HandleShellStateChanged;
        GoLiveSession.StateChanged -= HandleGoLiveSessionChanged;
        StopGoLiveWidgetRefreshLoop();
    }
}
