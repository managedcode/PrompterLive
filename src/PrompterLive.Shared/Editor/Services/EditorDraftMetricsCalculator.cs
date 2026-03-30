using System.Text.RegularExpressions;
using PrompterLive.Core.Models.Documents;

namespace PrompterLive.Shared.Services.Editor;

internal static partial class EditorDraftMetricsCalculator
{
    private const int MaxWpm = 600;
    private const int MinWpm = 60;
    private const double MillisecondsPerMinute = 60_000d;
    private const double WordDurationBaseFactor = 0.8d;
    private const double WordDurationLengthFactor = 0.04d;

    public static EditorDraftMetrics Calculate(string? text, int baseWpm)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return EditorDraftMetrics.Empty;
        }

        var sanitizedText = FormattingTokenRegex()
            .Replace(text.Replace("\r\n", "\n", StringComparison.Ordinal), " ");
        var wordCount = 0;
        var totalMilliseconds = 0d;

        foreach (Match match in PlainWordRegex().Matches(sanitizedText))
        {
            if (string.IsNullOrWhiteSpace(match.Value))
            {
                continue;
            }

            wordCount++;
            totalMilliseconds += CalculateWordDurationMilliseconds(match.Value, baseWpm);
        }

        return new EditorDraftMetrics(wordCount, TimeSpan.FromMilliseconds(totalMilliseconds));
    }

    public static EditorDraftMetrics Calculate(ScriptData? scriptData)
    {
        if (scriptData?.Segments is not { Length: > 0 } segments)
        {
            return EditorDraftMetrics.Empty;
        }

        var wordCount = 0;
        var totalMilliseconds = 0d;

        foreach (var segment in segments)
        {
            AccumulateSegment(segment, ResolveWpm(segment.WpmOverride, scriptData.TargetWpm), ref wordCount, ref totalMilliseconds);
        }

        return new EditorDraftMetrics(wordCount, TimeSpan.FromMilliseconds(totalMilliseconds));
    }

    private static void AccumulateSegment(ScriptSegment segment, int segmentWpm, ref int wordCount, ref double totalMilliseconds)
    {
        if (segment.Blocks is not { Length: > 0 } blocks)
        {
            AccumulatePlainText(segment.Content, segmentWpm, ref wordCount, ref totalMilliseconds);
            return;
        }

        foreach (var block in blocks)
        {
            AccumulateBlock(block, ResolveWpm(block.WpmOverride, segmentWpm), ref wordCount, ref totalMilliseconds);
        }
    }

    private static void AccumulateBlock(ScriptBlock block, int blockWpm, ref int wordCount, ref double totalMilliseconds)
    {
        if (block.Phrases is not { Length: > 0 } phrases)
        {
            AccumulatePlainText(block.Content, blockWpm, ref wordCount, ref totalMilliseconds);
            return;
        }

        foreach (var phrase in phrases)
        {
            AccumulatePhrase(phrase, blockWpm, ref wordCount, ref totalMilliseconds);
        }
    }

    private static void AccumulatePhrase(ScriptPhrase phrase, int blockWpm, ref int wordCount, ref double totalMilliseconds)
    {
        totalMilliseconds += Math.Max(0, phrase.PauseDuration ?? 0);

        if (phrase.Words is not { Length: > 0 } words)
        {
            AccumulatePlainText(phrase.Text, blockWpm, ref wordCount, ref totalMilliseconds);
            return;
        }

        foreach (var word in words)
        {
            AccumulateWord(word, ResolveWpm(word.WpmOverride, blockWpm), ref wordCount, ref totalMilliseconds);
        }
    }

    private static void AccumulateWord(ScriptWord word, int wordWpm, ref int wordCount, ref double totalMilliseconds)
    {
        if (!string.IsNullOrWhiteSpace(word.Text))
        {
            wordCount++;
            totalMilliseconds += CalculateWordDurationMilliseconds(word.Text, wordWpm);
        }

        totalMilliseconds += Math.Max(0, word.PauseAfter ?? 0);
    }

    private static void AccumulatePlainText(string content, int wpm, ref int wordCount, ref double totalMilliseconds)
    {
        foreach (Match match in PlainWordRegex().Matches(content ?? string.Empty))
        {
            var value = match.Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            wordCount++;
            totalMilliseconds += CalculateWordDurationMilliseconds(value, wpm);
        }
    }

    private static double CalculateWordDurationMilliseconds(string word, int wpm)
    {
        var baseMilliseconds = MillisecondsPerMinute / ClampWpm(wpm);
        return baseMilliseconds * (WordDurationBaseFactor + (word.Length * WordDurationLengthFactor));
    }

    private static int ResolveWpm(int? overrideWpm, int fallbackWpm) => ClampWpm(overrideWpm ?? fallbackWpm);

    private static int ClampWpm(int wpm) => Math.Clamp(wpm, MinWpm, MaxWpm);

    [GeneratedRegex(@"\p{L}[\p{L}\p{N}'’-]*|\p{N}+", RegexOptions.Compiled)]
    private static partial Regex PlainWordRegex();

    [GeneratedRegex(@"\[(?:/?[^\[\]]+)\]|#+", RegexOptions.Compiled)]
    private static partial Regex FormattingTokenRegex();
}

internal readonly record struct EditorDraftMetrics(int WordCount, TimeSpan EstimatedDuration)
{
    public static EditorDraftMetrics Empty { get; } = new(0, TimeSpan.Zero);
}
