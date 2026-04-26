namespace PrompterOne.Shared.Contracts;

public static class AppRoutes
{
    public const string ScriptIdQueryKey = "id";
    public const string OnboardingQueryKey = "onboarding";
    public const string OnboardingQueryValue = "1";
    public const string Root = "/";
    public const string Library = "/library";
    public const string Editor = "/editor";
    public const string Learn = "/learn";
    public const string Prep = "/prep";
    public const string Teleprompter = "/teleprompter";
    public const string GoLive = "/go-live";
    public const string Settings = "/settings";

    private const string QuerySeparator = "?";
    private const string KeyValueSeparator = "=";

    public static string EditorWithId(string scriptId) => BuildScoped(Editor, scriptId);

    public static string LearnWithId(string scriptId) => BuildScoped(Learn, scriptId);

    public static string PrepWithId(string scriptId) => BuildScoped(Prep, scriptId);

    public static string TeleprompterWithId(string scriptId) => BuildScoped(Teleprompter, scriptId);

    public static string GoLiveWithId(string scriptId) => BuildScoped(GoLive, scriptId);

    public static string LibraryWithOnboarding() => BuildQuery(Library, OnboardingQueryKey, OnboardingQueryValue);

    private static string BuildScoped(string route, string scriptId)
    {
        return BuildQuery(route, ScriptIdQueryKey, scriptId);
    }

    private static string BuildQuery(string route, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return route;
        }

        return string.Concat(
            route,
            QuerySeparator,
            key,
            KeyValueSeparator,
            Uri.EscapeDataString(value));
    }
}
