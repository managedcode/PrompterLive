using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Services;

public sealed class AppShellService
{
    private const string EmptyRoute = "";
    private const string QuerySeparator = "?";

    private string _currentRoute = AppRoutes.Library;
    private string _goLiveBackRoute = AppRoutes.Library;
    private string _settingsBackRoute = AppRoutes.Library;

    public event Action? StateChanged;
    public event Action<string>? LibrarySearchChanged;

    public AppShellState State { get; private set; } = AppShellState.Default;

    public void ShowLibrary(string breadcrumbLabel)
    {
        var nextSearchText = State.Screen == AppShellScreen.Library
            ? State.SearchText
            : string.Empty;

        SetState(new AppShellState(
            Screen: AppShellScreen.Library,
            Title: string.Empty,
            Subtitle: string.Empty,
            WpmLabel: string.Empty,
            BreadcrumbLabel: breadcrumbLabel,
            SearchText: nextSearchText,
            ScriptId: string.Empty));
    }

    public void ShowEditor(string title, string? scriptId) =>
        SetScriptScopedState(AppShellScreen.Editor, title, string.Empty, string.Empty, scriptId);

    public void ShowLearn(string title, string subtitle, string wpmLabel, string? scriptId) =>
        SetScriptScopedState(AppShellScreen.Learn, title, subtitle, wpmLabel, scriptId);

    public void ShowTeleprompter(string title, string subtitle, string? scriptId) =>
        SetScriptScopedState(AppShellScreen.Teleprompter, title, subtitle, string.Empty, scriptId);

    public void ShowGoLive(string title, string subtitle, string? scriptId) =>
        SetScriptScopedState(
            AppShellScreen.GoLive,
            title,
            subtitle,
            string.Empty,
            scriptId);

    public void ShowSettings() =>
        SetState(new AppShellState(
            Screen: AppShellScreen.Settings,
            Title: string.Empty,
            Subtitle: string.Empty,
            WpmLabel: string.Empty,
            BreadcrumbLabel: string.Empty,
            SearchText: string.Empty,
            ScriptId: string.Empty));

    public void UpdateLibrarySearch(string searchText)
    {
        var normalizedSearchText = searchText ?? string.Empty;
        if (string.Equals(State.SearchText, normalizedSearchText, StringComparison.Ordinal))
        {
            return;
        }

        SetState(State with { SearchText = normalizedSearchText });
        LibrarySearchChanged?.Invoke(normalizedSearchText);
    }

    public string GetEditorRoute() => BuildScriptScopedRoute(AppShellScreen.Editor);

    public string GetLearnRoute() => BuildScriptScopedRoute(AppShellScreen.Learn);

    public string GetTeleprompterRoute() => BuildScriptScopedRoute(AppShellScreen.Teleprompter);

    public string GetGoLiveRoute() => BuildScriptScopedRoute(AppShellScreen.GoLive);

    public string GetBackRoute() => State.Screen switch
    {
        AppShellScreen.Learn => GetEditorRoute(),
        AppShellScreen.Teleprompter => GetEditorRoute(),
        AppShellScreen.Settings => GetSettingsBackRoute(),
        AppShellScreen.GoLive => GetGoLiveBackRoute(),
        _ => AppRoutes.Library
    };

    public string GetGoLiveBackRoute() => IsValidGoLiveBackTarget(_goLiveBackRoute)
        ? _goLiveBackRoute
        : AppRoutes.Library;

    public string GetSettingsBackRoute() => IsValidSettingsBackTarget(_settingsBackRoute)
        ? _settingsBackRoute
        : AppRoutes.Library;

    public void TrackNavigation(string uri)
    {
        var nextRoute = NormalizeAppRoute(uri);
        if (string.IsNullOrWhiteSpace(nextRoute) || string.Equals(_currentRoute, nextRoute, StringComparison.Ordinal))
        {
            return;
        }

        if (IsSettingsRoute(nextRoute) && IsValidSettingsBackTarget(_currentRoute))
        {
            _settingsBackRoute = _currentRoute;
        }

        if (IsGoLiveRoute(nextRoute) && IsValidGoLiveBackTarget(_currentRoute))
        {
            _goLiveBackRoute = _currentRoute;
        }

        _currentRoute = nextRoute;
    }

    private void SetScriptScopedState(
        AppShellScreen screen,
        string title,
        string subtitle,
        string wpmLabel,
        string? scriptId)
    {
        SetState(new AppShellState(
            Screen: screen,
            Title: title,
            Subtitle: subtitle,
            WpmLabel: wpmLabel,
            BreadcrumbLabel: string.Empty,
            SearchText: string.Empty,
            ScriptId: scriptId ?? string.Empty));
    }

    private void SetState(AppShellState nextState)
    {
        if (EqualityComparer<AppShellState>.Default.Equals(State, nextState))
        {
            return;
        }

        State = nextState;
        StateChanged?.Invoke();
    }

    private string BuildScriptScopedRoute(AppShellScreen screen)
    {
        var scriptId = State.ScriptId;

        return screen switch
        {
            AppShellScreen.Editor => AppRoutes.EditorWithId(scriptId),
            AppShellScreen.Learn => AppRoutes.LearnWithId(scriptId),
            AppShellScreen.Teleprompter => AppRoutes.TeleprompterWithId(scriptId),
            AppShellScreen.GoLive => AppRoutes.GoLiveWithId(scriptId),
            _ => AppRoutes.Library
        };
    }

    private static bool IsGoLiveRoute(string route)
        => string.Equals(GetRouteBase(route), AppRoutes.GoLive, StringComparison.Ordinal);

    private static bool IsSettingsRoute(string route) =>
        string.Equals(GetRouteBase(route), AppRoutes.Settings, StringComparison.Ordinal);

    private static bool IsTrackedRoute(string path) => path switch
    {
        AppRoutes.Library => true,
        AppRoutes.Editor => true,
        AppRoutes.Learn => true,
        AppRoutes.Teleprompter => true,
        AppRoutes.GoLive => true,
        AppRoutes.Settings => true,
        _ => false
    };

    private static bool IsValidGoLiveBackTarget(string route) =>
        !string.IsNullOrWhiteSpace(route) && !IsGoLiveRoute(route);

    private static bool IsValidSettingsBackTarget(string route) =>
        !string.IsNullOrWhiteSpace(route) && !IsSettingsRoute(route);

    private static string GetRouteBase(string route)
    {
        var querySeparatorIndex = route.IndexOf(QuerySeparator, StringComparison.Ordinal);
        return querySeparatorIndex >= 0
            ? route[..querySeparatorIndex]
            : route;
    }

    private static string NormalizeAppRoute(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
        {
            return EmptyRoute;
        }

        var normalizedPath = NormalizePath(parsedUri.AbsolutePath);
        if (!IsTrackedRoute(normalizedPath))
        {
            return EmptyRoute;
        }

        return string.IsNullOrWhiteSpace(parsedUri.Query)
            ? normalizedPath
            : string.Concat(normalizedPath, parsedUri.Query);
    }

    private static string NormalizePath(string path)
    {
        var trimmedPath = path.TrimEnd('/');
        return string.IsNullOrWhiteSpace(trimmedPath)
            ? AppRoutes.Library
            : trimmedPath;
    }
}
