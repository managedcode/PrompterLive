using System.Globalization;
using System.Text;
using ManagedCode.Tps;
using PrompterOne.Core.Models.Tps;

namespace PrompterOne.Core.Services;

public class TpsExporter
{
    private const string FrontMatterDelimiter = "---";

    public Task<string> ExportAsync(TpsDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var builder = new StringBuilder();
        AppendFrontMatter(builder, document.Metadata);
        AppendSegments(builder, document.Segments);

        return Task.FromResult(builder.ToString().TrimEnd());
    }

    public async Task ExportToFileAsync(TpsDocument document, string filePath)
    {
        var content = await ExportAsync(document).ConfigureAwait(false);
        await File.WriteAllTextAsync(filePath, content).ConfigureAwait(false);
    }

    private static void AppendFrontMatter(StringBuilder builder, IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.Count == 0)
        {
            return;
        }

        builder.AppendLine(FrontMatterDelimiter);
        AppendMetadataValue(builder, metadata, TpsSpec.FrontMatterKeys.Title);
        AppendMetadataValue(builder, metadata, TpsSpec.FrontMatterKeys.Profile);
        AppendMetadataValue(builder, metadata, TpsSpec.FrontMatterKeys.Duration);
        AppendMetadataValue(builder, metadata, TpsSpec.FrontMatterKeys.BaseWpm, numeric: true);
        AppendSpeedOffsets(builder, metadata);
        AppendMetadataValue(builder, metadata, TpsSpec.FrontMatterKeys.Author);
        AppendMetadataValue(builder, metadata, TpsSpec.FrontMatterKeys.Created);
        AppendMetadataValue(builder, metadata, TpsSpec.FrontMatterKeys.Version);

        foreach (var entry in metadata
                     .Where(entry => !IsCanonicalMetadataKey(entry.Key))
                     .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(entry.Key);
            builder.Append(": ");
            builder.AppendLine(FormatMetadataValue(entry.Value, numeric: false));
        }

        builder.AppendLine(FrontMatterDelimiter);
        builder.AppendLine();
    }

    private static void AppendSpeedOffsets(StringBuilder builder, IReadOnlyDictionary<string, string> metadata)
    {
        var speedKeys = new[]
        {
            TpsSpec.FrontMatterKeys.SpeedOffsetsXslow,
            TpsSpec.FrontMatterKeys.SpeedOffsetsSlow,
            TpsSpec.FrontMatterKeys.SpeedOffsetsFast,
            TpsSpec.FrontMatterKeys.SpeedOffsetsXfast
        };

        if (!speedKeys.Any(metadata.ContainsKey))
        {
            return;
        }

        builder.AppendLine("speed_offsets:");
        AppendNestedOffset(builder, metadata, TpsSpec.FrontMatterKeys.SpeedOffsetsXslow, TpsSpec.Tags.Xslow);
        AppendNestedOffset(builder, metadata, TpsSpec.FrontMatterKeys.SpeedOffsetsSlow, TpsSpec.Tags.Slow);
        AppendNestedOffset(builder, metadata, TpsSpec.FrontMatterKeys.SpeedOffsetsFast, TpsSpec.Tags.Fast);
        AppendNestedOffset(builder, metadata, TpsSpec.FrontMatterKeys.SpeedOffsetsXfast, TpsSpec.Tags.Xfast);
    }

    private static void AppendNestedOffset(StringBuilder builder, IReadOnlyDictionary<string, string> metadata, string key, string nestedKey)
    {
        if (!metadata.TryGetValue(key, out var value))
        {
            return;
        }

        builder.Append("  ");
        builder.Append(nestedKey);
        builder.Append(": ");
        builder.AppendLine(FormatMetadataValue(value, numeric: true));
    }

    private static void AppendSegments(StringBuilder builder, IReadOnlyList<TpsSegment> segments)
    {
        foreach (var segment in segments)
        {
            builder.AppendLine(BuildSegmentHeader(segment));
            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(segment.LeadingContent))
            {
                builder.AppendLine(segment.LeadingContent.TrimEnd());
                builder.AppendLine();
            }

            foreach (var block in segment.Blocks)
            {
                builder.AppendLine(BuildBlockHeader(block));
                builder.AppendLine();
                if (!string.IsNullOrWhiteSpace(block.Content))
                {
                    builder.AppendLine(block.Content.TrimEnd());
                    builder.AppendLine();
                }
            }

            if (segment.Blocks.Count == 0 && !string.IsNullOrWhiteSpace(segment.Content))
            {
                builder.AppendLine(segment.Content.TrimEnd());
                builder.AppendLine();
            }
        }
    }

    private static string BuildSegmentHeader(TpsSegment segment)
    {
        var parts = new List<string> { EscapeHeaderPart(segment.Name) };
        if (!string.IsNullOrWhiteSpace(segment.Speaker))
        {
            parts.Add(string.Concat(TpsSpec.SpeakerPrefix, segment.Speaker.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(segment.Archetype))
        {
            parts.Add(string.Concat(TpsSpec.ArchetypePrefix, segment.Archetype.Trim()));
        }

        if (segment.TargetWPM is int targetWpm)
        {
            parts.Add(targetWpm.ToString(CultureInfo.InvariantCulture) + TpsSpec.WpmSuffix);
        }

        if (!string.IsNullOrWhiteSpace(segment.Emotion))
        {
            parts.Add(segment.Emotion.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(segment.Timing))
        {
            parts.Add(segment.Timing.Trim());
        }

        return $"## [{string.Join('|', parts)}]";
    }

    private static string BuildBlockHeader(TpsBlock block)
    {
        var parts = new List<string> { EscapeHeaderPart(block.Name) };
        if (!string.IsNullOrWhiteSpace(block.Speaker))
        {
            parts.Add(string.Concat(TpsSpec.SpeakerPrefix, block.Speaker.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(block.Archetype))
        {
            parts.Add(string.Concat(TpsSpec.ArchetypePrefix, block.Archetype.Trim()));
        }

        if (block.TargetWPM is int targetWpm)
        {
            parts.Add(targetWpm.ToString(CultureInfo.InvariantCulture) + TpsSpec.WpmSuffix);
        }

        if (!string.IsNullOrWhiteSpace(block.Emotion))
        {
            parts.Add(block.Emotion.Trim().ToLowerInvariant());
        }

        return $"### [{string.Join('|', parts)}]";
    }

    private static void AppendMetadataValue(StringBuilder builder, IReadOnlyDictionary<string, string> metadata, string key, bool numeric = false)
    {
        if (!metadata.TryGetValue(key, out var value))
        {
            return;
        }

        builder.Append(key);
        builder.Append(": ");
        builder.AppendLine(FormatMetadataValue(value, numeric));
    }

    private static string FormatMetadataValue(string value, bool numeric)
    {
        return numeric && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
            ? value
            : $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static bool IsCanonicalMetadataKey(string key)
    {
        return key.Equals(TpsSpec.FrontMatterKeys.Title, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.Profile, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.Duration, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.BaseWpm, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.SpeedOffsetsXslow, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.SpeedOffsetsSlow, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.SpeedOffsetsFast, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.SpeedOffsetsXfast, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.Author, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.Created, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.FrontMatterKeys.Version, StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapeHeaderPart(string value)
    {
        return value
            .Replace("\\", @"\\", StringComparison.Ordinal)
            .Replace("|", @"\|", StringComparison.Ordinal);
    }
}
