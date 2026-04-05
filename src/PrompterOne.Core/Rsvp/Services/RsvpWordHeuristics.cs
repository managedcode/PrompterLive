namespace PrompterOne.Core.Services.Rsvp;

internal static class RsvpWordHeuristics
{
    public static bool IsImportantWord(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return false;
        }

        return (word.Length > 2 && word == word.ToUpperInvariant()) ||
               word.Contains('!') ||
               word.Contains('?') ||
               word.Contains(':');
    }

    public static bool IsShortWord(string word) =>
        !string.IsNullOrEmpty(word) && word.Length <= 3;

    public static bool HasPunctuation(string word) =>
        !string.IsNullOrEmpty(word) && word.Any(",.;:!?".Contains);

    public static bool HasSentenceEndingPunctuation(string word) =>
        word.Any(character => character is '.' or '!' or '?');

    public static bool HasClausePunctuation(string word) =>
        word.Any(character => character is ',' or ';' or ':' or '—' or '–');

    public static bool EndsWithStrongPause(string word) =>
        word.EndsWith('.') || word.EndsWith('!') || word.EndsWith('?');
}
