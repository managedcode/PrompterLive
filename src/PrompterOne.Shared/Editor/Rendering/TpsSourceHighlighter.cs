using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace PrompterOne.Shared.Rendering;

public static class TpsSourceHighlighter
{
    private static readonly Regex BlockHeaderRegex = new(@"^(?<hash>###)\s+\[(?<content>.+)\]\s*$", RegexOptions.Compiled);
    private static readonly Regex FrontMatterEntryRegex = new(@"^(?<indent>\s*)(?<key>[A-Za-z0-9_]+):\s*(?<value>.*)$", RegexOptions.Compiled);
    private static readonly Regex HeaderWpmRegex = new(@"^\d+\s*WPM$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SegmentHeaderRegex = new(@"^(?<hash>##)\s+\[(?<content>.+)\]\s*$", RegexOptions.Compiled);
    private static readonly HashSet<string> EmotionTokens =
    [
        "neutral",
        "warm",
        "professional",
        "focused",
        "concerned",
        "urgent",
        "motivational",
        "excited",
        "happy",
        "sad",
        "calm",
        "energetic"
    ];

    public static MarkupString Render(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (MarkupString)"<div class=\"ed-src-line ed-src-line-empty\">Start writing in TPS.</div>";
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = normalized.Split('\n');
        var builder = new StringBuilder();
        var inFrontMatter = lines.Length > 0 && string.Equals(lines[0], "---", StringComparison.Ordinal);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (inFrontMatter)
            {
                builder.Append(RenderFrontMatterLine(line));
                if (index > 0 && string.Equals(line, "---", StringComparison.Ordinal))
                {
                    inFrontMatter = false;
                }

                continue;
            }

            builder.Append(RenderBodyLine(line));
        }

        return (MarkupString)builder.ToString();
    }

    private static string RenderFrontMatterLine(string line)
    {
        if (string.Equals(line, "---", StringComparison.Ordinal))
        {
            return WrapLine("ed-src-line ed-src-line-frontmatter", "<span class=\"ed-src-frontmatter-delimiter\">---</span>");
        }

        var match = FrontMatterEntryRegex.Match(line);
        if (!match.Success)
        {
            return WrapLine("ed-src-line ed-src-line-frontmatter", EncodeOrSpace(line));
        }

        return WrapLine(
            "ed-src-line ed-src-line-frontmatter",
            string.Concat(
                WebUtility.HtmlEncode(match.Groups["indent"].Value),
                "<span class=\"ed-src-frontmatter-key\">",
                WebUtility.HtmlEncode(match.Groups["key"].Value),
                "</span>: <span class=\"ed-src-frontmatter-value\">",
                EncodeOrSpace(match.Groups["value"].Value),
                "</span>"));
    }

    private static string RenderBodyLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return WrapLine("ed-src-line ed-src-line-empty", "&nbsp;");
        }

        if (SegmentHeaderRegex.Match(line) is { Success: true } segmentMatch)
        {
            return WrapLine(
                "ed-src-line ed-src-line-segment",
                BuildHeaderMarkup(segmentMatch.Groups["hash"].Value, segmentMatch.Groups["content"].Value, true));
        }

        if (BlockHeaderRegex.Match(line) is { Success: true } blockMatch)
        {
            return WrapLine(
                "ed-src-line ed-src-line-block",
                BuildHeaderMarkup(blockMatch.Groups["hash"].Value, blockMatch.Groups["content"].Value, false));
        }

        return WrapLine("ed-src-line", EditorMarkupRenderer.Render(line).Value);
    }

    private static string BuildHeaderMarkup(string hashToken, string content, bool isSegment)
    {
        var parts = content.Split('|', StringSplitOptions.TrimEntries);
        var builder = new StringBuilder();
        builder.Append("<span class=\"h-mark\">")
            .Append(WebUtility.HtmlEncode(hashToken))
            .Append(" </span><span class=\"h-br\">[</span>");

        for (var index = 0; index < parts.Length; index++)
        {
            if (index > 0)
            {
                builder.Append("<span class=\"h-sep\">|</span>");
            }

            var cssClass = ResolveHeaderPartCssClass(parts[index], index, isSegment);

            builder.Append("<span class=\"")
                .Append(cssClass)
                .Append("\">")
                .Append(WebUtility.HtmlEncode(parts[index]))
                .Append("</span>");
        }

        builder.Append("<span class=\"h-br\">]</span>");
        return builder.ToString();
    }

    private static string ResolveHeaderPartCssClass(string part, int index, bool isSegment)
    {
        if (index == 0)
        {
            return "h-name";
        }

        if (part.StartsWith("Speaker:", StringComparison.OrdinalIgnoreCase))
        {
            return "h-speaker";
        }

        if (HeaderWpmRegex.IsMatch(part))
        {
            return "h-wpm";
        }

        if (EmotionTokens.Contains(part))
        {
            return "h-emo";
        }

        if (isSegment && part.Contains('-', StringComparison.Ordinal) && part.Contains(':', StringComparison.Ordinal))
        {
            return "h-timing";
        }

        return "h-meta";
    }

    private static string EncodeOrSpace(string line) =>
        string.IsNullOrEmpty(line) ? "&nbsp;" : WebUtility.HtmlEncode(line);

    private static string WrapLine(string cssClass, string markup) =>
        $"<div class=\"{cssClass}\">{markup}</div>";
}
