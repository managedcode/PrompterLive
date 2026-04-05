using System.Text;
using System.Text.RegularExpressions;
using ManagedCode.Tps;
using PrompterOne.Core.Models.Editor;

namespace PrompterOne.Core.Services.Editor;

public sealed partial class TpsFrontMatterDocumentService
{
    private static readonly string[] PreferredMetadataOrder =
    [
        MetadataKeys.Title,
        MetadataKeys.Profile,
        MetadataKeys.Duration,
        MetadataKeys.BaseWpm,
        MetadataKeys.Author,
        MetadataKeys.Created,
        MetadataKeys.Version
    ];

    [GeneratedRegex(@"\A---\s*\r?\n(?<front>.*?)\r?\n---\s*\r?\n?", RegexOptions.Singleline)]
    private static partial Regex FrontMatterRegex();

    public TpsFrontMatterDocument Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TpsFrontMatterDocument(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), string.Empty, 0);
        }

        var match = FrontMatterRegex().Match(text);
        if (!match.Success)
        {
            return new TpsFrontMatterDocument(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), text, 0);
        }

        var metadata = ParseMetadata(match.Groups["front"].Value);
        return new TpsFrontMatterDocument(metadata, text[match.Length..], match.Length);
    }

    public string Build(IReadOnlyDictionary<string, string> metadata, string? body)
    {
        var builder = new StringBuilder();
        builder.AppendLine("---");

        foreach (var key in PreferredMetadataOrder.Where(metadata.ContainsKey))
        {
            AppendMetadataEntry(builder, key, metadata[key], IsNumericKey(key));
        }

        AppendSpeedOffsets(builder, metadata);

        foreach (var entry in metadata
                     .Where(entry => !PreferredMetadataOrder.Contains(entry.Key, StringComparer.OrdinalIgnoreCase) && !IsSpeedOffsetKey(entry.Key))
                     .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            AppendMetadataEntry(builder, entry.Key, entry.Value, IsNumericKey(entry.Key));
        }

        builder.AppendLine("---");
        builder.AppendLine();
        builder.Append((body ?? string.Empty).TrimStart());
        return builder.ToString();
    }

    public string Upsert(string? text, IReadOnlyDictionary<string, string?> updates)
    {
        var document = Parse(text);
        var nextMetadata = new Dictionary<string, string>(document.Metadata, StringComparer.OrdinalIgnoreCase);

        foreach (var update in updates)
        {
            if (string.IsNullOrWhiteSpace(update.Key))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(update.Value))
            {
                nextMetadata.Remove(update.Key);
                continue;
            }

            nextMetadata[update.Key] = update.Value.Trim();
        }

        return Build(nextMetadata, document.Body);
    }

    public string ResolveTitle(string? text, string fallback)
    {
        return ResolveValue(text, MetadataKeys.Title, fallback);
    }

    public string ResolveValue(string? text, string key, string fallback)
    {
        var document = Parse(text);
        return document.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;
    }

    public static class MetadataKeys
    {
        public const string Author = TpsSpec.FrontMatterKeys.Author;
        public const string BaseWpm = TpsSpec.FrontMatterKeys.BaseWpm;
        public const string Created = TpsSpec.FrontMatterKeys.Created;
        public const string Duration = TpsSpec.FrontMatterKeys.Duration;
        public const string Profile = TpsSpec.FrontMatterKeys.Profile;
        public const string SpeedOffsetFast = TpsSpec.FrontMatterKeys.SpeedOffsetsFast;
        public const string SpeedOffsetSlow = TpsSpec.FrontMatterKeys.SpeedOffsetsSlow;
        public const string SpeedOffsetXfast = TpsSpec.FrontMatterKeys.SpeedOffsetsXfast;
        public const string SpeedOffsetXslow = TpsSpec.FrontMatterKeys.SpeedOffsetsXslow;
        public const string Title = TpsSpec.FrontMatterKeys.Title;
        public const string Version = TpsSpec.FrontMatterKeys.Version;
    }

    private static Dictionary<string, string> ParseMetadata(string frontMatterText)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        foreach (var rawLine in frontMatterText.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var indentationLength = rawLine.Length - rawLine.TrimStart().Length;
            var line = rawLine.Trim();
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = TrimValue(line[(separatorIndex + 1)..]);
            if (indentationLength > 0 && !string.IsNullOrWhiteSpace(currentSection))
            {
                var compositeKey = $"{currentSection}.{key}";
                if (!IsLegacyKey(compositeKey))
                {
                    metadata[compositeKey] = value;
                }

                continue;
            }

            currentSection = string.IsNullOrWhiteSpace(value) ? key : null;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!IsLegacyKey(key))
            {
                metadata[key] = value;
            }
        }

        return metadata;
    }

    private static void AppendSpeedOffsets(StringBuilder builder, IReadOnlyDictionary<string, string> metadata)
    {
        var speedOffsetKeys = new[]
        {
            MetadataKeys.SpeedOffsetXslow,
            MetadataKeys.SpeedOffsetSlow,
            MetadataKeys.SpeedOffsetFast,
            MetadataKeys.SpeedOffsetXfast
        };

        if (!speedOffsetKeys.Any(metadata.ContainsKey))
        {
            return;
        }

        builder.AppendLine("speed_offsets:");
        AppendNestedSpeedOffset(builder, metadata, MetadataKeys.SpeedOffsetXslow, TpsSpec.Tags.Xslow);
        AppendNestedSpeedOffset(builder, metadata, MetadataKeys.SpeedOffsetSlow, TpsSpec.Tags.Slow);
        AppendNestedSpeedOffset(builder, metadata, MetadataKeys.SpeedOffsetFast, TpsSpec.Tags.Fast);
        AppendNestedSpeedOffset(builder, metadata, MetadataKeys.SpeedOffsetXfast, TpsSpec.Tags.Xfast);
    }

    private static void AppendNestedSpeedOffset(StringBuilder builder, IReadOnlyDictionary<string, string> metadata, string key, string nestedKey)
    {
        if (!metadata.TryGetValue(key, out var value))
        {
            return;
        }

        builder.Append("  ");
        builder.Append(nestedKey);
        builder.Append(": ");
        builder.AppendLine(FormatValue(value, numeric: true));
    }

    private static void AppendMetadataEntry(StringBuilder builder, string key, string value, bool numeric)
    {
        builder.Append(key);
        builder.Append(": ");
        builder.AppendLine(FormatValue(value, numeric));
    }

    private static string TrimValue(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length >= 2 &&
            ((trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal)) ||
             (trimmed.StartsWith("'", StringComparison.Ordinal) && trimmed.EndsWith("'", StringComparison.Ordinal))))
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }

    private static string FormatValue(string value, bool numeric)
    {
        return numeric && int.TryParse(value, out _)
            ? value
            : $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static bool IsNumericKey(string key)
    {
        return key.Equals(MetadataKeys.BaseWpm, StringComparison.OrdinalIgnoreCase) || IsSpeedOffsetKey(key);
    }

    private static bool IsSpeedOffsetKey(string key)
    {
        return key.Equals(MetadataKeys.SpeedOffsetXslow, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(MetadataKeys.SpeedOffsetSlow, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(MetadataKeys.SpeedOffsetFast, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(MetadataKeys.SpeedOffsetXfast, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLegacyKey(string key)
    {
        return key.Equals(TpsSpec.LegacyKeys.DisplayDuration, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.LegacyKeys.XslowOffset, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.LegacyKeys.SlowOffset, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.LegacyKeys.FastOffset, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.LegacyKeys.XfastOffset, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.LegacyKeys.PresetsXslow, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.LegacyKeys.PresetsSlow, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.LegacyKeys.PresetsFast, StringComparison.OrdinalIgnoreCase) ||
               key.Equals(TpsSpec.LegacyKeys.PresetsXfast, StringComparison.OrdinalIgnoreCase);
    }
}
