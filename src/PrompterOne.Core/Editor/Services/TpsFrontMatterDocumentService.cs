using System.Text;
using System.Text.RegularExpressions;
using PrompterOne.Core.Models.Editor;

namespace PrompterOne.Core.Services.Editor;

public sealed class TpsFrontMatterDocumentService
{
    private static readonly Regex FrontMatterRegex = new(
        @"\A---\s*\r?\n(?<front>.*?)\r?\n---\s*\r?\n?",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly string[] PreferredMetadataOrder =
    [
        MetadataKeys.Title,
        MetadataKeys.Author,
        MetadataKeys.Profile,
        MetadataKeys.BaseWpm,
        MetadataKeys.DisplayDuration,
        MetadataKeys.XslowOffset,
        MetadataKeys.SlowOffset,
        MetadataKeys.FastOffset,
        MetadataKeys.XfastOffset,
        MetadataKeys.Version,
        MetadataKeys.Created
    ];

    public TpsFrontMatterDocument Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TpsFrontMatterDocument(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), string.Empty, 0);
        }

        var match = FrontMatterRegex.Match(text);
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
        builder.AppendLine(MetadataTokens.Delimiter);

        foreach (var key in PreferredMetadataOrder.Where(metadata.ContainsKey))
        {
            builder.Append(key);
            builder.Append(MetadataTokens.Separator);
            builder.AppendLine(FormatMetadataValue(key, metadata[key]));
        }

        foreach (var entry in metadata
                     .Where(entry => !PreferredMetadataOrder.Contains(entry.Key, StringComparer.OrdinalIgnoreCase))
                     .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(entry.Key);
            builder.Append(MetadataTokens.Separator);
            builder.AppendLine(FormatMetadataValue(entry.Key, entry.Value));
        }

        builder.AppendLine(MetadataTokens.Delimiter);
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
        var document = Parse(text);
        return document.Metadata.TryGetValue(MetadataKeys.Title, out var title) &&
               !string.IsNullOrWhiteSpace(title)
            ? title.Trim()
            : fallback;
    }

    public string ResolveValue(string? text, string key, string fallback)
    {
        var document = Parse(text);
        return document.Metadata.TryGetValue(key, out var value) &&
               !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;
    }

    private static string FormatMetadataValue(string key, string? value)
    {
        value ??= string.Empty;

        if (IsNumericMetadataKey(key) && int.TryParse(value, out _))
        {
            return value;
        }

        return $"{MetadataTokens.Quote}{value.Replace(MetadataTokens.Quote, MetadataTokens.EscapedQuote, StringComparison.Ordinal)}{MetadataTokens.Quote}";
    }

    public static class MetadataKeys
    {
        public const string Author = "author";
        public const string BaseWpm = "base_wpm";
        public const string Created = "created";
        public const string DisplayDuration = "display_duration";
        public const string FastOffset = "fast_offset";
        public const string Profile = "profile";
        public const string SlowOffset = "slow_offset";
        public const string Title = "title";
        public const string Version = "version";
        public const string XfastOffset = "xfast_offset";
        public const string XslowOffset = "xslow_offset";
    }

    private static Dictionary<string, string> ParseMetadata(string frontMatterText)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        foreach (var rawLine in frontMatterText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.TrimEnd();
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            var separatorIndex = trimmedLine.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmedLine[..separatorIndex].Trim();
            var value = trimmedLine[(separatorIndex + 1)..].Trim().Trim('"', '\'');
            var isNestedLine = line.Length != line.TrimStart().Length;

            if (isNestedLine && !string.IsNullOrWhiteSpace(currentSection))
            {
                metadata[$"{currentSection}.{key}"] = value;
                continue;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                currentSection = key;
                continue;
            }

            currentSection = null;
            metadata[key] = value;
        }

        return TpsFrontMatterMetadataNormalizer.Normalize(metadata);
    }

    private static bool IsNumericMetadataKey(string key) =>
        string.Equals(key, MetadataKeys.BaseWpm, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, MetadataKeys.XslowOffset, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, MetadataKeys.SlowOffset, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, MetadataKeys.FastOffset, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, MetadataKeys.XfastOffset, StringComparison.OrdinalIgnoreCase);

    private static class MetadataTokens
    {
        public const string Delimiter = "---";
        public const string EscapedQuote = "\\\"";
        public const string Quote = "\"";
        public const string Separator = ": ";
    }
}
