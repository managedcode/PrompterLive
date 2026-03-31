using PrompterLive.Core.Services.Rsvp;

namespace PrompterLive.Shared.Pages;

public partial class LearnPage
{
    private IReadOnlyList<RsvpTimelineEntry> BuildTimeline(RsvpTextProcessor.ProcessedScript processed, int fallbackSpeed)
    {
        var entries = new List<RsvpTimelineEntry>();

        for (var wordIndex = 0; wordIndex < processed.AllWords.Count; wordIndex++)
        {
            var word = processed.AllWords[wordIndex];
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            entries.Add(new RsvpTimelineEntry(
                Word: word,
                DurationMs: Math.Max(
                    MinimumWordDurationMilliseconds,
                    (int)Math.Round(PlaybackEngine.GetWordDisplayTime(wordIndex, word).TotalMilliseconds)),
                PauseAfterMs: PlaybackEngine.GetPauseAfterMilliseconds(wordIndex) ?? 0,
                BaseWpm: ResolveBaseWpm(processed, wordIndex, fallbackSpeed),
                NextPhrase: ResolveNextPhrase(processed, wordIndex),
                Emotion: ResolveEmotion(processed, wordIndex)));
        }

        return entries.Count == 0
            ? [new RsvpTimelineEntry(ReadyWord, 240, 0, Math.Max(RsvpMinSpeed, fallbackSpeed), EndOfScriptPhrase, NeutralEmotion)]
            : entries;
    }

    private string BuildProgressLabel(IReadOnlyList<RsvpTimelineEntry> timeline, int currentIndex, int wordsPerMinute)
    {
        var safeTimeline = timeline.Count == 0
            ? [new RsvpTimelineEntry(ReadyWord, 240, 0, Math.Max(RsvpMinSpeed, wordsPerMinute), EndOfScriptPhrase, NeutralEmotion)]
            : timeline;

        var remainingMilliseconds = safeTimeline
            .Skip(Math.Max(0, currentIndex + 1))
            .Sum(entry => GetScaledDuration(entry.DurationMs, entry.BaseWpm) + GetScaledDuration(entry.PauseAfterMs, entry.BaseWpm, allowZero: true));

        var remainingSeconds = (int)Math.Ceiling(remainingMilliseconds / 1000d);
        return $"Word {Math.Min(currentIndex + 1, safeTimeline.Count)} / {safeTimeline.Count} · ~{remainingSeconds / 60}:{remainingSeconds % 60:00} left";
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
                break;
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
                break;
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

    private static int ResolveBaseWpm(RsvpTextProcessor.ProcessedScript processed, int currentWordIndex, int fallback)
    {
        if (processed.WordSpeedOverrides.TryGetValue(currentWordIndex, out var wordSpeed) && wordSpeed > 0)
        {
            return wordSpeed;
        }

        if (processed.WordToSegmentMap.TryGetValue(currentWordIndex, out var segmentIndex) &&
            segmentIndex >= 0 &&
            segmentIndex < processed.Segments.Count)
        {
            return processed.Segments[segmentIndex].Speed;
        }

        return fallback;
    }

    private sealed record RsvpFocusWordViewModel(string Leading, string Orp, string Trailing);

    private sealed record RsvpTimelineEntry(
        string Word,
        int DurationMs,
        int PauseAfterMs,
        int BaseWpm,
        string NextPhrase,
        string Emotion);
}
