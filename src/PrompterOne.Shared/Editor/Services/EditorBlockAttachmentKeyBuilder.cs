using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PrompterOne.Shared.Services.Editor;

public static partial class EditorBlockAttachmentKeyBuilder
{
    private const string BlockPrefix = "block:";
    private const string FallbackPrefix = "block-position:";

    public static string Build(
        string source,
        int blockStartIndex,
        int blockEndIndex,
        int segmentIndex,
        int blockIndex,
        string? blockName = null)
    {
        var heading = ResolveHeadingAt(source, blockStartIndex, blockEndIndex)
            ?? ResolveHeadingByName(source, blockName)
            ?? string.Empty;

        return string.IsNullOrWhiteSpace(heading)
            ? BuildFallback(segmentIndex, blockIndex)
            : BuildHeadingKey(heading);
    }

    public static string BuildFromBlockName(
        string source,
        string? blockName,
        int segmentIndex,
        int blockIndex)
    {
        var heading = ResolveHeadingByName(source, blockName);
        return string.IsNullOrWhiteSpace(heading)
            ? BuildFallback(segmentIndex, blockIndex)
            : BuildHeadingKey(heading);
    }

    private static string? ResolveHeadingAt(string source, int blockStartIndex, int blockEndIndex)
    {
        if (string.IsNullOrWhiteSpace(source) || blockStartIndex < 0 || blockStartIndex >= source.Length)
        {
            return null;
        }

        var safeEnd = Math.Clamp(blockEndIndex + 1, blockStartIndex, source.Length);
        var newlineIndex = source.IndexOf('\n', blockStartIndex, safeEnd - blockStartIndex);
        var lineEnd = newlineIndex >= 0 ? newlineIndex : safeEnd;
        var line = source[blockStartIndex..lineEnd].Trim();
        return IsBlockHeading(line) ? line : null;
    }

    private static string? ResolveHeadingByName(string source, string? blockName)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(blockName))
        {
            return null;
        }

        var normalizedName = NormalizeName(blockName);
        foreach (Match match in BlockHeadingRegex().Matches(source))
        {
            var headingName = match.Groups["name"].Value;
            if (string.Equals(NormalizeName(headingName), normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return match.Value.Trim();
            }
        }

        return null;
    }

    private static string BuildHeadingKey(string heading)
    {
        var normalized = Regex.Replace(heading.Trim(), @"\s+", " ").ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return string.Concat(BlockPrefix, Convert.ToHexString(hash)[..24].ToLowerInvariant());
    }

    private static string BuildFallback(int segmentIndex, int blockIndex) =>
        string.Concat(
            FallbackPrefix,
            segmentIndex.ToString(CultureInfo.InvariantCulture),
            ":",
            blockIndex.ToString(CultureInfo.InvariantCulture));

    private static bool IsBlockHeading(string line) =>
        BlockHeadingRegex().IsMatch(line);

    private static string NormalizeName(string name) =>
        Regex.Replace(name.Trim(), @"\s+", " ");

    [GeneratedRegex(@"^\s{0,3}###\s*\[(?<name>[^\]|\r\n]+)(?:\|[^\]\r\n]*)?\]\s*$", RegexOptions.Multiline)]
    private static partial Regex BlockHeadingRegex();
}
