using PrompterOne.Core.Models.Documents;

namespace PrompterOne.Core.Services.Rsvp;

internal static class RsvpProcessedWordAppender
{
    public static void AppendBlock(
        RsvpTextProcessor.ProcessedScript processed,
        RsvpTextProcessor.ProcessedSegment processedSegment,
        ScriptBlock block,
        int segmentSpeed,
        int segmentIndex)
    {
        var blockSpeed = RsvpProcessedScriptProjector.ResolveSegmentSpeed(block.WpmOverride ?? segmentSpeed, usePlainTextReadingDefaults: false);
        var blockEmotion = RsvpProcessedScriptProjector.ResolveEmotion(block.Emotion, processedSegment.Emotion);

        foreach (var phrase in EnumeratePhrases(block))
        {
            AppendPhrase(processed, processedSegment, phrase, blockSpeed, blockEmotion, segmentIndex);
        }
    }

    private static void AppendPhrase(
        RsvpTextProcessor.ProcessedScript processed,
        RsvpTextProcessor.ProcessedSegment processedSegment,
        ScriptPhrase phrase,
        int blockSpeed,
        string blockEmotion,
        int segmentIndex)
    {
        foreach (var word in EnumerateWords(phrase))
        {
            AppendWord(processed, processedSegment, word, blockSpeed, blockEmotion, segmentIndex);
        }

        if (phrase.PauseDuration is > 0)
        {
            AppendPause(processed, processedSegment, phrase.PauseDuration.Value, segmentIndex);
        }
    }

    private static void AppendWord(
        RsvpTextProcessor.ProcessedScript processed,
        RsvpTextProcessor.ProcessedSegment processedSegment,
        ScriptWord word,
        int blockSpeed,
        string blockEmotion,
        int segmentIndex)
    {
        var cleanedWord = NormalizeWordText(word.Text);
        if (!string.IsNullOrEmpty(cleanedWord))
        {
            var wordIndex = processed.AllWords.Count;
            processed.AllWords.Add(cleanedWord);
            processedSegment.Words.Add(cleanedWord);
            processed.WordToSegmentMap[wordIndex] = segmentIndex;

            var wordSpeed = RsvpProcessedScriptProjector.ResolveSegmentSpeed(word.WpmOverride ?? blockSpeed, usePlainTextReadingDefaults: false);
            if (wordSpeed != processedSegment.Speed)
            {
                processed.WordSpeedOverrides[wordIndex] = wordSpeed;
            }

            var wordEmotion = RsvpProcessedScriptProjector.ResolveEmotion(word.Emotion, blockEmotion);
            if (!string.Equals(wordEmotion, processedSegment.Emotion, StringComparison.Ordinal))
            {
                processed.WordEmotionOverrides[wordIndex] = wordEmotion;
            }

            if (!string.IsNullOrWhiteSpace(word.Color))
            {
                processed.WordColorOverrides[wordIndex] = word.Color.Trim();
            }
        }

        if (word.PauseAfter is > 0)
        {
            AppendPause(processed, processedSegment, word.PauseAfter.Value, segmentIndex);
        }
    }

    private static void AppendPause(
        RsvpTextProcessor.ProcessedScript processed,
        RsvpTextProcessor.ProcessedSegment processedSegment,
        int pauseDuration,
        int segmentIndex)
    {
        var pauseIndex = processed.AllWords.Count;
        processed.AllWords.Add(string.Empty);
        processedSegment.Words.Add(string.Empty);
        processed.WordToSegmentMap[pauseIndex] = segmentIndex;
        processed.PauseDurations[pauseIndex] = pauseDuration;
    }

    private static IEnumerable<ScriptPhrase> EnumeratePhrases(ScriptBlock block)
    {
        if (block.Phrases is { Length: > 0 } phrases)
        {
            return phrases;
        }

        return
        [
            new ScriptPhrase
            {
                Text = block.Content ?? string.Empty
            }
        ];
    }

    private static IEnumerable<ScriptWord> EnumerateWords(ScriptPhrase phrase)
    {
        if (phrase.Words is { Length: > 0 } words)
        {
            return words;
        }

        return string.IsNullOrWhiteSpace(phrase.Text)
            ? []
            : phrase.Text
                .Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Select(token => new ScriptWord
                {
                    Text = token
                });
    }

    private static string NormalizeWordText(string? text) =>
        string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
}
