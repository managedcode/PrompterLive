namespace PrompterOne.Core.Services;

internal static class TpsTokenTextRules
{
    private const string StandaloneDashCharacters = "-—–";
    private const string StandalonePunctuationCharacters = ",.;:!?-—–…";

    public static bool IsStandalonePunctuationToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        foreach (var character in token.Trim())
        {
            if (!StandalonePunctuationCharacters.Contains(character))
            {
                return false;
            }
        }

        return true;
    }

    public static string BuildStandalonePunctuationSuffix(string token)
    {
        var trimmed = token.Trim();
        return UsesLeadingSeparator(trimmed)
            ? string.Concat(" ", trimmed)
            : trimmed;
    }

    private static bool UsesLeadingSeparator(string token)
    {
        foreach (var character in token)
        {
            if (!StandaloneDashCharacters.Contains(character))
            {
                return false;
            }
        }

        return token.Length > 0;
    }
}
