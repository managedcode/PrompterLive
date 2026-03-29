namespace PrompterLive.Shared.Contracts;

public static class AppRoutes
{
    public const string Root = "/";
    public const string Library = "/library";
    public const string Editor = "/editor";
    public const string Learn = "/learn";
    public const string Teleprompter = "/teleprompter";
    public const string Settings = "/settings";

    private const string ScriptIdQueryKey = "id";
    private const string QuerySeparator = "?";
    private const string KeyValueSeparator = "=";

    public static string EditorWithId(string scriptId) => BuildScoped(Editor, scriptId);

    public static string LearnWithId(string scriptId) => BuildScoped(Learn, scriptId);

    public static string TeleprompterWithId(string scriptId) => BuildScoped(Teleprompter, scriptId);

    private static string BuildScoped(string route, string scriptId)
    {
        if (string.IsNullOrWhiteSpace(scriptId))
        {
            return route;
        }

        return string.Concat(
            route,
            QuerySeparator,
            ScriptIdQueryKey,
            KeyValueSeparator,
            Uri.EscapeDataString(scriptId));
    }
}
