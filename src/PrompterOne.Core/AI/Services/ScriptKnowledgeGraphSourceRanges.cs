using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptKnowledgeGraphSourceRanges
{
    public static void AddRangeIfFound(
        string content,
        string nodeId,
        string label,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var start = content.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return;
        }

        ranges.TryAdd(nodeId, CreateSourceRange(nodeId, content, start, start + label.Length));
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
