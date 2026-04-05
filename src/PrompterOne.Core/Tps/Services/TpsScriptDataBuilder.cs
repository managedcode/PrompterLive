using System.Globalization;
using ManagedCode.Tps;
using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Models.Tps;

namespace PrompterOne.Core.Services;

internal static class TpsScriptDataBuilder
{
    public static ScriptData Build(TpsDocument document, CompiledScript compiled, string sourceText)
    {
        var baseWpm = ResolveBaseWpm(document.Metadata);
        var segments = new List<ScriptSegment>(document.Segments.Count);
        var nextWordIndex = 0;

        for (var segmentIndex = 0; segmentIndex < document.Segments.Count; segmentIndex++)
        {
            var segment = document.Segments[segmentIndex];
            var compiledSegment = compiled.Segments.ElementAtOrDefault(segmentIndex);
            var compiledBlocksById = CreateCompiledBlockLookup(compiledSegment);
            var blocks = new List<ScriptBlock>(segment.Blocks.Count);
            var segmentStartIndex = nextWordIndex;

            foreach (var block in segment.Blocks)
            {
                compiledBlocksById.TryGetValue(block.Id, out var compiledBlock);
                blocks.Add(BuildScriptBlock(block, compiledBlock, segment.TargetWPM ?? baseWpm, ref nextWordIndex));
            }

            var (startTime, endTime) = SplitTiming(segment.Timing);
            segments.Add(new ScriptSegment
            {
                Name = segment.Name,
                Emotion = ResolveEmotion(segment.Emotion, TpsSpec.DefaultEmotion),
                Speaker = segment.Speaker,
                Archetype = NormalizeValue(segment.Archetype)?.ToLowerInvariant(),
                Timing = segment.Timing,
                BackgroundColor = segment.BackgroundColor,
                TextColor = segment.TextColor,
                AccentColor = segment.AccentColor,
                WpmOverride = segment.TargetWPM,
                StartTime = startTime,
                EndTime = endTime,
                StartIndex = segmentStartIndex,
                EndIndex = Math.Max(segmentStartIndex, nextWordIndex - 1),
                Content = !string.IsNullOrWhiteSpace(segment.LeadingContent) ? segment.LeadingContent! : segment.Content,
                Blocks = blocks.Count == 0 ? null : blocks.ToArray()
            });
        }

        return new ScriptData
        {
            Title = document.Metadata.TryGetValue(TpsSpec.FrontMatterKeys.Title, out var title) ? title : null,
            Content = sourceText,
            TargetWpm = baseWpm,
            Segments = segments.Count == 0 ? null : segments.ToArray()
        };
    }

    private static Dictionary<string, CompiledBlock> CreateCompiledBlockLookup(CompiledSegment? compiledSegment)
    {
        if (compiledSegment is null || compiledSegment.Blocks.Count == 0)
        {
            return new Dictionary<string, CompiledBlock>(StringComparer.Ordinal);
        }

        return compiledSegment.Blocks
            .Where(block => !string.IsNullOrWhiteSpace(block.Id))
            .ToDictionary(block => block.Id, StringComparer.Ordinal);
    }

    private static ScriptBlock BuildScriptBlock(TpsBlock block, CompiledBlock? compiledBlock, int fallbackWpm, ref int nextWordIndex)
    {
        var phrases = BuildScriptPhrases(compiledBlock?.Words ?? [], fallbackWpm, ref nextWordIndex);
        return new ScriptBlock
        {
            Name = block.Name,
            Emotion = NormalizeValue(block.Emotion)?.ToLowerInvariant(),
            Speaker = block.Speaker,
            Archetype = NormalizeValue(block.Archetype)?.ToLowerInvariant(),
            WpmOverride = block.TargetWPM,
            StartIndex = phrases.Length == 0 ? nextWordIndex : phrases[0].StartIndex,
            EndIndex = phrases.Length == 0 ? nextWordIndex : phrases[^1].EndIndex,
            Content = block.Content,
            Phrases = phrases.Length == 0 ? null : phrases
        };
    }

    private static ScriptPhrase[] BuildScriptPhrases(IEnumerable<CompiledWord> compiledWords, int fallbackWpm, ref int nextWordIndex)
    {
        var phrases = new List<ScriptPhrase>();
        var currentWords = new List<ScriptWord>();
        var currentPhraseStart = nextWordIndex;

        foreach (var compiledWord in compiledWords)
        {
            currentWords.Add(BuildScriptWord(compiledWord, fallbackWpm));
            nextWordIndex++;

            if (compiledWord.Metadata.IsPause || HasSentenceEndingPunctuation(compiledWord.CleanText))
            {
                phrases.Add(CreatePhrase(currentWords, currentPhraseStart, nextWordIndex - 1));
                currentWords.Clear();
                currentPhraseStart = nextWordIndex;
            }
        }

        if (currentWords.Count > 0)
        {
            phrases.Add(CreatePhrase(currentWords, currentPhraseStart, nextWordIndex - 1));
        }

        return phrases.ToArray();
    }

    private static ScriptPhrase CreatePhrase(List<ScriptWord> words, int startIndex, int endIndex)
    {
        return new ScriptPhrase
        {
            Text = string.Join(' ', words.Where(word => !string.IsNullOrWhiteSpace(word.Text)).Select(word => word.Text)),
            StartIndex = startIndex,
            EndIndex = Math.Max(startIndex, endIndex),
            Words = words.ToArray()
        };
    }

    private static ScriptWord BuildScriptWord(CompiledWord compiledWord, int fallbackWpm)
    {
        var effectiveWpm = compiledWord.Metadata.SpeedOverride
            ?? (compiledWord.Metadata.SpeedMultiplier is float multiplier
                ? Math.Max(1, (int)Math.Round(fallbackWpm * multiplier, MidpointRounding.AwayFromZero))
                : (int?)null);

        return new ScriptWord
        {
            Text = compiledWord.CleanText,
            OrpIndex = compiledWord.ORPPosition,
            WpmOverride = effectiveWpm,
            SpeedMultiplier = compiledWord.Metadata.SpeedMultiplier,
            EmphasisLevel = compiledWord.Metadata.EmphasisLevel,
            IsHighlight = compiledWord.Metadata.IsHighlight,
            IsBreath = compiledWord.Metadata.IsBreath,
            Emotion = NormalizeValue(compiledWord.Metadata.InlineEmotionHint ?? compiledWord.Metadata.EmotionHint)?.ToLowerInvariant(),
            VolumeLevel = NormalizeValue(compiledWord.Metadata.VolumeLevel)?.ToLowerInvariant(),
            DeliveryMode = NormalizeValue(compiledWord.Metadata.DeliveryMode)?.ToLowerInvariant(),
            ArticulationStyle = NormalizeValue(compiledWord.Metadata.ArticulationStyle)?.ToLowerInvariant(),
            EnergyLevel = compiledWord.Metadata.EnergyLevel,
            MelodyLevel = compiledWord.Metadata.MelodyLevel,
            PauseAfter = compiledWord.Metadata.PauseDuration,
            Pronunciation = compiledWord.Metadata.PronunciationGuide,
            StressText = compiledWord.Metadata.StressText,
            StressGuide = compiledWord.Metadata.StressGuide,
            Speaker = compiledWord.Metadata.Speaker,
            IsEditPoint = compiledWord.Metadata.IsEditPoint,
            EditPointPriority = NormalizeValue(compiledWord.Metadata.EditPointPriority)?.ToLowerInvariant()
        };
    }

    private static (string? Start, string? End) SplitTiming(string? timing)
    {
        var trimmed = NormalizeValue(timing);
        if (trimmed is null)
        {
            return (null, null);
        }

        var separatorIndex = trimmed.IndexOf('-', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return (trimmed, trimmed);
        }

        return (trimmed[..separatorIndex], trimmed[(separatorIndex + 1)..]);
    }

    private static int ResolveBaseWpm(IReadOnlyDictionary<string, string> metadata)
    {
        return metadata.TryGetValue(TpsSpec.FrontMatterKeys.BaseWpm, out var value) &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Clamp(parsed, TpsSpec.MinimumWpm, TpsSpec.MaximumWpm)
            : TpsSpec.DefaultBaseWpm;
    }

    private static string ResolveEmotion(string? emotion, string fallback)
    {
        var normalized = NormalizeValue(emotion)?.ToLowerInvariant();
        return normalized is not null && TpsSpec.Emotions.Contains(normalized)
            ? normalized
            : fallback;
    }

    private static bool HasSentenceEndingPunctuation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var lastCharacter = value.TrimEnd()[^1];
        return lastCharacter is '.' or '!' or '?';
    }

    private static string? NormalizeValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
