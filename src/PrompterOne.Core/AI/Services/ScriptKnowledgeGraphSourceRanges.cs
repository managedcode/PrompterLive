using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptKnowledgeGraphSourceRanges
{
    public static bool AddRangeIfFound(
        string content,
        string nodeId,
        string label,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var start = content.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return false;
        }

        ranges.TryAdd(nodeId, CreateSourceRange(nodeId, content, start, start + label.Length));
        return true;
    }

    public static bool AddSequentialTokenRangeIfFound(
        string content,
        string nodeId,
        string displayText,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var tokens = displayText
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static token => token.Length > 0)
            .ToArray();
        if (tokens.Length == 0)
        {
            return false;
        }

        var sourceStart = -1;
        var sourceEnd = -1;
        var searchStart = 0;
        foreach (var token in tokens)
        {
            var next = content.IndexOf(token, searchStart, StringComparison.OrdinalIgnoreCase);
            if (next < 0)
            {
                return false;
            }

            if (sourceStart < 0)
            {
                sourceStart = next;
            }

            sourceEnd = next + token.Length;
            searchStart = sourceEnd;
        }

        ranges.TryAdd(nodeId, CreateSourceRange(nodeId, content, sourceStart, sourceEnd));
        return true;
    }

    public static ScriptKnowledgeGraphSourceRange CreateSourceRange(
        string nodeId,
        string content,
        int start,
        int end)
    {
        return new ScriptKnowledgeGraphSourceRange(
            nodeId,
            new ScriptDocumentRange(start, end),
            ScriptDocumentPosition.FromOffset(content, start),
            ScriptDocumentPosition.FromOffset(content, end));
    }

    public static IEnumerable<ScriptKnowledgeGraphLine> EnumerateLines(string content)
    {
        var lineNumber = 1;
        var lineStart = 0;

        for (var index = 0; index <= content.Length; index++)
        {
            if (index < content.Length && content[index] != '\n')
            {
                continue;
            }

            var end = index > lineStart && content[index - 1] == '\r' ? index - 1 : index;
            yield return new ScriptKnowledgeGraphLine(lineNumber++, content[lineStart..end], lineStart, end);
            lineStart = index + 1;
        }
    }
}
