using PrompterOne.Core.AI.Models;
using System.Globalization;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptKnowledgeGraphDocumentBuilder
{
    private const string LineNodePrefix = "prompterone:line:";
    private const string SectionNodePrefix = "prompterone:section:";

    public static void AddDocumentGraph(
        string documentNodeId,
        string containsEdgeLabel,
        ScriptKnowledgeGraphBuildRequest request,
        string content,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        nodes[documentNodeId] = new ScriptKnowledgeGraphNode(
            documentNodeId,
            string.IsNullOrWhiteSpace(request.Title) ? "Script document" : request.Title.Trim(),
            "Document",
            "script",
            content.Length == 0 ? "Empty script document" : "Script document root",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["source"] = "script",
                ["documentId"] = request.DocumentId ?? string.Empty
            });

        AddLineNodes(documentNodeId, containsEdgeLabel, content, nodes, edges, ranges);
    }

    private static void AddLineNodes(
        string documentNodeId,
        string containsEdgeLabel,
        string content,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        foreach (var line in ScriptKnowledgeGraphSourceRanges.EnumerateLines(content))
        {
            if (string.IsNullOrWhiteSpace(line.Text))
            {
                continue;
            }

            var nodeId = CreateLineNodeId(line.Number);
            nodes[nodeId] = new ScriptKnowledgeGraphNode(
                nodeId,
                CreateLineLabel(line),
                "Line",
                "script",
                line.Text.Trim(),
                CreateLineAttributes(line));
            ranges[nodeId] = ScriptKnowledgeGraphSourceRanges.CreateSourceRange(nodeId, content, line.Start, line.End);
            ScriptKnowledgeGraphEdges.Add(edges, documentNodeId, nodeId, containsEdgeLabel);

            if (TryCreateSectionNode(line, out var sectionNode))
            {
                nodes[sectionNode.Id] = sectionNode;
                ranges[sectionNode.Id] = ScriptKnowledgeGraphSourceRanges.CreateSourceRange(
                    sectionNode.Id,
                    content,
                    line.Start,
                    line.End);
                ScriptKnowledgeGraphEdges.Add(edges, documentNodeId, sectionNode.Id, containsEdgeLabel);
                ScriptKnowledgeGraphEdges.Add(edges, sectionNode.Id, nodeId, "starts at");
            }
        }
    }

    private static bool TryCreateSectionNode(ScriptKnowledgeGraphLine line, out ScriptKnowledgeGraphNode node)
    {
        var trimmed = line.Text.Trim();
        if (!trimmed.StartsWith('#'))
        {
            node = new ScriptKnowledgeGraphNode(string.Empty, string.Empty, string.Empty);
            return false;
        }

        var label = trimmed.TrimStart('#', ' ', '[').TrimEnd(']');
        var headingLevel = trimmed.TakeWhile(static character => character == '#').Count();
        node = new ScriptKnowledgeGraphNode(
            SectionNodePrefix + line.Number.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(label) ? $"Section line {line.Number}" : label,
            "Section",
            "script",
            trimmed,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["source"] = "script",
                ["line"] = line.Number.ToString(CultureInfo.InvariantCulture),
                ["headingLevel"] = headingLevel.ToString(CultureInfo.InvariantCulture)
            });
        return true;
    }

    private static string CreateLineLabel(ScriptKnowledgeGraphLine line)
    {
        var text = line.Text.Trim();
        return text.Length <= 80 ? $"Line {line.Number}: {text}" : $"Line {line.Number}: {text[..80]}";
    }

    private static string CreateLineNodeId(int lineNumber) =>
        LineNodePrefix + lineNumber.ToString(CultureInfo.InvariantCulture);

    private static IReadOnlyDictionary<string, string> CreateLineAttributes(ScriptKnowledgeGraphLine line) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = "script",
            ["line"] = line.Number.ToString(CultureInfo.InvariantCulture)
        };
}
