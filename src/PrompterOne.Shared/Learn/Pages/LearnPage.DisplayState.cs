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

    private static (int StartIndex, int EndIndex) ResolveSentenceRange(IReadOnlyList<RsvpTimelineEntry> timeline, int currentIndex) =>
        (FindSentenceStartIndex(timeline, currentIndex), FindSentenceEndIndex(timeline, currentIndex));

    private static int FindSentenceStartIndex(IReadOnlyList<RsvpTimelineEntry> timeline, int currentIndex)
    {
        var startIndex = currentIndex;

        for (var index = currentIndex - 1; index >= 0; index--)
        {
            if (HasSentenceBoundaryAfter(timeline[index]))
            {
                break;
            }

            startIndex = index;
        }

        return startIndex;
    }

    private static int FindSentenceEndIndex(IReadOnlyList<RsvpTimelineEntry> timeline, int currentIndex)
    {
        var endIndex = currentIndex;

        for (var index = currentIndex + 1; index < timeline.Count; index++)
        {
            endIndex = index;
            if (HasSentenceBoundaryAfter(timeline[index]))
            {
                break;
            }
        }

        return endIndex;
    }

    private static bool HasSentenceBoundaryAfter(RsvpTimelineEntry entry) =>
        HasSentenceEndingPunctuation(entry.Word);

    private static bool IsDisplayBoundaryPunctuation(char character) =>
        char.IsPunctuation(character) && character is not '\'' and not '’';
}
