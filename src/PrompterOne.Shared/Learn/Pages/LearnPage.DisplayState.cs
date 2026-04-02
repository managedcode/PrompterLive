namespace PrompterOne.Shared.Pages;

public partial class LearnPage
{
    private static IReadOnlyList<string> BuildDisplayContextWords(IReadOnlyList<RsvpTimelineEntry> timeline, int startIndex, int endIndex)
    {
        if (startIndex >= endIndex)
        {
            return [];
        }

        return timeline
            .Skip(startIndex)
            .Take(endIndex - startIndex)
            .Select(entry => NormalizeDisplayWord(entry.Word))
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToArray();
    }

    private static IReadOnlyList<string> BuildDisplayContextWindowWords(
        IReadOnlyList<RsvpTimelineEntry> timeline,
        int startIndex,
        int endIndex,
        int maximumWordCount,
        bool takeTrailingWords)
    {
        var words = BuildDisplayContextWords(timeline, startIndex, endIndex);
        if (words.Count <= maximumWordCount)
        {
            return words;
        }

        return takeTrailingWords
            ? words.Skip(words.Count - maximumWordCount).ToArray()
            : words.Take(maximumWordCount).ToArray();
    }

    private static string BuildDisplayPreviewText(string previewText)
    {
        if (string.IsNullOrWhiteSpace(previewText) ||
            string.Equals(previewText, EndOfScriptPhrase, StringComparison.Ordinal))
        {
            return previewText;
        }

        var words = previewText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeDisplayWord)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToArray();

        return words.Length == 0
            ? string.Empty
            : string.Join(' ', words);
    }

    private static string NormalizeDisplayWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        var startIndex = 0;
        var endIndex = word.Length - 1;

        while (startIndex <= endIndex && IsDisplayBoundaryPunctuation(word[startIndex]))
        {
            startIndex++;
        }

        while (endIndex >= startIndex && IsDisplayBoundaryPunctuation(word[endIndex]))
        {
            endIndex--;
        }

        return startIndex > endIndex
            ? string.Empty
            : word[startIndex..(endIndex + 1)];
    }

    private static (int StartIndex, int EndIndex) ResolveSentenceRange(IReadOnlyList<RsvpTimelineEntry> timeline, int currentIndex)
    {
        if (timeline.Count == 0)
        {
            return (0, -1);
        }

        var safeIndex = Math.Clamp(currentIndex, 0, timeline.Count - 1);
        var entry = timeline[safeIndex];
        var startIndex = Math.Clamp(entry.SentenceStartIndex, 0, timeline.Count - 1);
        var endIndex = Math.Clamp(entry.SentenceEndIndex, startIndex, timeline.Count - 1);
        return (startIndex, endIndex);
    }

    private static bool IsDisplayBoundaryPunctuation(char character) =>
        char.IsPunctuation(character) && character is not '\'' and not '’';
}
