using System.Globalization;
using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const string DefaultReaderBlockName = "Reader Block";
    private const string DefaultReaderSectionName = "Section";
    private const int LongPauseThresholdMilliseconds = 1500;
    private const int MediumPauseThresholdMilliseconds = 600;
    private const int MinimumReaderWordDurationMilliseconds = 120;
    private const int MinimumPauseDurationMilliseconds = 250;
    private const string NeutralEmotionKey = "neutral";
    private const string ReaderPauseCssClass = "rd-pause";
    private const string ReaderPauseLongCssClass = "rd-pause rd-pause-long";
    private const string ReaderPauseMediumCssClass = "rd-pause rd-pause-med";
    private const string TpsClassPrefix = "tps";

    private Task<List<ReaderCardViewModel>> BuildReaderCardsAsync() =>
        Task.FromResult(BuildReaderCards());

    private List<ReaderCardViewModel> BuildReaderCards()
    {
        var scriptData = SessionService.State.ScriptData;
        if (scriptData?.Segments is not { Length: > 0 })
        {
            return [];
        }

        var seeds = BuildReaderCardSeeds(scriptData, SessionService.State.CompiledScript);
        if (seeds.Count == 0)
        {
            return [];
        }

        return BuildReaderCardViewModels(seeds);
    }

    private static List<ReaderCardSeed> BuildReaderCardSeeds(
        ScriptData scriptData,
        PrompterOne.Core.Models.CompiledScript.CompiledScript? compiledScript)
    {
        var seeds = new List<ReaderCardSeed>();
        for (var segmentIndex = 0; segmentIndex < scriptData.Segments!.Length; segmentIndex++)
        {
            var segment = scriptData.Segments[segmentIndex];
            var compiledSegment = compiledScript?.Segments.ElementAtOrDefault(segmentIndex);
            AppendReaderCardSeeds(seeds, segment, compiledSegment, scriptData.TargetWpm);
        }

        return seeds;
    }

    private static void AppendReaderCardSeeds(
        ICollection<ReaderCardSeed> seeds,
        ScriptSegment segment,
        CompiledSegment? compiledSegment,
        int baseWpm)
    {
        var segmentEmotion = ResolveEmotionKey(segment.Emotion);
        var segmentAccent = ResolveAccentColor(segment.AccentColor, segmentEmotion);
        var blocks = ResolveReaderBlocks(segment);
        var hasExplicitBlocks = segment.Blocks is { Length: > 0 };

        for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
        {
            var block = blocks[blockIndex];
            var words = ResolveCompiledWords(compiledSegment, block, blockIndex, hasExplicitBlocks);
            var wordCount = CountReadableWords(words);
            if (wordCount == 0)
            {
                continue;
            }

            var emotionKey = ResolveEmotionKey(block.Emotion ?? segment.Emotion);
            var targetWpm = block.WpmOverride ?? segment.WpmOverride ?? baseWpm;
            var durationMilliseconds = Math.Max(
                1000,
                (int)Math.Ceiling(words.Sum(word => word.DisplayDuration.TotalMilliseconds)));

            seeds.Add(new ReaderCardSeed(
                SectionName: string.IsNullOrWhiteSpace(segment.Name) ? DefaultReaderSectionName : segment.Name,
                DisplayName: string.IsNullOrWhiteSpace(block.Name) ? segment.Name : block.Name,
                EmotionKey: emotionKey,
                EmotionLabel: FormatEmotionLabel(emotionKey),
                BackgroundClass: ResolveReaderBackgroundClass(emotionKey),
                AccentColor: segmentAccent,
                TargetWpm: targetWpm,
                WordCount: wordCount,
                DurationMilliseconds: durationMilliseconds,
                WidthPercentString: string.Empty,
                EdgeColor: string.Empty,
                Chunks: BuildReaderChunks(words, targetWpm)));
        }
    }

    private static IReadOnlyList<ScriptBlock> ResolveReaderBlocks(ScriptSegment segment)
    {
        if (segment.Blocks is { Length: > 0 } blocks)
        {
            return blocks;
        }

        return
        [
            new ScriptBlock
            {
                Name = string.IsNullOrWhiteSpace(segment.Name) ? DefaultReaderBlockName : segment.Name,
                Emotion = segment.Emotion,
                WpmOverride = segment.WpmOverride,
                Content = segment.Content
            }
        ];
    }

    private static IReadOnlyList<CompiledWord> ResolveCompiledWords(
        CompiledSegment? compiledSegment,
        ScriptBlock block,
        int blockIndex,
        bool hasExplicitBlocks)
    {
        if (compiledSegment is null)
        {
            return [];
        }

        if (!hasExplicitBlocks)
        {
            return compiledSegment.Words;
        }

        var matchedBlock = compiledSegment.Blocks
            .FirstOrDefault(candidate => string.Equals(candidate.Name, block.Name, StringComparison.Ordinal));
        if (matchedBlock is not null)
        {
            return matchedBlock.Words;
        }

        return compiledSegment.Blocks.ElementAtOrDefault(blockIndex + 1)?.Words
            ?? compiledSegment.Blocks.ElementAtOrDefault(blockIndex)?.Words
            ?? [];
    }

    private static List<ReaderCardViewModel> BuildReaderCardViewModels(IReadOnlyList<ReaderCardSeed> seeds)
    {
        var totalWords = Math.Max(1, seeds.Sum(seed => seed.WordCount));

        return seeds
            .Select((seed, index) => new ReaderCardViewModel(
                SectionName: seed.SectionName,
                DisplayName: seed.DisplayName,
                EmotionKey: seed.EmotionKey,
                EmotionLabel: seed.EmotionLabel,
                BackgroundClass: seed.BackgroundClass,
                AccentColor: seed.AccentColor,
                TargetWpm: seed.TargetWpm,
                WordCount: seed.WordCount,
                DurationMilliseconds: seed.DurationMilliseconds,
                WidthPercentString: $"{Math.Max(8d, seed.WordCount * 100d / totalWords):0.##}%",
                EdgeColor: ToAlphaColor(seed.AccentColor, 0.35),
                Chunks: seed.Chunks,
                TestId: UiTestIds.Teleprompter.Card(index)))
            .ToList();
    }

    private static IReadOnlyList<ReaderChunkViewModel> BuildReaderChunks(IEnumerable<CompiledWord> words, int targetWpm)
    {
        var compiledWords = words.ToList();
        var chunks = new List<ReaderChunkViewModel>();
        var currentGroup = new List<ReaderWordViewModel>();
        var currentCharacterCount = 0;
        bool? currentGroupIsEmphasis = null;

        for (var compiledWordIndex = 0; compiledWordIndex < compiledWords.Count; compiledWordIndex++)
        {
            var word = compiledWords[compiledWordIndex];
            if (word.Metadata?.IsPause == true)
            {
                var pauseDuration = Math.Max(MinimumPauseDurationMilliseconds, word.Metadata.PauseDuration ?? MinimumPauseDurationMilliseconds);
                if (currentGroup.Count > 0)
                {
                    var lastWord = currentGroup[^1];
                    currentGroup[^1] = lastWord with { PauseAfterMs = pauseDuration };
                }

                FlushGroup(chunks, currentGroup, currentGroupIsEmphasis ?? false);
                currentCharacterCount = 0;
                currentGroupIsEmphasis = null;
                chunks.Add(new ReaderPauseViewModel(
                    pauseDuration,
                    pauseDuration >= LongPauseThresholdMilliseconds
                        ? ReaderPauseLongCssClass
                        : pauseDuration >= MediumPauseThresholdMilliseconds
                            ? ReaderPauseMediumCssClass
                            : ReaderPauseCssClass));
                continue;
            }

            if (string.IsNullOrWhiteSpace(word.CleanText))
            {
                continue;
            }

            var isEmphasisWord = IsReaderWordEmphasis(word.Metadata);
            if (currentGroup.Count > 0 &&
                currentGroupIsEmphasis.HasValue &&
                currentGroupIsEmphasis.Value != isEmphasisWord)
            {
                FlushGroup(chunks, currentGroup, currentGroupIsEmphasis.Value);
                currentCharacterCount = 0;
                currentGroupIsEmphasis = null;
            }

            currentGroupIsEmphasis ??= isEmphasisWord;
            currentCharacterCount += word.CleanText.Length;
            if (currentGroup.Count > 0)
            {
                currentCharacterCount += 1;
            }

            var effectiveWpm = ResolveEffectiveWpm(word.Metadata, targetWpm);
            var speedCueValue = ResolveReaderSpeedCueValue(targetWpm, effectiveWpm);
            currentGroup.Add(new ReaderWordViewModel(
                Text: word.CleanText,
                CssClass: BuildReaderWordBaseClass(word.Metadata, speedCueValue),
                DurationMs: Math.Max(MinimumReaderWordDurationMilliseconds, (int)Math.Round(word.DisplayDuration.TotalMilliseconds)),
                Style: BuildReaderWordStyle(
                    word.Metadata,
                    targetWpm,
                    effectiveWpm,
                    ResolveReaderCueProgress(compiledWords, compiledWordIndex)),
                PronunciationGuide: string.IsNullOrWhiteSpace(word.Metadata?.PronunciationGuide) ? null : word.Metadata.PronunciationGuide.Trim(),
                EffectiveWpm: effectiveWpm,
                Attributes: BuildReaderWordAttributes(word.Metadata, speedCueValue)));

            if (ShouldEndReaderGroup(word.CleanText, currentGroup.Count, currentCharacterCount))
            {
                FlushGroup(chunks, currentGroup, currentGroupIsEmphasis ?? false);
                currentCharacterCount = 0;
                currentGroupIsEmphasis = null;
            }
        }

        FlushGroup(chunks, currentGroup, currentGroupIsEmphasis ?? false);
        return chunks;
    }

    private static bool ShouldEndReaderGroup(string cleanText, int wordCount, int characterCount)
    {
        if (wordCount >= MaxReaderGroupWordCount || characterCount >= MaxReaderGroupCharacterCount)
        {
            return true;
        }

        if (HasSentenceEndingPunctuation(cleanText))
        {
            return true;
        }

        return HasClausePunctuation(cleanText) && wordCount >= 3;
    }

    private static void FlushGroup(List<ReaderChunkViewModel> chunks, List<ReaderWordViewModel> currentGroup, bool isEmphasis)
    {
        if (currentGroup.Count == 0)
        {
            return;
        }

        chunks.Add(new ReaderGroupViewModel(currentGroup.ToArray(), isEmphasis));
        currentGroup.Clear();
    }

    private static bool IsReaderWordEmphasis(WordMetadata? metadata) =>
        metadata?.IsEmphasis == true;

    private static string BuildReaderWordBaseClass(WordMetadata? metadata, string? speedCueValue)
    {
        if (metadata is null)
        {
            return string.Empty;
        }

        var classes = new List<string>();

        if (metadata.IsHighlight)
        {
            classes.Add($"{TpsClassPrefix}-highlight");
        }

        var emotionClass = ResolveEmotionWordClass(metadata.InlineEmotionHint, TpsClassPrefix);
        if (!string.IsNullOrWhiteSpace(emotionClass))
        {
            classes.Add(emotionClass);
        }

        var volumeClass = ResolveSemanticWordClass(metadata.VolumeLevel, TpsClassPrefix);
        if (!string.IsNullOrWhiteSpace(volumeClass))
        {
            classes.Add(volumeClass);
        }

        var deliveryClass = ResolveSemanticWordClass(metadata.DeliveryMode, TpsClassPrefix);
        if (!string.IsNullOrWhiteSpace(deliveryClass))
        {
            classes.Add(deliveryClass);
        }

        if (!string.IsNullOrWhiteSpace(metadata.StressText) || !string.IsNullOrWhiteSpace(metadata.StressGuide))
        {
            classes.Add($"{TpsClassPrefix}-stress");
        }

        if (!string.IsNullOrWhiteSpace(speedCueValue))
        {
            classes.Add($"{TpsClassPrefix}-{speedCueValue}");
        }

        if (!string.IsNullOrWhiteSpace(metadata.PronunciationGuide))
        {
            classes.Add("tps-phonetic");
        }

        return string.Join(' ', classes);
    }

    private static IReadOnlyDictionary<string, object>? BuildReaderWordAttributes(WordMetadata? metadata, string? speedCueValue)
    {
        if (metadata is null)
        {
            return null;
        }

        Dictionary<string, object>? attributes = null;
        var emotionCueValue = ResolveEmotionKey(metadata.InlineEmotionHint, string.Empty);
        AddReaderWordAttribute(ref attributes, TpsVisualCueContracts.EmotionAttributeName, emotionCueValue);
        AddReaderWordAttribute(ref attributes, TpsVisualCueContracts.VolumeAttributeName, NormalizeCueValue(metadata.VolumeLevel));
        AddReaderWordAttribute(ref attributes, TpsVisualCueContracts.DeliveryAttributeName, NormalizeCueValue(metadata.DeliveryMode));
        AddReaderWordAttribute(ref attributes, TpsVisualCueContracts.SpeedAttributeName, speedCueValue);

        if (metadata.IsHighlight)
        {
            AddReaderWordAttribute(ref attributes, TpsVisualCueContracts.HighlightAttributeName, TpsVisualCueContracts.HighlightAttributeValue);
        }

        if (!string.IsNullOrWhiteSpace(metadata.StressText) || !string.IsNullOrWhiteSpace(metadata.StressGuide))
        {
            AddReaderWordAttribute(ref attributes, TpsVisualCueContracts.StressAttributeName, TpsVisualCueContracts.StressAttributeValue);
        }

        return attributes;
    }

    private static string? NormalizeCueValue(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();

    private static void AddReaderWordAttribute(ref Dictionary<string, object>? attributes, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        attributes ??= new Dictionary<string, object>(StringComparer.Ordinal);
        attributes[name] = value;
    }

    private static double ResolveReaderCueProgress(IReadOnlyList<CompiledWord> words, int currentIndex)
    {
        if (currentIndex < 0 || currentIndex >= words.Count || !IsBuildingCueWord(words[currentIndex]))
        {
            return 0d;
        }

        var startIndex = currentIndex;
        while (startIndex > 0 && IsBuildingCueWord(words[startIndex - 1]))
        {
            startIndex--;
        }

        var endIndex = currentIndex;
        while (endIndex < words.Count - 1 && IsBuildingCueWord(words[endIndex + 1]))
        {
            endIndex++;
        }

        var runLength = endIndex - startIndex + 1;
        if (runLength <= 1)
        {
            return 1d;
        }

        return (currentIndex - startIndex) / (double)(runLength - 1);
    }

    private static bool IsBuildingCueWord(CompiledWord word) =>
        word.Metadata?.IsPause != true &&
        !string.IsNullOrWhiteSpace(word.CleanText) &&
        string.Equals(
            NormalizeCueValue(word.Metadata?.DeliveryMode),
            TpsVisualCueContracts.DeliveryModeBuilding,
            StringComparison.Ordinal);

    private static int CountReadableWords(IEnumerable<CompiledWord> words) =>
        words.Count(word => word.Metadata?.IsPause != true && !string.IsNullOrWhiteSpace(word.CleanText));

    private static string ResolveAccentColor(string? accentColor, string emotionKey)
    {
        if (!string.IsNullOrWhiteSpace(accentColor))
        {
            return accentColor;
        }

        return emotionKey switch
        {
            "warm" => "#E97F00",
            "concerned" => "#B91C1C",
            "focused" => "#16A34A",
            "motivational" => "#7C3AED",
            "urgent" => "#DC2626",
            "happy" => "#D97706",
            "excited" => "#DB2777",
            "sad" => "#4F46E5",
            "calm" => "#0D9488",
            "energetic" => "#EA580C",
            "professional" => "#1D4ED8",
            _ => "#2563EB"
        };
    }

    private static string ResolveReaderBackgroundClass(string emotionKey) =>
        emotionKey switch
        {
            "focused" => "professional",
            _ => emotionKey
        };

    private static string ResolveEmotionKey(string? emotion, string fallbackEmotionKey = NeutralEmotionKey)
    {
        var normalized = string.IsNullOrWhiteSpace(emotion)
            ? fallbackEmotionKey
            : emotion.Trim().ToLowerInvariant();

        return normalized switch
        {
            "worried" or "serious" or "frustrated" or "empathetic" => "concerned",
            "determined" or "innovative" or "analytical" or "precise" or "stable" or "methodical" => "focused",
            "supportive" or "helpful" or "encouraging" or "inspiring" => "motivational",
            "thoughtful" or "confident" or "satisfied" => "professional",
            "artistic" or "creative" or "amazed" or "impressed" or "passionate" => "excited",
            "grateful" => "warm",
            _ => normalized switch
            {
                "warm" or "concerned" or "focused" or "motivational" or NeutralEmotionKey or "urgent" or
                "happy" or "excited" or "sad" or "calm" or "energetic" or "professional" => normalized,
                _ => fallbackEmotionKey
            }
        };
    }

    private static string ResolveSemanticWordClass(string? value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return $"{prefix}-{value.Trim().ToLowerInvariant()}";
    }

    private static string ResolveEmotionWordClass(string? emotion, string prefix)
    {
        var emotionKey = ResolveEmotionKey(emotion, string.Empty);
        return string.IsNullOrWhiteSpace(emotionKey) ? string.Empty : $"{prefix}-{emotionKey}";
    }

    private static string FormatEmotionLabel(string emotionKey) =>
        emotionKey switch
        {
            "concerned" => "Concerned",
            "motivational" => "Motivational",
            "professional" => "Professional",
            "energetic" => "Energetic",
            _ when string.IsNullOrWhiteSpace(emotionKey) => "Neutral",
            _ => char.ToUpperInvariant(emotionKey[0]) + emotionKey[1..]
        };

    private static string ToAlphaColor(string color, double opacity)
    {
        var hex = color.Trim().TrimStart('#');
        if (hex.Length == 8)
        {
            hex = hex[2..];
        }

        if (hex.Length != 6 ||
            !int.TryParse(hex.AsSpan(0, 2), NumberStyles.HexNumber, null, out var red) ||
            !int.TryParse(hex.AsSpan(2, 2), NumberStyles.HexNumber, null, out var green) ||
            !int.TryParse(hex.AsSpan(4, 2), NumberStyles.HexNumber, null, out var blue))
        {
            return color;
        }

        return $"rgba({red}, {green}, {blue}, {opacity.ToString("0.##", CultureInfo.InvariantCulture)})";
    }

    private static string BuildPrimaryCameraStyle(MediaSourceTransform transform)
    {
        var transformValue = BuildCameraTransform(transform, includeTranslate: false);
        return string.IsNullOrWhiteSpace(transformValue)
            ? string.Empty
            : $"transform:{transformValue};";
    }

    private static bool HasSentenceEndingPunctuation(string text) =>
        text.IndexOfAny(['.', '!', '?']) >= 0;

    private static bool HasClausePunctuation(string text) =>
        text.IndexOfAny([',', ';', ':', '—', '–']) >= 0;

    private static string BuildCameraTransform(MediaSourceTransform transform, bool includeTranslate)
    {
        var transforms = new List<string>();

        if (includeTranslate)
        {
            transforms.Add("translate(-50%, -50%)");
        }

        if (Math.Abs(transform.Rotation) > 0.01)
        {
            transforms.Add($"rotate({transform.Rotation.ToString("0.##", CultureInfo.InvariantCulture)}deg)");
        }

        if (transform.MirrorHorizontal)
        {
            transforms.Add("scaleX(-1)");
        }

        if (transform.MirrorVertical)
        {
            transforms.Add("scaleY(-1)");
        }

        return string.Join(' ', transforms);
    }
}
