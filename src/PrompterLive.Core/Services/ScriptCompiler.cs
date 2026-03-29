using System.Globalization;
using System.Text.RegularExpressions;
using PrompterLive.Core.Models.CompiledScript;
using PrompterLive.Core.Models.HeadCues;
using PrompterLive.Core.Models.Tps;

namespace PrompterLive.Core.Services;

public class ScriptCompiler
{
    private const int DefaultWpm = 120;
    private const int MinWpm = 60;
    private const int MaxWpm = 600;
    private const float XSlowFactor = 0.6f;
    private const float SlowFactor = 0.8f;
    private const float FastFactor = 1.25f;
    private const float XFastFactor = 1.5f;
    private const string DefaultHighlightColor = "#FFEB3B";
    private const string DefaultEmphasisColor = "#FFD700";

    private static readonly Regex HeaderRegex = new(@"###?\s*\[[^\]]+\]", RegexOptions.Compiled);
    private static readonly Regex SimpleHeaderRegex = new(@"^###?\s+.+$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex BoldMarkdownRegex = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicMarkdownRegex = new(@"\*(?!\*)([^*]+)\*(?!\*)", RegexOptions.Compiled);
    private static readonly Regex TokenSplitRegex = new(@"(\[[^\[\]]+\]|</?[^>]+>|\{[^{}]+\}|//|/)", RegexOptions.Compiled);
    public static readonly Dictionary<string, string> AvailableColors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "red", "#FF5252" },
        { "green", "#4CAF50" },
        { "blue", "#2196F3" },
        { "yellow", "#FFD700" },
        { "orange", "#FF9800" },
        { "purple", "#9C27B0" },
        { "cyan", "#00BCD4" },
        { "magenta", "#FF00FF" },
        { "pink", "#EC4899" },
        { "teal", "#14B8A6" },
        { "white", "#FFFFFF" },
        { "black", "#111827" },
        { "gray", "#6B7280" },
        { "highlight", DefaultHighlightColor }
    };

    private static readonly Dictionary<string, EmotionColorSet> EmotionStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        { "warm", new EmotionColorSet("#FFFB923C", "#FF1F2937", "#FFE97F00") },
        { "concerned", new EmotionColorSet("#FFF87171", "#FF1F2937", "#FFDC2626") },
        { "focused", new EmotionColorSet("#FF4ADE80", "#FF1F2937", "#FF16A34A") },
        { "motivational", new EmotionColorSet("#FFA855F7", "#FFFFFFFF", "#FF7C3AED") },
        { "urgent", new EmotionColorSet("#FFEF4444", "#FFFFFFFF", "#FFB91C1C") },
        { "happy", new EmotionColorSet("#FFFACC15", "#FF1F2937", "#FFD97706") },
        { "excited", new EmotionColorSet("#FFEC4899", "#FFFFFFFF", "#FFDB2777") },
        { "sad", new EmotionColorSet("#FF6366F1", "#FFFFFFFF", "#FF4F46E5") },
        { "calm", new EmotionColorSet("#FF14B8A6", "#FFFFFFFF", "#FF0D9488") },
        { "energetic", new EmotionColorSet("#FFF97316", "#FFFFFFFF", "#FFEA580C") },
        { "professional", new EmotionColorSet("#FF1E40AF", "#FFFFFFFF", "#FF1E3A8A") },
        { "neutral", new EmotionColorSet("#FF3B82F6", "#FFFFFFFF", "#FF2563EB") }
    };

    public Task<CompiledScript> CompileAsync(TpsDocument document)
    {
        var compiledScript = new CompiledScript
        {
            Metadata = document.Metadata ?? new Dictionary<string, string>(),
            Segments = new List<CompiledSegment>()
        };

        foreach (var segment in document.Segments)
        {
            compiledScript.Segments.Add(CompileSegment(segment));
        }

        return Task.FromResult(compiledScript);
    }

    private static CompiledSegment CompileSegment(TpsSegment segment)
    {
        var emotion = NormalizeEmotion(segment.Emotion);
        var targetWpm = ClampWpm(segment.TargetWPM ?? DefaultWpm);
        var colors = ResolveEmotionColors(emotion);

        var compiledSegment = new CompiledSegment
        {
            Id = segment.Id,
            Name = CleanSegmentName(segment.Name),
            Emotion = emotion,
            TargetWPM = targetWpm,
            BackgroundColor = !string.IsNullOrWhiteSpace(segment.BackgroundColor) ? segment.BackgroundColor : colors.Background,
            TextColor = !string.IsNullOrWhiteSpace(segment.TextColor) ? segment.TextColor : colors.Text,
            AccentColor = !string.IsNullOrWhiteSpace(segment.AccentColor) ? segment.AccentColor : colors.Accent,
            Duration = segment.Duration,
            Blocks = new List<CompiledBlock>(),
            Words = new List<CompiledWord>()
        };

        var baseState = CreateBaseState(targetWpm, emotion);

        var blocks = segment.Blocks ?? [];
        var hasBlocks = blocks.Count > 0;

        if (hasBlocks)
        {
            if (!string.IsNullOrWhiteSpace(segment.LeadingContent))
            {
                var leadingWords = CompileContent(segment.LeadingContent, baseState);
                if (leadingWords.Count > 0)
                {
                    compiledSegment.Blocks.Add(new CompiledBlock
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = CleanBlockName(segment.Name),
                        TargetWPM = targetWpm,
                        Emotion = emotion,
                        Words = leadingWords,
                        Phrases = new List<CompiledPhrase>()
                    });
                }
            }

            foreach (var block in blocks)
            {
                compiledSegment.Blocks.Add(CompileBlock(block, baseState));
            }

            if (compiledSegment.Blocks.Count > 0)
            {
                compiledSegment.Words = compiledSegment.Blocks
                    .Where(b => b.Words != null)
                    .SelectMany(b => b.Words)
                    .ToList();
            }
        }
        else
        {
            compiledSegment.Words = CompileContent(segment.Content, baseState);
        }

        return compiledSegment;
    }

    private static CompiledBlock CompileBlock(TpsBlock block, FormattingState parentState)
    {
        var emotion = NormalizeEmotion(block.Emotion) ?? parentState.Emotion;
        var blockWpm = ClampWpm(block.TargetWPM ?? parentState.BaseWpm);

        var blockState = parentState.Clone();
        blockState.BaseWpm = blockWpm;
        SetEmotion(blockState, emotion);

        var compiledBlock = new CompiledBlock
        {
            Id = block.Id,
            Name = CleanBlockName(block.Name),
            TargetWPM = blockWpm,
            Emotion = emotion,
            Words = new List<CompiledWord>(),
            Phrases = new List<CompiledPhrase>()
        };

        if (block.Phrases?.Any() == true)
        {
            foreach (var phrase in block.Phrases)
            {
                var compiledPhrase = CompilePhrase(phrase, blockState);
                if (compiledPhrase.Words.Count > 0)
                {
                    compiledBlock.Phrases.Add(compiledPhrase);
                }
            }

            if (compiledBlock.Phrases.Count > 0)
            {
                compiledBlock.Words = compiledBlock.Phrases.SelectMany(p => p.Words).ToList();
            }
        }
        else
        {
            compiledBlock.Words = CompileContent(block.Content, blockState);
        }

        return compiledBlock;
    }

    private static CompiledPhrase CompilePhrase(TpsPhrase phrase, FormattingState parentState)
    {
        var phraseState = parentState.Clone();

        if (!string.IsNullOrWhiteSpace(phrase.Color) && AvailableColors.TryGetValue(phrase.Color, out var colorHex))
        {
            phraseState.Color = colorHex;
        }

        if (phrase.IsEmphasis)
        {
            phraseState.IsEmphasis = true;
        }

        if (phrase.IsSlow)
        {
            phraseState.SpeedMultiplier = SlowFactor;
            phraseState.SpeedOverride = null;
        }

        if (phrase.CustomWpm.HasValue)
        {
            phraseState.SpeedOverride = ClampWpm(phrase.CustomWpm.Value);
            phraseState.SpeedMultiplier = 1f;
        }

        var compiledPhrase = new CompiledPhrase
        {
            Id = phrase.Id,
            Words = CompileContent(phrase.Content, phraseState)
        };

        return compiledPhrase;
    }

    private static List<CompiledWord> CompileContent(string? rawText, FormattingState baseState)
    {
        var results = new List<CompiledWord>();
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return results;
        }

        var normalized = NormalizeContent(rawText);

        var scopeStack = new Stack<ScopeFrame>();
        scopeStack.Push(new ScopeFrame("root", baseState.Clone()));

        foreach (var token in EnumerateTokens(normalized))
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (TryHandleSlashPause(token, results))
            {
                continue;
            }

            if (TryHandleTagToken(token, scopeStack, results))
            {
                continue;
            }

            AddWord(results, token, scopeStack.Peek().State);
        }

        return results;
    }

    private static IEnumerable<string> EnumerateTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var index = 0;
        foreach (Match match in TokenSplitRegex.Matches(text))
        {
            if (match.Index > index)
            {
                var chunk = text.Substring(index, match.Index - index);
                foreach (var word in SplitIntoWords(chunk))
                {
                    yield return word;
                }
            }

            yield return match.Value;
            index = match.Index + match.Length;
        }

        if (index < text.Length)
        {
            foreach (var word in SplitIntoWords(text[index..]))
            {
                yield return word;
            }
        }
    }

    private static IEnumerable<string> SplitIntoWords(string chunk)
    {
        if (string.IsNullOrWhiteSpace(chunk))
        {
            yield break;
        }

        foreach (var part in chunk.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return part;
        }
    }

    private static bool TryHandleTagToken(string token, Stack<ScopeFrame> scopeStack, List<CompiledWord> words)
    {
        if (!IsTagToken(token))
        {
            return false;
        }

        if (token.StartsWith("{", StringComparison.Ordinal))
        {
            HandleCurlyTagToken(token, scopeStack);
            return true;
        }

        var inner = token.Substring(1, token.Length - 2);
        if (token[0] == '<' && inner.EndsWith("/", StringComparison.Ordinal))
        {
            inner = inner[..^1];
        }

        if (string.IsNullOrWhiteSpace(inner))
        {
            return true;
        }

        if (inner[0] == '/')
        {
            var closeName = inner[1..].Split(':')[0];
            PopScope(closeName, scopeStack);
            return true;
        }

        var parts = inner.Split(':', 2);
        var name = parts[0].Trim();
        var argument = parts.Length > 1 ? parts[1].Trim() : null;
        var lowered = name.ToLowerInvariant();

        if (lowered == "pause")
        {
            var pauseDuration = ParsePause(argument);
            AddPauseWord(words, pauseDuration);
            return true;
        }

        if (lowered == "emotion")
        {
            PushScope(scopeStack, lowered, state => SetEmotion(state, argument));
            return true;
        }

        if (lowered == "headcue")
        {
            PushScope(scopeStack, lowered, state => state.HeadCueId = NormalizeHeadCueId(argument));
            return true;
        }

        if (lowered is "edit_point" or "editpoint")
        {
            return true;
        }

        if (lowered is "phonetic" or "pronunciation")
        {
            PushScope(scopeStack, lowered, state => state.Pronunciation = argument);
            return true;
        }

        if (lowered is "emphasis" or "strong" or "bold")
        {
            PushScope(scopeStack, lowered, state => state.IsEmphasis = true);
            return true;
        }

        if (lowered == "highlight")
        {
            PushScope(scopeStack, lowered, state =>
            {
                state.IsEmphasis = true;
                state.Color = DefaultHighlightColor;
            });
            return true;
        }

        if (lowered == "xslow")
        {
            PushScope(scopeStack, lowered, state => state.SpeedMultiplier *= XSlowFactor);
            return true;
        }

        if (lowered == "slow")
        {
            PushScope(scopeStack, lowered, state => state.SpeedMultiplier *= SlowFactor);
            return true;
        }

        if (lowered == "fast")
        {
            PushScope(scopeStack, lowered, state => state.SpeedMultiplier *= FastFactor);
            return true;
        }

        if (lowered == "xfast")
        {
            PushScope(scopeStack, lowered, state => state.SpeedMultiplier *= XFastFactor);
            return true;
        }

        if (lowered == "normal")
        {
            PushScope(scopeStack, lowered, state => state.SpeedMultiplier = 1f);
            return true;
        }

        if (lowered == "wpm" || lowered == "speed")
        {
            if (int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var wpm))
            {
                PushScope(scopeStack, lowered, state => state.SpeedOverride = ClampWpm(wpm));
            }
            return true;
        }

        if (AvailableColors.TryGetValue(lowered, out var colorHex))
        {
            PushScope(scopeStack, lowered, state =>
            {
                state.IsEmphasis = true;
                state.Color = colorHex;
            });
            return true;
        }

        PushScope(scopeStack, lowered, _ => { });
        return true;
    }

    private static void HandleCurlyTagToken(string token, Stack<ScopeFrame> scopeStack)
    {
        var inner = token.Substring(1, token.Length - 2).Trim();
        if (string.Equals(inner, "/", StringComparison.Ordinal))
        {
            if (scopeStack.Count > 1)
            {
                scopeStack.Pop();
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(inner))
        {
            return;
        }

        var parts = inner.Split(',', 2, StringSplitOptions.TrimEntries);
        var name = parts[0];
        var argument = parts.Length > 1 ? parts[1] : null;
        ApplyCurlyTag(name, argument, scopeStack);
    }

    private static void ApplyCurlyTag(string name, string? argument, Stack<ScopeFrame> scopeStack)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var lowered = name.ToLowerInvariant();

        if (lowered == "highlight")
        {
            PushScope(scopeStack, lowered, state =>
            {
                state.IsEmphasis = true;
                state.Color = ResolveCurlyColor(argument, DefaultHighlightColor);
            });
            return;
        }

        if (lowered is "emphasize" or "emphasis" or "strong")
        {
            PushScope(scopeStack, lowered, state =>
            {
                state.IsEmphasis = true;
                var baseColor = string.IsNullOrWhiteSpace(state.Color) ? DefaultEmphasisColor : state.Color;
                state.Color = ResolveCurlyColor(argument, baseColor);
            });
            return;
        }

        if (lowered == "emotion")
        {
            PushScope(scopeStack, lowered, state => SetEmotion(state, argument));
            return;
        }

        if (lowered == "headcue")
        {
            PushScope(scopeStack, lowered, state => state.HeadCueId = NormalizeHeadCueId(argument));
            return;
        }

        if (AvailableColors.TryGetValue(lowered, out var directColor))
        {
            PushScope(scopeStack, lowered, state =>
            {
                state.IsEmphasis = true;
                state.Color = directColor;
            });
            return;
        }

        PushScope(scopeStack, lowered, state => state.IsEmphasis = true);
    }

    private static string ResolveCurlyColor(string? argument, string fallback)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            return string.IsNullOrWhiteSpace(fallback) ? DefaultEmphasisColor : fallback;
        }

        if (AvailableColors.TryGetValue(argument, out var mapped))
        {
            return mapped;
        }

        if (argument.StartsWith('#'))
        {
            return argument;
        }

        return string.IsNullOrWhiteSpace(fallback) ? DefaultEmphasisColor : fallback;
    }

    private static bool IsTagToken(string token) =>
        (token.StartsWith("[", StringComparison.Ordinal) && token.EndsWith("]", StringComparison.Ordinal)) ||
        (token.StartsWith("<", StringComparison.Ordinal) && token.EndsWith(">", StringComparison.Ordinal)) ||
        (token.StartsWith("{", StringComparison.Ordinal) && token.EndsWith("}", StringComparison.Ordinal));

    private static void PushScope(Stack<ScopeFrame> scopeStack, string tag, Action<FormattingState> mutator)
    {
        var cloned = scopeStack.Peek().State.Clone();
        mutator(cloned);
        scopeStack.Push(new ScopeFrame(tag, cloned));
    }

    private static void PopScope(string tag, Stack<ScopeFrame> scopeStack)
    {
        if (scopeStack.Count <= 1)
        {
            return;
        }

        var lowered = tag.ToLowerInvariant();
        if (string.Equals(scopeStack.Peek().Tag, lowered, StringComparison.OrdinalIgnoreCase))
        {
            scopeStack.Pop();
            return;
        }

        var buffer = new Stack<ScopeFrame>();
        while (scopeStack.Count > 1)
        {
            var frame = scopeStack.Pop();
            if (string.Equals(frame.Tag, lowered, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            buffer.Push(frame);
        }

        while (buffer.Count > 0)
        {
            scopeStack.Push(buffer.Pop());
        }
    }

    private static bool TryHandleSlashPause(string token, List<CompiledWord> words)
    {
        if (token == "//")
        {
            AddPauseWord(words, 500);
            return true;
        }

        if (token == "/")
        {
            AddPauseWord(words, 250);
            return true;
        }

        return false;
    }

    private static void AddPauseWord(List<CompiledWord> words, int durationMs)
    {
        if (durationMs <= 0)
        {
            durationMs = 500;
        }

        words.Add(new CompiledWord
        {
            CleanText = string.Empty,
            CharacterCount = 0,
            ORPPosition = 0,
            DisplayDuration = TimeSpan.FromMilliseconds(durationMs),
            Metadata = new WordMetadata
            {
                IsPause = true,
                PauseDuration = durationMs,
                HeadCue = HeadCueCatalog.Neutral.Id
            }
        });
    }

    private static int ParsePause(string? argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            return 1000;
        }

        var value = argument.Trim();
        if (value.EndsWith("ms", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(value[..^2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms))
        {
            return ms;
        }

        if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return (int)Math.Round(seconds * 1000);
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var plainMs))
        {
            return plainMs;
        }

        return 1000;
    }

    private static void AddWord(List<CompiledWord> words, string token, FormattingState state)
    {
        var clean = token.Trim();
        if (string.IsNullOrEmpty(clean))
        {
            return;
        }

        var metadata = new WordMetadata
        {
            IsEmphasis = state.IsEmphasis,
            Color = state.Color,
            EmotionHint = state.Emotion,
            PronunciationGuide = state.Pronunciation
        };

        if (metadata.IsEmphasis && string.IsNullOrWhiteSpace(metadata.Color))
        {
            metadata.Color = DefaultEmphasisColor;
        }

        if (state.SpeedOverride.HasValue)
        {
            metadata.SpeedOverride = state.SpeedOverride;
        }
        else if (Math.Abs(state.SpeedMultiplier - 1f) > 0.001f)
        {
            metadata.SpeedMultiplier = state.SpeedMultiplier;
        }

        metadata.HeadCue = !string.IsNullOrWhiteSpace(state.HeadCueId)
            ? state.HeadCueId
            : HeadCueCatalog.ResolveForEmotion(metadata.EmotionHint);

        var effectiveWpm = state.SpeedOverride ?? ClampWpm((int)Math.Round(state.BaseWpm * state.SpeedMultiplier));

        words.Add(new CompiledWord
        {
            CleanText = clean,
            CharacterCount = clean.Length,
            ORPPosition = CalculateORP(clean),
            DisplayDuration = CalculateDisplayDuration(clean, effectiveWpm),
            Metadata = metadata
        });
    }

    private static string NormalizeContent(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = HeaderRegex.Replace(text, string.Empty);
        normalized = SimpleHeaderRegex.Replace(normalized, string.Empty);
        normalized = normalized.Replace("<emphasis>", "[emphasis]", StringComparison.OrdinalIgnoreCase)
                               .Replace("</emphasis>", "[/emphasis]", StringComparison.OrdinalIgnoreCase)
                               .Replace("<strong>", "[emphasis]", StringComparison.OrdinalIgnoreCase)
                               .Replace("</strong>", "[/emphasis]", StringComparison.OrdinalIgnoreCase);
        normalized = BoldMarkdownRegex.Replace(normalized, "[emphasis]$1[/emphasis]");
        normalized = ItalicMarkdownRegex.Replace(normalized, "[emphasis]$1[/emphasis]");
        return normalized;
    }

    private static FormattingState CreateBaseState(int baseWpm, string? emotion)
    {
        var state = new FormattingState
        {
            BaseWpm = ClampWpm(baseWpm),
            SpeedMultiplier = 1f
        };

        SetEmotion(state, emotion);
        return state;
    }

    private static void SetEmotion(FormattingState state, string? emotion)
    {
        var normalized = NormalizeEmotion(emotion);
        state.Emotion = normalized;
        state.HeadCueId = HeadCueCatalog.ResolveForEmotion(normalized);
    }

    private static string NormalizeHeadCueId(string? cueId)
    {
        return HeadCueCatalog.Get(cueId).Id;
    }

    private static int ClampWpm(int wpm) => Math.Clamp(wpm, MinWpm, MaxWpm);

    private static string? NormalizeEmotion(string? emotion) =>
        string.IsNullOrWhiteSpace(emotion) ? null : emotion.Trim();

    private static EmotionColorSet ResolveEmotionColors(string? emotion)
    {
        if (!string.IsNullOrWhiteSpace(emotion) && EmotionStyles.TryGetValue(emotion, out var set))
        {
            return set;
        }

        return EmotionStyles["neutral"];
    }

    private static string CleanSegmentName(string name)
    {
        name = Regex.Replace(name, @"\|\d+WPM.*$", string.Empty, RegexOptions.IgnoreCase);
        return name.Trim();
    }

    private static string CleanBlockName(string name)
    {
        name = Regex.Replace(name, @"\|\d+WPM.*$", string.Empty, RegexOptions.IgnoreCase);
        return name.Trim();
    }

    private static int CalculateORP(string word)
    {
        var length = word.Length;
        if (length <= 2)
        {
            return 0;
        }

        if (length <= 5)
        {
            return 1;
        }

        if (length <= 9)
        {
            return 2;
        }

        if (length <= 13)
        {
            return 3;
        }

        return 4;
    }

    private static TimeSpan CalculateDisplayDuration(string word, int wpm)
    {
        var clamped = ClampWpm(wpm);
        var baseMs = 60000.0 / clamped;
        var adjustedMs = baseMs * (0.8 + (word.Length * 0.04));
        return TimeSpan.FromMilliseconds(adjustedMs);
    }

    private sealed record EmotionColorSet(string Background, string Text, string Accent);

    private sealed class ScopeFrame(string tag, ScriptCompiler.FormattingState state)
    {
        public string Tag { get; } = tag.ToLowerInvariant();
        public FormattingState State { get; } = state;
    }

    private sealed class FormattingState
    {
        public int BaseWpm { get; set; }
        public string? Emotion { get; set; }
        public string? Color { get; set; }
        public bool IsEmphasis { get; set; }
        public int? SpeedOverride { get; set; }
        public float SpeedMultiplier { get; set; } = 1f;
        public string? Pronunciation { get; set; }
        public string? HeadCueId { get; set; }

        public FormattingState Clone()
        {
            return new FormattingState
            {
                BaseWpm = BaseWpm,
                Emotion = Emotion,
                Color = Color,
                IsEmphasis = IsEmphasis,
                SpeedOverride = SpeedOverride,
                SpeedMultiplier = SpeedMultiplier,
                Pronunciation = Pronunciation,
                HeadCueId = HeadCueId
            };
        }
    }
}
