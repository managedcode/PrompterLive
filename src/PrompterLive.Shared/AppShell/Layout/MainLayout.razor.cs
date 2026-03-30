using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PrompterLive.Core.Abstractions;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Localization;
using PrompterLive.Shared.Services;
using PrompterLive.Shared.Services.Diagnostics;

namespace PrompterLive.Shared.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private const string GoLiveWidgetIdleElapsed = "00:00:00";
    private const string GoLiveWidgetLiveStateLabel = "Live";
    private const string GoLiveWidgetRecordingStateLabel = "Rec";
    private const string GoLiveWidgetStreamingRecordingStateLabel = "Live + Rec";
    private const string RouteChangedLogTemplate = "Route changed to {Location}.";

    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private AppShellService Shell { get; set; } = null!;
    [Inject] private BrowserConnectivityService Connectivity { get; set; } = null!;
    [Inject] private GoLiveSessionService GoLiveSession { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private ILogger<MainLayout> Logger { get; set; } = null!;
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private AppShellState ShellState => Shell.State;

    private GoLiveSessionState GoLiveSessionState => GoLiveSession.State;

    private bool IsLibraryScreen => ShellState.Screen == AppShellScreen.Library;

    private bool ShowBackButton => ShellState.Screen != AppShellScreen.Library;

    private bool ShowLibraryActions => ShellState.Screen == AppShellScreen.Library;

    private bool ShowLearnAction => ShellState.Screen == AppShellScreen.Editor;

    private bool ShowLearnWpmBadge => ShellState.Screen == AppShellScreen.Learn;

    private bool ShowReadAction => ShellState.Screen is AppShellScreen.Editor or AppShellScreen.GoLive;

    private bool ShowHeaderSubtitle => !string.IsNullOrWhiteSpace(HeaderSubtitle);

    private string HeaderSubtitle => ShellState.Screen switch
    {
        AppShellScreen.Teleprompter => ShellState.Subtitle,
        AppShellScreen.GoLive => ShellState.Title,
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
        RecordingStateValue => "Recording active",
        StreamingStateValue => "Stream active",
        _ => "Ready"
    };

    private string GoLiveIndicatorState => GoLiveSessionState.IsRecordingActive
        ? RecordingStateValue
        : GoLiveSessionState.IsStreamActive
            ? StreamingStateValue
            : IdleStateValue;

    private DateTimeOffset? GoLiveStartedAt => GoLiveSessionState.IsRecordingActive
        ? GoLiveSessionState.RecordingStartedAt ?? GoLiveSessionState.StreamStartedAt
        : GoLiveSessionState.StreamStartedAt;

    private string GoLiveWidgetDetail => string.IsNullOrWhiteSpace(GoLiveSessionState.ScriptSubtitle)
        ? GoLiveSessionState.PrimaryMicrophoneLabel
        : GoLiveSessionState.ScriptSubtitle;

    private string GoLiveWidgetElapsed => FormatSessionElapsed(GoLiveStartedAt);

    private string GoLiveWidgetStateLabel => (GoLiveSessionState.IsStreamActive, GoLiveSessionState.IsRecordingActive) switch
    {
        (true, true) => GoLiveWidgetStreamingRecordingStateLabel,
        (true, false) => GoLiveWidgetLiveStateLabel,
        (false, true) => GoLiveWidgetRecordingStateLabel,
        _ => GoLiveIndicatorCopy
    };

    private string GoLiveWidgetTitle => string.IsNullOrWhiteSpace(GoLiveSessionState.ActiveSourceLabel)
        ? GoLiveSessionState.ScriptTitle
        : GoLiveSessionState.ActiveSourceLabel;

    private bool ShowGoLiveWidget => GoLiveSessionState.HasActiveSession && ShellState.Screen != AppShellScreen.GoLive;

    private string GoLiveRoute => !string.IsNullOrWhiteSpace(GoLiveSessionState.ScriptId)
        ? AppRoutes.GoLiveWithId(GoLiveSessionState.ScriptId)
        : Shell.GetGoLiveRoute();

    private const string IdleStateValue = "idle";
    private const string RecordingStateValue = "recording";
    private const string StreamingStateValue = "streaming";

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
        SyncShellStateWithCurrentRoute(Navigation.Uri);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await Connectivity.StartAsync();
        await Bootstrapper.EnsureReadyAsync();
    }

    private void HandleShellStateChanged() => InvokeAsync(StateHasChanged);

    private void HandleGoLiveSessionChanged() => InvokeAsync(StateHasChanged);

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        Logger.LogInformation(RouteChangedLogTemplate, e.Location);
        SyncShellStateWithCurrentRoute(e.Location);
        StateHasChanged();
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
                Shell.ShowLearn(ShellState.Title, ShellState.Subtitle, ShellState.WpmLabel, currentScriptId);
                break;
            case AppRoutes.Teleprompter:
                Shell.ShowTeleprompter(ShellState.Title, ShellState.Subtitle, currentScriptId);
                break;
            case AppRoutes.GoLive:
                Shell.ShowGoLive(ShellState.Title, ShellState.Subtitle, currentScriptId);
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
        Navigation.NavigateTo(AppRoutes.Library);
        return Task.CompletedTask;
    }

    private async Task HandleCreateScriptClickAsync()
    {
        await Bootstrapper.EnsureReadyAsync();
        await SessionService.NewAsync();
        Navigation.NavigateTo(AppRoutes.Editor);
    }

    private Task HandleOpenLearnClickAsync()
    {
        Navigation.NavigateTo(Shell.GetLearnRoute());
        return Task.CompletedTask;
    }

    private Task HandleOpenGoLiveClickAsync()
    {
        Navigation.NavigateTo(GoLiveRoute);
        return Task.CompletedTask;
    }

    private Task HandleOpenReadClickAsync()
    {
        Navigation.NavigateTo(Shell.GetTeleprompterRoute());
        return Task.CompletedTask;
    }

    private Task HandleLibrarySearchInputAsync(ChangeEventArgs args)
    {
        Shell.UpdateLibrarySearch(args.Value?.ToString() ?? string.Empty);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
        Shell.StateChanged -= HandleShellStateChanged;
        GoLiveSession.StateChanged -= HandleGoLiveSessionChanged;
    }
}
