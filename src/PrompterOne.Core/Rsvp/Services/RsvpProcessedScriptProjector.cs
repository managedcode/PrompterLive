using ManagedCode.Tps;
using PrompterOne.Core.Models.Documents;

namespace PrompterOne.Core.Services.Rsvp;

internal static class RsvpProcessedScriptProjector
{
    public static RsvpTextProcessor.ProcessedScript Build(ScriptData scriptData, bool usePlainTextReadingDefaults)
    {
        var processed = new RsvpTextProcessor.ProcessedScript();
        var segments = scriptData.Segments ?? [];

        foreach (var segment in segments)
        {
            AppendSegment(processed, scriptData, segment, usePlainTextReadingDefaults);
        }

        if (processed.Segments.Count == 0)
        {
            processed.Segments.Add(CreateFallbackSegment(scriptData, usePlainTextReadingDefaults, processed.AllWords.Count));
        }

        return processed;
    }

    private static void AppendSegment(
        RsvpTextProcessor.ProcessedScript processed,
        ScriptData scriptData,
        ScriptSegment segment,
        bool usePlainTextReadingDefaults)
    {
        var segmentIndex = processed.Segments.Count;
        var segmentSpeed = ResolveSegmentSpeed(segment.WpmOverride ?? scriptData.TargetWpm, usePlainTextReadingDefaults);
        var processedSegment = new RsvpTextProcessor.ProcessedSegment
        {
            Title = ResolveSegmentTitle(segment.Name),
            Emotion = ResolveEmotion(segment.Emotion, TpsSpec.DefaultEmotion),
            Speed = segmentSpeed,
            StartIndex = processed.AllWords.Count
        };

        foreach (var block in EnumerateBlocks(segment))
        {
            RsvpProcessedWordAppender.AppendBlock(processed, processedSegment, block, segmentSpeed, segmentIndex);
        }

        processedSegment.EndIndex = Math.Max(processedSegment.StartIndex, processed.AllWords.Count - 1);
        processed.Segments.Add(processedSegment);
    }

    private static RsvpTextProcessor.ProcessedSegment CreateFallbackSegment(
        ScriptData scriptData,
        bool usePlainTextReadingDefaults,
        int wordCount)
    {
        return new RsvpTextProcessor.ProcessedSegment
        {
            Title = RsvpTextProcessorDefaults.DefaultSegmentTitle,
            Emotion = TpsSpec.DefaultEmotion,
            Speed = ResolveSegmentSpeed(scriptData.TargetWpm, usePlainTextReadingDefaults),
            StartIndex = 0,
            EndIndex = Math.Max(0, wordCount - 1)
        };
    }

    private static string ResolveSegmentTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? RsvpTextProcessorDefaults.DefaultSegmentTitle : title.Trim();

    internal static IEnumerable<ScriptBlock> EnumerateBlocks(ScriptSegment segment)
    {
        if (segment.Blocks is { Length: > 0 } blocks)
        {
            return blocks;
        }

        return
        [
            new ScriptBlock
            {
                Name = ResolveSegmentTitle(segment.Name),
                Emotion = segment.Emotion,
                Speaker = segment.Speaker,
                Archetype = segment.Archetype,
                WpmOverride = segment.WpmOverride,
                Content = segment.Content ?? string.Empty
            }
        ];
    }

    internal static int ResolveSegmentSpeed(int? candidateSpeed, bool usePlainTextReadingDefaults)
    {
        if (usePlainTextReadingDefaults && candidateSpeed == TpsSpec.DefaultBaseWpm)
        {
            return RsvpTextProcessorDefaults.DefaultSegmentSpeed;
        }

        return candidateSpeed is > 0
            ? candidateSpeed.Value
            : RsvpTextProcessorDefaults.DefaultSegmentSpeed;
    }

    internal static string ResolveEmotion(string? candidate, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(candidate)
            ? fallback
            : candidate.Trim().ToLowerInvariant();
        return TpsSpec.Emotions.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? normalized
            : fallback;
    }
}
