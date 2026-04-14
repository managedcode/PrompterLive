using System.Globalization;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptKnowledgeGraphDocumentBuilder
{
    private const string SectionNodePrefix = "prompterone:section:";

    public static void AddDocumentGraph(
        string documentNodeId,
        string containsEdgeLabel,
        ScriptKnowledgeGraphBuildRequest request,
        string content,
        string displayContent,
        ICollection<ScriptKnowledgeGraphSemanticScope> scopes,
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
        scopes.Add(new ScriptKnowledgeGraphSemanticScope(documentNodeId, "Document", displayContent));

        AddSectionNodes(documentNodeId, containsEdgeLabel, content, nodes, edges, ranges);
    }

    private static void AddSectionNodes(
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

            if (TryCreateSectionNode(line, out var sectionNode))
            {
                nodes[sectionNode.Id] = sectionNode;
                ranges[sectionNode.Id] = ScriptKnowledgeGraphSourceRanges.CreateSourceRange(
                    sectionNode.Id,
                    content,
                    line.Start,
                    line.End);
                ScriptKnowledgeGraphEdges.Add(edges, documentNodeId, sectionNode.Id, containsEdgeLabel);
            }
        }
    }

    private static bool TryCreateSectionNode(ScriptKnowledgeGraphLine line, out ScriptKnowledgeGraphNode node)
    {
        var trimmed = line.Text.Trim();
        if (!trimmed.StartsWith('#') || IsTpsStructureHeader(trimmed))
        {
            node = new ScriptKnowledgeGraphNode(string.Empty, string.Empty, string.Empty);
            return false;
        }

        var label = CreateSectionLabel(trimmed);
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

    private static bool IsTpsStructureHeader(string trimmed) =>
        trimmed.StartsWith("## [", StringComparison.Ordinal) ||
        trimmed.StartsWith("### [", StringComparison.Ordinal);

    private static string CreateSectionLabel(string trimmed)
    {
        var label = trimmed.TrimStart('#', ' ', '[').TrimEnd(']');
        var metadataSeparator = label.IndexOf('|', StringComparison.Ordinal);
        return metadataSeparator <= 0 ? label : label[..metadataSeparator].Trim();
    }
}
