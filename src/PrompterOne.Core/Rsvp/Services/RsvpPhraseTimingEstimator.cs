namespace PrompterOne.Core.Services.Rsvp;

internal static class RsvpPhraseTimingEstimator
{
    public static int EstimateWordDuration(RsvpTextProcessor.ProcessedScript script, int wordIndex, string word)
    {
        var wpm = GetSpeedForWord(script, wordIndex);
        var baseMilliseconds = 60000d / (wpm <= 0 ? RsvpTextProcessorDefaults.DefaultSegmentSpeed : wpm);
        var multiplier = RsvpTextProcessorDefaults.ActorPacingMultiplier;

        if (RsvpWordHeuristics.IsShortWord(word))
        {
            multiplier *= 1.05;
        }

        if (RsvpWordHeuristics.IsImportantWord(word))
        {
            multiplier *= 1.2;
        }

        if (RsvpWordHeuristics.HasSentenceEndingPunctuation(word))
        {
            multiplier *= 1.3;
        }
        else if (RsvpWordHeuristics.HasClausePunctuation(word))
        {
            multiplier *= 1.15;
        }

        return Math.Max(RsvpTextProcessorDefaults.MinimumWordDurationMs, (int)Math.Round(baseMilliseconds * multiplier));
    }

    public static string GetEmotionForWord(RsvpTextProcessor.ProcessedScript script, int wordIndex, string fallback)
    {
        if (script.WordEmotionOverrides.TryGetValue(wordIndex, out var emotion) && !string.IsNullOrWhiteSpace(emotion))
        {
            return emotion;
        }

        return script.WordToSegmentMap.TryGetValue(wordIndex, out var segmentIndex) &&
               segmentIndex >= 0 &&
               segmentIndex < script.Segments.Count &&
               !string.IsNullOrWhiteSpace(script.Segments[segmentIndex].Emotion)
            ? script.Segments[segmentIndex].Emotion
            : fallback;
    }

    public static int GetPauseDuration(RsvpTextProcessor.ProcessedScript script, int wordIndex, string previousWord)
    {
        if (script.PauseDurations.TryGetValue(wordIndex, out var duration) && duration > 0)
        {
            return duration;
        }

        return !string.IsNullOrEmpty(previousWord) && RsvpWordHeuristics.HasSentenceEndingPunctuation(previousWord)
            ? RsvpTextProcessorDefaults.LongPauseMs
            : RsvpTextProcessorDefaults.DefaultPauseMs;
    }

    private static int GetSpeedForWord(RsvpTextProcessor.ProcessedScript script, int wordIndex)
    {
        if (script.WordSpeedOverrides.TryGetValue(wordIndex, out var speed))
        {
            return speed;
        }

        return script.WordToSegmentMap.TryGetValue(wordIndex, out var segmentIndex) &&
               segmentIndex >= 0 &&
               segmentIndex < script.Segments.Count
            ? script.Segments[segmentIndex].Speed
            : RsvpTextProcessorDefaults.DefaultSegmentSpeed;
    }
}
