using PrompterOne.Core.Services.Rsvp;

namespace PrompterOne.Shared.Pages;

public partial class LearnPage
{
    private static IReadOnlyList<RsvpTimelineEntry> BuildTimeline(RsvpTextProcessor.ProcessedScript processed)
    {
        var entries = new List<RsvpTimelineEntry>();
        var timelineIndexByWordIndex = new Dictionary<int, int>();

        for (var wordIndex = 0; wordIndex < processed.AllWords.Count; wordIndex++)
        {
            var word = processed.AllWords[wordIndex];
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            var timelineIndex = entries.Count;
            timelineIndexByWordIndex[wordIndex] = timelineIndex;
            entries.Add(new RsvpTimelineEntry(
                WordIndex: wordIndex,
                Word: word,
                SentenceStartIndex: timelineIndex,
                SentenceEndIndex: timelineIndex,
                NextPhrase: ResolveNextPhrase(processed, wordIndex),
                Emotion: ResolveEmotion(processed, wordIndex)));
        }

        return entries.Count == 0
            ? [new RsvpTimelineEntry(-1, ReadyWord, 0, 0, EndOfScriptPhrase, NeutralEmotion)]
            : ApplySentenceRanges(entries, processed.AllWords, timelineIndexByWordIndex);
    }

    private string BuildProgressLabel(IReadOnlyList<RsvpTimelineEntry> timeline, int currentIndex)
    {
        if (timeline.Count == 0)
        {
            return string.Empty;
        }

        var safeCurrentIndex = Math.Clamp(currentIndex, 0, timeline.Count - 1);
        var remainingMilliseconds = timeline
            .Skip(safeCurrentIndex + 1)
            .Sum(GetTimelineEntryPlaybackMilliseconds);

        var remainingSeconds = (int)Math.Ceiling(remainingMilliseconds / 1000d);
        return $"Word {safeCurrentIndex + 1} / {timeline.Count} · ~{remainingSeconds / 60}:{remainingSeconds % 60:00} left";
    }

    private static string BuildWpmLabel(int speed) => string.Concat(speed, WpmSuffix);

    private static RsvpFocusWordViewModel BuildFocusWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return new RsvpFocusWordViewModel(string.Empty, ReadyWord, string.Empty);
        }

        var orpIndex = GetOrpIndex(word);
        if (orpIndex >= word.Length)
        {
            orpIndex = word.Length - 1;
        }

        return new RsvpFocusWordViewModel(
            word[..orpIndex],
            word[orpIndex].ToString(),
            word[(orpIndex + 1)..]);
    }

    private static int GetOrpIndex(string word)
    {
        var readableLength = word.Count(char.IsLetter);
        return readableLength switch
        {
            <= 1 => 0,
            <= 5 => 1,
            <= 9 => 2,
            _ => 3
        };
    }

    private static string ResolveFallbackNextPhrase(IReadOnlyList<RsvpTimelineEntry> timeline, int currentIndex)
    {
        var fallbackWords = timeline
            .Skip(currentIndex + 1)
            .Select(entry => entry.Word)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Take(PreviewWordCount)
            .ToArray();

        return fallbackWords.Length == 0
            ? EndOfScriptPhrase
            : string.Join(' ', fallbackWords);
    }

    private static string ResolveNextPhrase(RsvpTextProcessor.ProcessedScript processed, int currentWordIndex)
    {
        var currentSentencePreview = ResolveCurrentSentencePreview(processed.AllWords, currentWordIndex);
        if (!string.IsNullOrWhiteSpace(currentSentencePreview))
        {
            return currentSentencePreview;
        }

        var fallbackWords = processed.AllWords
            .Skip(currentWordIndex + 1)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .Take(PreviewWordCount)
            .ToArray();

        return fallbackWords.Length == 0
            ? EndOfScriptPhrase
            : string.Join(' ', fallbackWords);
    }

    private static string ResolveCurrentSentencePreview(IReadOnlyList<string> words, int currentWordIndex)
    {
        if (words.Count == 0)
        {
            return string.Empty;
        }

        var safeIndex = Math.Clamp(currentWordIndex, 0, words.Count - 1);
        if (string.IsNullOrWhiteSpace(words[safeIndex]))
        {
            return string.Empty;
        }

        var startIndex = FindSentenceStartIndex(words, safeIndex);
        var endIndex = FindSentenceEndIndex(words, safeIndex);
        var previewWords = words
            .Skip(startIndex)
            .Take(endIndex - startIndex + 1)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToArray();

        return previewWords.Length == 0
            ? string.Empty
            : string.Join(' ', previewWords);
    }

    private static int FindSentenceStartIndex(IReadOnlyList<string> words, int currentWordIndex)
    {
        var startIndex = currentWordIndex;

        for (var index = currentWordIndex - 1; index >= 0; index--)
        {
            var candidate = words[index];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                if (StartsWithUppercase(words[startIndex]))
                {
                    break;
                }

                continue;
            }

            if (HasSentenceEndingPunctuation(candidate))
            {
                break;
            }

            startIndex = index;
        }

        return startIndex;
    }

    private static int FindSentenceEndIndex(IReadOnlyList<string> words, int currentWordIndex)
    {
        var endIndex = currentWordIndex;

        for (var index = currentWordIndex + 1; index < words.Count; index++)
        {
            var candidate = words[index];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                if (HasCapitalizedWordAhead(words, index + 1))
                {
                    break;
                }

                continue;
            }

            endIndex = index;
            if (HasSentenceEndingPunctuation(candidate))
            {
                break;
            }
        }

        return endIndex;
    }

    private static bool HasSentenceEndingPunctuation(string word) => word.IndexOfAny(['.', '!', '?']) >= 0;

    private static bool StartsWithUppercase(string word)
    {
        var firstLetter = word.FirstOrDefault(char.IsLetter);
        return firstLetter != default && char.IsUpper(firstLetter);
    }

    private static bool HasCapitalizedWordAhead(IReadOnlyList<string> words, int startIndex)
    {
        for (var index = startIndex; index < words.Count; index++)
        {
            var candidate = words[index];
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            return StartsWithUppercase(candidate);
        }

        return false;
    }

    private static string ResolveEmotion(RsvpTextProcessor.ProcessedScript processed, int currentWordIndex)
    {
        if (processed.WordEmotionOverrides.TryGetValue(currentWordIndex, out var emotion) &&
            !string.IsNullOrWhiteSpace(emotion))
        {
            return emotion;
        }

        if (processed.WordToSegmentMap.TryGetValue(currentWordIndex, out var segmentIndex) &&
            segmentIndex >= 0 &&
            segmentIndex < processed.Segments.Count)
        {
            return processed.Segments[segmentIndex].Emotion;
        }

        return NeutralEmotion;
    }

    private static IReadOnlyList<RsvpTimelineEntry> ApplySentenceRanges(
        List<RsvpTimelineEntry> entries,
        IReadOnlyList<string> allWords,
        IReadOnlyDictionary<int, int> timelineIndexByWordIndex)
    {
        foreach (var (wordIndex, timelineIndex) in timelineIndexByWordIndex)
        {
            var sentenceStartWordIndex = FindSentenceStartIndex(allWords, wordIndex);
            var sentenceEndWordIndex = FindSentenceEndIndex(allWords, wordIndex);
            var sentenceStartIndex = ResolveSentenceBoundaryTimelineIndex(
                timelineIndexByWordIndex,
                sentenceStartWordIndex,
                sentenceEndWordIndex,
                step: 1);
            var sentenceEndIndex = ResolveSentenceBoundaryTimelineIndex(
                timelineIndexByWordIndex,
                sentenceEndWordIndex,
                sentenceStartWordIndex,
                step: -1);

            entries[timelineIndex] = entries[timelineIndex] with
            {
                SentenceStartIndex = sentenceStartIndex,
                SentenceEndIndex = sentenceEndIndex
            };
        }

        return entries;
    }

    private static int ResolveSentenceBoundaryTimelineIndex(
        IReadOnlyDictionary<int, int> timelineIndexByWordIndex,
        int startWordIndex,
        int endWordIndex,
        int step)
    {
        if (timelineIndexByWordIndex.TryGetValue(startWordIndex, out var directMatch))
        {
            return directMatch;
        }

        for (var wordIndex = startWordIndex; step > 0 ? wordIndex <= endWordIndex : wordIndex >= endWordIndex; wordIndex += step)
        {
            if (timelineIndexByWordIndex.TryGetValue(wordIndex, out var resolvedIndex))
            {
                return resolvedIndex;
            }
        }

        return 0;
    }

    private int GetTimelineEntryDelayMilliseconds(RsvpTimelineEntry entry)
    {
        var playbackMilliseconds = GetTimelineEntryPlaybackMilliseconds(entry);
        return Math.Max(MinimumLoopDelayMilliseconds, playbackMilliseconds);
    }

    private int GetTimelineEntryPlaybackMilliseconds(RsvpTimelineEntry entry) =>
        GetTimelineEntryWordDurationMilliseconds(entry) + GetTimelineEntryPauseMilliseconds(entry);

    private int GetTimelineEntryWordDurationMilliseconds(RsvpTimelineEntry entry)
    {
        if (entry.WordIndex < 0)
        {
            return ReadyWordDurationMilliseconds;
        }

        return Math.Max(
            MinimumWordDurationMilliseconds,
            (int)Math.Round(PlaybackEngine.GetWordDisplayTime(entry.WordIndex, entry.Word).TotalMilliseconds));
    }

    private int GetTimelineEntryPauseMilliseconds(RsvpTimelineEntry entry)
    {
        if (entry.WordIndex < 0)
        {
            return 0;
        }

        return PlaybackEngine.GetPauseAfterMilliseconds(entry.WordIndex) ?? 0;
    }

    private sealed record RsvpFocusWordViewModel(string Leading, string Orp, string Trailing);

    private sealed record RsvpTimelineEntry(
        int WordIndex,
        string Word,
        int SentenceStartIndex,
        int SentenceEndIndex,
        string NextPhrase,
        string Emotion);
}
