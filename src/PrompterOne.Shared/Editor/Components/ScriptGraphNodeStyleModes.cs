namespace PrompterOne.Shared.Components.Editor;

public static class ScriptGraphNodeStyleModes
{
    public const string Compact = "compact";
    public const string Cards = "cards";
    public const string Dots = "dots";

    private static readonly string[] AllModes =
    [
        Compact, Cards, Dots
    ];

    public static string Normalize(string? value) =>
        Array.Exists(AllModes, mode => string.Equals(mode, value, StringComparison.Ordinal))
            ? value!
            : Compact;
}
