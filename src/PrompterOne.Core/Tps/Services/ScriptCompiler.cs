using System.Globalization;
using System.Text;
using PrompterOne.Core.Models.CompiledScript;
using PrompterOne.Core.Models.HeadCues;
using PrompterOne.Core.Models.Tps;
using PrompterOne.Core.Services.Rsvp;

namespace PrompterOne.Core.Services;

public class ScriptCompiler
{
    public Task<CompiledScript> CompileAsync(TpsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Task.FromResult(TpsCompilerCore.Compile(document));
    }
}

internal static class TpsCompilerCore
{
    private const string MarkdownStrongScope = "__markdown-strong__";
    private const float RelativeSpeedTolerance = 0.0001f;
    private const int MinimumWordDurationMilliseconds = 120;
    private const double MillisecondsPerMinute = 60_000d;
    private const double WordDurationBaseFactor = 0.8d;
    private const double WordDurationLengthFactor = 0.04d;

    private static readonly RsvpOrpCalculator OrpCalculator = new();

    public static CompiledScript Compile(TpsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var baseWpm = ResolveBaseWpm(document.Metadata);
        var speedOffsets = ResolveSpeedOffsets(document.Metadata);
        var compiled = new CompiledScript
        {
            Metadata = new Dictionary<string, string>(document.Metadata, StringComparer.OrdinalIgnoreCase)
        };

        foreach (var segment in document.Segments)
        {
            var emotion = ResolveEmotion(segment.Emotion, TpsSpec.DefaultEmotion);
            var palette = ResolvePalette(emotion);
            var inherited = new InheritedFormattingState(
                TargetWpm: Math.Max(1, segment.TargetWPM ?? baseWpm),
                Emotion: emotion,
                Speaker: NormalizeValue(segment.Speaker),
                SpeedOffsets: speedOffsets);

            var compiledSegment = new CompiledSegment
            {
                Id = segment.Id,
                Name = NormalizeValue(segment.Name) ?? TpsSpec.DefaultImplicitSegmentName,
                TargetWPM = inherited.TargetWpm,
                Emotion = emotion,
                Speaker = inherited.Speaker,
                Timing = NormalizeValue(segment.Timing),
                AccentColor = NormalizeValue(segment.AccentColor) ?? palette.Accent,
                BackgroundColor = NormalizeValue(segment.BackgroundColor) ?? palette.Background,
                TextColor = NormalizeValue(segment.TextColor) ?? palette.Text,
                Duration = segment.Duration
            };

            if (!string.IsNullOrWhiteSpace(segment.LeadingContent))
            {
                var leading = CompileContent(segment.LeadingContent!, inherited);
                compiledSegment.Words.AddRange(leading.Words);
            }
            else if (segment.Blocks.Count == 0 && !string.IsNullOrWhiteSpace(segment.Content))
            {
                var direct = CompileContent(segment.Content, inherited);
                compiledSegment.Words.AddRange(direct.Words);
            }

            foreach (var block in segment.Blocks)
            {
                var blockEmotion = ResolveEmotion(block.Emotion, emotion);
                var blockInherited = inherited with
                {
                    TargetWpm = Math.Max(1, block.TargetWPM ?? inherited.TargetWpm),
                    Emotion = blockEmotion,
                    Speaker = NormalizeValue(block.Speaker) ?? inherited.Speaker
                };

                var content = CompileContent(block.Content, blockInherited);
                compiledSegment.Blocks.Add(new CompiledBlock
                {
                    Id = block.Id,
                    Name = NormalizeValue(block.Name) ?? TpsSpec.DefaultImplicitSegmentName,
                    TargetWPM = blockInherited.TargetWpm,
                    Emotion = blockEmotion,
                    Speaker = blockInherited.Speaker,
                    Phrases = content.Phrases,
                    Words = content.Words
                });
            }

            compiled.Segments.Add(compiledSegment);
        }

        return compiled;
    }

    private static ContentCompilationResult CompileContent(string rawText, InheritedFormattingState inherited)
    {
        var protectedText = TpsEscaping.Protect(NormalizeLineEndings(rawText));
        var words = new List<CompiledWord>();
        var phrases = new List<CompiledPhrase>();
        var currentPhrase = new List<CompiledWord>();
        var scopes = new List<InlineScope>();
        var tokenBuilder = new StringBuilder();
        TokenAccumulator? token = null;

        for (var index = 0; index < protectedText.Length; index++)
        {
            var current = protectedText[index];

            if (TryHandleMarkdownMarker(protectedText, ref index, scopes))
            {
                FinalizeToken(words, phrases, currentPhrase, tokenBuilder, ref token, inherited);
                continue;
            }

            if (current == '[' && TryReadTag(protectedText, index, out var tagToken, out var nextIndex))
            {
                if (RequiresTokenBoundary(tagToken))
                {
                    FinalizeToken(words, phrases, currentPhrase, tokenBuilder, ref token, inherited);
                }

                if (TryHandleTag(tagToken, scopes, words, phrases, currentPhrase, inherited))
                {
                    index = nextIndex;
                    continue;
                }

                AppendLiteral(tagToken, scopes, inherited, tokenBuilder, ref token);
                index = nextIndex;
                continue;
            }

            if (current == '/' && TryHandleSlashPause(protectedText, ref index, words, phrases, currentPhrase, tokenBuilder, ref token, inherited))
            {
                continue;
            }

            if (char.IsWhiteSpace(current))
            {
                FinalizeToken(words, phrases, currentPhrase, tokenBuilder, ref token, inherited);
                continue;
            }

            AppendCharacter(current, scopes, inherited, tokenBuilder, ref token);
        }

        FinalizeToken(words, phrases, currentPhrase, tokenBuilder, ref token, inherited);
        FlushPhrase(phrases, currentPhrase);

        return new ContentCompilationResult(words, phrases);
    }

    private static bool TryHandleMarkdownMarker(string content, ref int index, List<InlineScope> scopes)
    {
        if (content[index] != '*')
        {
            return false;
        }

        var isStrong = index + 1 < content.Length && content[index + 1] == '*';
        var markerLength = isStrong ? 2 : 1;
        var scopeName = isStrong ? MarkdownStrongScope : TpsSpec.Tags.Emphasis;
        var existingIndex = scopes.FindLastIndex(scope => string.Equals(scope.Name, scopeName, StringComparison.Ordinal));
        var closer = new string('*', markerLength);

        if (existingIndex >= 0)
        {
            scopes.RemoveAt(existingIndex);
            index += markerLength - 1;
            return true;
        }

        var closerIndex = content.IndexOf(closer, index + markerLength, StringComparison.Ordinal);
        if (closerIndex < 0)
        {
            return false;
        }

        scopes.Add(new InlineScope(scopeName, EmphasisLevel: isStrong ? 2 : 1));
        index += markerLength - 1;
        return true;
    }

    private static bool RequiresTokenBoundary(string tagToken)
    {
        if (tagToken.Length < 2)
        {
            return false;
        }

        var innerToken = TpsEscaping.Restore(tagToken[1..^1]).Trim();
        if (string.IsNullOrWhiteSpace(innerToken) || innerToken[0] == '/')
        {
            return false;
        }

        var separatorIndex = innerToken.IndexOf(':');
        var tagName = separatorIndex >= 0 ? innerToken[..separatorIndex].Trim() : innerToken.Trim();
        var normalizedName = tagName.ToLowerInvariant();

        return string.Equals(normalizedName, TpsSpec.Tags.Pause, StringComparison.Ordinal) ||
               string.Equals(normalizedName, TpsSpec.Tags.Breath, StringComparison.Ordinal) ||
               string.Equals(normalizedName, TpsSpec.Tags.EditPoint, StringComparison.Ordinal);
    }

    private static bool TryReadTag(string content, int startIndex, out string tagToken, out int nextIndex)
    {
        tagToken = string.Empty;
        nextIndex = startIndex;
        var endIndex = content.IndexOf(']', startIndex + 1);
        if (endIndex < 0)
        {
            return false;
        }

        tagToken = content[startIndex..(endIndex + 1)];
        nextIndex = endIndex;
        return true;
    }

    private static bool TryHandleTag(
        string tagToken,
        List<InlineScope> scopes,
        List<CompiledWord> words,
        List<CompiledPhrase> phrases,
        List<CompiledWord> currentPhrase,
        InheritedFormattingState inherited)
    {
        if (tagToken.Length < 2)
        {
            return false;
        }

        var innerToken = TpsEscaping.Restore(tagToken[1..^1]).Trim();
        if (string.IsNullOrWhiteSpace(innerToken))
        {
            return false;
        }

        if (innerToken[0] == '/')
        {
            var closeName = innerToken[1..].Trim();
            var scopeIndex = scopes.FindLastIndex(scope => string.Equals(scope.Name, closeName, StringComparison.OrdinalIgnoreCase));
            if (scopeIndex < 0)
            {
                return false;
            }

            scopes.RemoveAt(scopeIndex);
            return true;
        }

        var separatorIndex = innerToken.IndexOf(':');
        var tagName = separatorIndex >= 0 ? innerToken[..separatorIndex].Trim() : innerToken.Trim();
        var tagArgument = separatorIndex >= 0 ? innerToken[(separatorIndex + 1)..].Trim() : null;
        var normalizedName = tagName.ToLowerInvariant();

        if (string.Equals(normalizedName, TpsSpec.Tags.Pause, StringComparison.Ordinal))
        {
            if (!TryResolvePauseMilliseconds(tagArgument, out var pauseDuration))
            {
                return false;
            }

            FlushPhrase(phrases, currentPhrase);
            words.Add(CreateControlWord(
                isPause: true,
                pauseDuration: pauseDuration,
                inherited: inherited));
            return true;
        }

        if (string.Equals(normalizedName, TpsSpec.Tags.Breath, StringComparison.Ordinal))
        {
            words.Add(CreateControlWord(isBreath: true, inherited: inherited));
            return true;
        }

        if (string.Equals(normalizedName, TpsSpec.Tags.EditPoint, StringComparison.Ordinal))
        {
            words.Add(CreateControlWord(
                isEditPoint: true,
                editPointPriority: NormalizeValue(tagArgument),
                inherited: inherited));
            return true;
        }

        if (string.Equals(normalizedName, TpsSpec.Tags.Phonetic, StringComparison.Ordinal) ||
            string.Equals(normalizedName, TpsSpec.Tags.Pronunciation, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(tagArgument))
            {
                return false;
            }

            scopes.Add(new InlineScope(normalizedName, PronunciationGuide: tagArgument));
            return true;
        }

        if (string.Equals(normalizedName, TpsSpec.Tags.Stress, StringComparison.Ordinal))
        {
            scopes.Add(new InlineScope(normalizedName, StressGuide: NormalizeValue(tagArgument), StressWrap: string.IsNullOrWhiteSpace(tagArgument)));
            return true;
        }

        if (string.Equals(normalizedName, TpsSpec.Tags.Emphasis, StringComparison.Ordinal))
        {
            scopes.Add(new InlineScope(normalizedName, EmphasisLevel: 1));
            return true;
        }

        if (string.Equals(normalizedName, TpsSpec.Tags.Highlight, StringComparison.Ordinal))
        {
            scopes.Add(new InlineScope(normalizedName, Highlight: true));
            return true;
        }

        if (TpsSpec.VolumeLevels.Contains(normalizedName))
        {
            scopes.Add(new InlineScope(normalizedName, VolumeLevel: normalizedName));
            return true;
        }

        if (TpsSpec.DeliveryModes.Contains(normalizedName))
        {
            scopes.Add(new InlineScope(normalizedName, DeliveryMode: normalizedName));
            return true;
        }

        if (TpsSpec.Emotions.Contains(normalizedName))
        {
            scopes.Add(new InlineScope(normalizedName, InlineEmotion: normalizedName));
            return true;
        }

        if (TryParseAbsoluteWpm(normalizedName, out var absoluteWpm))
        {
            scopes.Add(new InlineScope(normalizedName, AbsoluteSpeed: absoluteWpm));
            return true;
        }

        if (TryResolveRelativeSpeed(normalizedName, inherited.SpeedOffsets, out var multiplier))
        {
            scopes.Add(new InlineScope(normalizedName, RelativeSpeedMultiplier: multiplier));
            return true;
        }

        if (string.Equals(normalizedName, TpsSpec.Tags.Normal, StringComparison.Ordinal))
        {
            scopes.Add(new InlineScope(normalizedName, ResetSpeed: true));
            return true;
        }

        return false;
    }

    private static bool TryHandleSlashPause(
        string content,
        ref int index,
        List<CompiledWord> words,
        List<CompiledPhrase> phrases,
        List<CompiledWord> currentPhrase,
        StringBuilder tokenBuilder,
        ref TokenAccumulator? token,
        InheritedFormattingState inherited)
    {
        var hasPreviousContent = tokenBuilder.Length > 0;
        var hasNextSlash = index + 1 < content.Length && content[index + 1] == '/';
        var previousIsBoundary = index == 0 || char.IsWhiteSpace(content[index - 1]);
        var nextIndex = hasNextSlash ? index + 2 : index + 1;
        var nextIsBoundary = nextIndex >= content.Length || char.IsWhiteSpace(content[nextIndex]);
        if (hasPreviousContent || !previousIsBoundary || !nextIsBoundary)
        {
            return false;
        }

        FinalizeToken(words, phrases, currentPhrase, tokenBuilder, ref token, inherited);
        FlushPhrase(phrases, currentPhrase);
        words.Add(CreateControlWord(
            isPause: true,
            pauseDuration: hasNextSlash ? 600 : 300,
            inherited: inherited));
        if (hasNextSlash)
        {
            index++;
        }

        return true;
    }

    private static void AppendLiteral(
        string literal,
        List<InlineScope> scopes,
        InheritedFormattingState inherited,
        StringBuilder tokenBuilder,
        ref TokenAccumulator? token)
    {
        foreach (var character in literal)
        {
            AppendCharacter(character, scopes, inherited, tokenBuilder, ref token);
        }
    }

    private static void AppendCharacter(
        char character,
        List<InlineScope> scopes,
        InheritedFormattingState inherited,
        StringBuilder tokenBuilder,
        ref TokenAccumulator? token)
    {
        tokenBuilder.Append(character);
        token ??= new TokenAccumulator();
        token.Apply(ResolveActiveState(scopes, inherited), character);
    }

    private static void FinalizeToken(
        List<CompiledWord> words,
        List<CompiledPhrase> phrases,
        List<CompiledWord> currentPhrase,
        StringBuilder tokenBuilder,
        ref TokenAccumulator? token,
        InheritedFormattingState inherited)
    {
        if (tokenBuilder.Length == 0 || token is null)
        {
            tokenBuilder.Clear();
            token = null;
            return;
        }

        var text = TpsEscaping.Restore(tokenBuilder.ToString());
        tokenBuilder.Clear();

        if (string.IsNullOrWhiteSpace(text))
        {
            token = null;
            return;
        }

        if (TpsTokenTextRules.IsStandalonePunctuationToken(text))
        {
            if (TryAttachStandalonePunctuation(words, currentPhrase, text))
            {
                if (HasSentenceEndingPunctuation(text))
                {
                    FlushPhrase(phrases, currentPhrase);
                }
            }

            token = null;
            return;
        }

        var metadata = token.BuildWordMetadata(inherited.TargetWpm);
        var effectiveWpm = ResolveEffectiveWpm(metadata, inherited.TargetWpm);
        var cleanText = text.Trim();
        var compiledWord = new CompiledWord
        {
            CleanText = cleanText,
            CharacterCount = cleanText.Length,
            ORPPosition = OrpCalculator.CalculateOrpIndex(cleanText),
            DisplayDuration = TimeSpan.FromMilliseconds(CalculateWordDurationMilliseconds(cleanText, effectiveWpm)),
            Metadata = metadata
        };

        words.Add(compiledWord);
        currentPhrase.Add(compiledWord);

        if (HasSentenceEndingPunctuation(cleanText))
        {
            FlushPhrase(phrases, currentPhrase);
        }

        token = null;
    }

    private static bool TryAttachStandalonePunctuation(
        List<CompiledWord> words,
        List<CompiledWord> currentPhrase,
        string punctuationToken)
    {
        var target = currentPhrase.LastOrDefault(word => word.Metadata.IsPause is false && !string.IsNullOrWhiteSpace(word.CleanText))
            ?? words.LastOrDefault(word => word.Metadata.IsPause is false && !string.IsNullOrWhiteSpace(word.CleanText));

        if (target is null)
        {
            return false;
        }

        var suffix = TpsTokenTextRules.BuildStandalonePunctuationSuffix(punctuationToken);
        target.CleanText = string.Concat(target.CleanText, suffix);
        target.CharacterCount = target.CleanText.Length;
        target.ORPPosition = OrpCalculator.CalculateOrpIndex(target.CleanText);
        return true;
    }

    private static void FlushPhrase(List<CompiledPhrase> phrases, List<CompiledWord> currentPhrase)
    {
        if (currentPhrase.Count == 0)
        {
            return;
        }

        phrases.Add(new CompiledPhrase
        {
            Id = Guid.NewGuid().ToString(),
            Words = currentPhrase.ToList()
        });

        currentPhrase.Clear();
    }

    private static CompiledWord CreateControlWord(
        bool isPause = false,
        int? pauseDuration = null,
        bool isBreath = false,
        bool isEditPoint = false,
        string? editPointPriority = null,
        InheritedFormattingState? inherited = null)
    {
        inherited ??= new InheritedFormattingState(TpsSpec.DefaultBaseWpm, TpsSpec.DefaultEmotion, null, TpsSpec.DefaultSpeedOffsets);
        var metadata = new WordMetadata
        {
            IsPause = isPause,
            PauseDuration = pauseDuration,
            IsBreath = isBreath,
            IsEditPoint = isEditPoint,
            EditPointPriority = NormalizeValue(editPointPriority),
            EmotionHint = inherited.Emotion,
            Speaker = inherited.Speaker,
            HeadCue = HeadCueCatalog.ResolveForEmotion(inherited.Emotion)
        };

        return new CompiledWord
        {
            CleanText = string.Empty,
            CharacterCount = 0,
            ORPPosition = 0,
            DisplayDuration = TimeSpan.FromMilliseconds(Math.Max(0, pauseDuration ?? 0)),
            Metadata = metadata
        };
    }

    private static ActiveInlineState ResolveActiveState(List<InlineScope> scopes, InheritedFormattingState inherited)
    {
        var absoluteSpeed = inherited.TargetWpm;
        var hasAbsoluteSpeed = false;
        var hasRelativeSpeed = false;
        var relativeSpeedMultiplier = 1f;
        var emphasisLevel = 0;
        var highlight = false;
        var emotion = inherited.Emotion;
        string? inlineEmotion = null;
        string? volumeLevel = null;
        string? deliveryMode = null;
        string? pronunciationGuide = null;
        string? stressGuide = null;
        var stressWrap = false;

        foreach (var scope in scopes)
        {
            if (scope.AbsoluteSpeed is int scopedAbsoluteSpeed)
            {
                absoluteSpeed = scopedAbsoluteSpeed;
                hasAbsoluteSpeed = true;
                hasRelativeSpeed = false;
                relativeSpeedMultiplier = 1f;
            }

            if (scope.ResetSpeed)
            {
                hasRelativeSpeed = false;
                relativeSpeedMultiplier = 1f;
            }

            if (scope.RelativeSpeedMultiplier is float scopedRelativeSpeed)
            {
                hasRelativeSpeed = true;
                relativeSpeedMultiplier *= scopedRelativeSpeed;
            }

            emphasisLevel = Math.Max(emphasisLevel, scope.EmphasisLevel);
            highlight |= scope.Highlight;

            if (!string.IsNullOrWhiteSpace(scope.InlineEmotion))
            {
                emotion = scope.InlineEmotion!;
                inlineEmotion = scope.InlineEmotion;
            }

            if (!string.IsNullOrWhiteSpace(scope.VolumeLevel))
            {
                volumeLevel = scope.VolumeLevel;
            }

            if (!string.IsNullOrWhiteSpace(scope.DeliveryMode))
            {
                deliveryMode = scope.DeliveryMode;
            }

            if (!string.IsNullOrWhiteSpace(scope.PronunciationGuide))
            {
                pronunciationGuide = scope.PronunciationGuide;
            }

            if (!string.IsNullOrWhiteSpace(scope.StressGuide))
            {
                stressGuide = scope.StressGuide;
            }

            stressWrap |= scope.StressWrap;
        }

        return new ActiveInlineState(
            Emotion: emotion,
            InlineEmotion: inlineEmotion,
            Speaker: inherited.Speaker,
            EmphasisLevel: emphasisLevel,
            Highlight: highlight,
            VolumeLevel: volumeLevel,
            DeliveryMode: deliveryMode,
            PronunciationGuide: pronunciationGuide,
            StressGuide: stressGuide,
            StressWrap: stressWrap,
            HasAbsoluteSpeed: hasAbsoluteSpeed,
            AbsoluteSpeed: absoluteSpeed,
            HasRelativeSpeed: hasRelativeSpeed,
            RelativeSpeedMultiplier: relativeSpeedMultiplier);
    }

    private static IReadOnlyDictionary<string, int> ResolveSpeedOffsets(IReadOnlyDictionary<string, string> metadata)
    {
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [TpsSpec.Tags.Xslow] = ResolveSpeedOffset(metadata, TpsSpec.FrontMatterKeys.SpeedOffsetsXslow, TpsSpec.DefaultXslowOffset),
            [TpsSpec.Tags.Slow] = ResolveSpeedOffset(metadata, TpsSpec.FrontMatterKeys.SpeedOffsetsSlow, TpsSpec.DefaultSlowOffset),
            [TpsSpec.Tags.Fast] = ResolveSpeedOffset(metadata, TpsSpec.FrontMatterKeys.SpeedOffsetsFast, TpsSpec.DefaultFastOffset),
            [TpsSpec.Tags.Xfast] = ResolveSpeedOffset(metadata, TpsSpec.FrontMatterKeys.SpeedOffsetsXfast, TpsSpec.DefaultXfastOffset)
        };
    }

    private static int ResolveSpeedOffset(IReadOnlyDictionary<string, string> metadata, string key, int fallback)
    {
        return metadata.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static int ResolveBaseWpm(IReadOnlyDictionary<string, string> metadata)
    {
        return metadata.TryGetValue(TpsSpec.FrontMatterKeys.BaseWpm, out var value) &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Max(1, parsed)
            : TpsSpec.DefaultBaseWpm;
    }

    private static string ResolveEmotion(string? candidate, string fallback)
    {
        var normalized = NormalizeValue(candidate)?.ToLowerInvariant();
        return normalized is not null && TpsSpec.Emotions.Contains(normalized)
            ? normalized
            : fallback;
    }

    private static EmotionPalette ResolvePalette(string? emotion)
    {
        var key = ResolveEmotion(emotion, TpsSpec.DefaultEmotion);
        return TpsSpec.EmotionPalettes.TryGetValue(key, out var palette)
            ? palette
            : TpsSpec.EmotionPalettes[TpsSpec.DefaultEmotion];
    }

    private static bool TryParseAbsoluteWpm(string normalizedToken, out int wpm)
    {
        wpm = 0;
        if (!normalizedToken.EndsWith(TpsSpec.WpmSuffix.ToLowerInvariant(), StringComparison.Ordinal))
        {
            return false;
        }

        var numberPart = normalizedToken[..^TpsSpec.WpmSuffix.Length];
        return int.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out wpm);
    }

    private static bool TryResolveRelativeSpeed(
        string normalizedToken,
        IReadOnlyDictionary<string, int> speedOffsets,
        out float multiplier)
    {
        multiplier = 1f;
        if (!speedOffsets.TryGetValue(normalizedToken, out var offset))
        {
            return false;
        }

        multiplier = 1f + (offset / 100f);
        return true;
    }

    private static bool TryResolvePauseMilliseconds(string? argument, out int pauseMilliseconds)
    {
        pauseMilliseconds = 0;
        var trimmed = NormalizeValue(argument);
        if (trimmed is null)
        {
            return false;
        }

        if (trimmed.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(trimmed[..^2], NumberStyles.Integer, CultureInfo.InvariantCulture, out pauseMilliseconds);
        }

        if (!trimmed.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!double.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return false;
        }

        pauseMilliseconds = (int)Math.Round(seconds * 1000d, MidpointRounding.AwayFromZero);
        return true;
    }

    private static int ResolveEffectiveWpm(WordMetadata metadata, int fallbackWpm)
    {
        if (metadata.SpeedOverride is int overrideWpm)
        {
            return Math.Max(1, overrideWpm);
        }

        if (metadata.SpeedMultiplier is float speedMultiplier && Math.Abs(speedMultiplier) > RelativeSpeedTolerance)
        {
            return Math.Max(1, (int)Math.Round(fallbackWpm * speedMultiplier, MidpointRounding.AwayFromZero));
        }

        return Math.Max(1, fallbackWpm);
    }

    private static int CalculateWordDurationMilliseconds(string word, int effectiveWpm)
    {
        var baseMilliseconds = MillisecondsPerMinute / Math.Max(1, effectiveWpm);
        return Math.Max(
            MinimumWordDurationMilliseconds,
            (int)Math.Round(
                baseMilliseconds * (WordDurationBaseFactor + (word.Length * WordDurationLengthFactor)),
                MidpointRounding.AwayFromZero));
    }

    private static bool HasSentenceEndingPunctuation(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lastCharacter = text.TrimEnd()[^1];
        return lastCharacter is '.' or '!' or '?';
    }

    private static string NormalizeLineEndings(string? value) =>
        value?.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n') ?? string.Empty;

    private static string? NormalizeValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ContentCompilationResult(List<CompiledWord> Words, List<CompiledPhrase> Phrases);

    private sealed record InheritedFormattingState(
        int TargetWpm,
        string Emotion,
        string? Speaker,
        IReadOnlyDictionary<string, int> SpeedOffsets);

    private sealed record ActiveInlineState(
        string Emotion,
        string? InlineEmotion,
        string? Speaker,
        int EmphasisLevel,
        bool Highlight,
        string? VolumeLevel,
        string? DeliveryMode,
        string? PronunciationGuide,
        string? StressGuide,
        bool StressWrap,
        bool HasAbsoluteSpeed,
        int AbsoluteSpeed,
        bool HasRelativeSpeed,
        float RelativeSpeedMultiplier);

    private sealed record InlineScope(
        string Name,
        int EmphasisLevel = 0,
        bool Highlight = false,
        string? InlineEmotion = null,
        string? VolumeLevel = null,
        string? DeliveryMode = null,
        string? PronunciationGuide = null,
        string? StressGuide = null,
        bool StressWrap = false,
        int? AbsoluteSpeed = null,
        float? RelativeSpeedMultiplier = null,
        bool ResetSpeed = false);

    private sealed class TokenAccumulator
    {
        private readonly StringBuilder _stressTextBuilder = new();

        public int AbsoluteSpeed { get; private set; }

        public string? DeliveryMode { get; private set; }

        public int EmphasisLevel { get; private set; }

        public string? EmotionHint { get; private set; }

        public bool HasAbsoluteSpeed { get; private set; }

        public bool HasRelativeSpeed { get; private set; }

        public bool IsHighlight { get; private set; }

        public string? InlineEmotionHint { get; private set; }

        public string? PronunciationGuide { get; private set; }

        public float RelativeSpeedMultiplier { get; private set; } = 1f;

        public string? Speaker { get; private set; }

        public string? StressGuide { get; private set; }

        public string? VolumeLevel { get; private set; }

        public void Apply(ActiveInlineState state, char character)
        {
            EmphasisLevel = Math.Max(EmphasisLevel, state.EmphasisLevel);
            IsHighlight |= state.Highlight;
            EmotionHint = state.Emotion;

            if (!string.IsNullOrWhiteSpace(state.InlineEmotion))
            {
                InlineEmotionHint = state.InlineEmotion;
            }

            if (!string.IsNullOrWhiteSpace(state.VolumeLevel))
            {
                VolumeLevel = state.VolumeLevel;
            }

            if (!string.IsNullOrWhiteSpace(state.DeliveryMode))
            {
                DeliveryMode = state.DeliveryMode;
            }

            if (!string.IsNullOrWhiteSpace(state.PronunciationGuide))
            {
                PronunciationGuide = state.PronunciationGuide;
            }

            if (!string.IsNullOrWhiteSpace(state.StressGuide))
            {
                StressGuide = state.StressGuide;
            }

            if (state.StressWrap)
            {
                _stressTextBuilder.Append(character);
            }

            if (ShouldCaptureSpeedMetadata(character))
            {
                HasAbsoluteSpeed = state.HasAbsoluteSpeed;
                AbsoluteSpeed = state.AbsoluteSpeed;
                HasRelativeSpeed = state.HasRelativeSpeed;
                RelativeSpeedMultiplier = state.RelativeSpeedMultiplier;
            }

            Speaker = state.Speaker;
        }

        public WordMetadata BuildWordMetadata(int inheritedWpm)
        {
            var metadata = new WordMetadata
            {
                IsEmphasis = EmphasisLevel > 0,
                EmphasisLevel = EmphasisLevel,
                IsHighlight = IsHighlight,
                EmotionHint = EmotionHint,
                InlineEmotionHint = InlineEmotionHint,
                VolumeLevel = VolumeLevel,
                DeliveryMode = DeliveryMode,
                PronunciationGuide = PronunciationGuide,
                StressGuide = StressGuide,
                StressText = _stressTextBuilder.Length == 0 ? null : _stressTextBuilder.ToString(),
                Speaker = Speaker,
                HeadCue = HeadCueCatalog.ResolveForEmotion(EmotionHint)
            };

            if (HasAbsoluteSpeed)
            {
                var effectiveWpm = HasRelativeSpeed
                    ? Math.Max(1, (int)Math.Round(AbsoluteSpeed * RelativeSpeedMultiplier, MidpointRounding.AwayFromZero))
                    : AbsoluteSpeed;

                if (effectiveWpm != inheritedWpm)
                {
                    metadata.SpeedOverride = effectiveWpm;
                }
            }
            else if (HasRelativeSpeed && Math.Abs(RelativeSpeedMultiplier - 1f) > RelativeSpeedTolerance)
            {
                metadata.SpeedMultiplier = RelativeSpeedMultiplier;
            }

            return metadata;
        }

        private static bool ShouldCaptureSpeedMetadata(char character) =>
            !char.IsWhiteSpace(character) &&
            !TpsTokenTextRules.IsStandalonePunctuationToken(character.ToString());
    }
}
