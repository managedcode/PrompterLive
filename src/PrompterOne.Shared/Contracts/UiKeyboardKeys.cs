namespace PrompterOne.Shared.Contracts;

public static class UiKeyboardKeys
{
    public const string ALower = "a";
    public const string ArrowDown = "ArrowDown";
    public const string ArrowLeft = "ArrowLeft";
    public const string ArrowRight = "ArrowRight";
    public const string ArrowUp = "ArrowUp";
    public const string BracketLeft = "[";
    public const string BracketRight = "]";
    public const string CLower = "c";
    public const string CameraLower = "c";
    public const string CameraUpper = "C";
    public const string Digit1 = "1";
    public const string Digit2 = "2";
    public const string Digit3 = "3";
    public const string Digit4 = "4";
    public const string Enter = "Enter";
    public const string Escape = "Escape";
    public const string FLower = "f";
    public const string HLower = "h";
    public const string KLower = "k";
    public const string LLower = "l";
    public const string OLower = "o";
    public const string PageDown = "PageDown";
    public const string PageUp = "PageUp";
    public const string RLower = "r";
    public const string SLower = "s";
    public const string SpaceNamed = "Space";
    public const string Spacebar = "Spacebar";
    public const string Space = " ";
    public const string VLower = "v";
    public const string ZLower = "z";

    public static string Normalize(string? key)
    {
        if (key is null)
        {
            return string.Empty;
        }

        if (string.Equals(key, Space, StringComparison.Ordinal)
            || string.Equals(key, SpaceNamed, StringComparison.Ordinal)
            || string.Equals(key, Spacebar, StringComparison.Ordinal))
        {
            return Space;
        }

        if (key.Length == 0)
        {
            return string.Empty;
        }

        return key.Length == 1
            ? key.ToLowerInvariant()
            : key;
    }
}
