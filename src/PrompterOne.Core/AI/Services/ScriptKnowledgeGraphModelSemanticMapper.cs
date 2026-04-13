using PrompterOne.Core.AI.Models;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptKnowledgeGraphModelSemanticMapper
{
    private const string SemanticNodePrefix = "prompterone:semantic:";
    private const string SemanticNodeGroup = "model";
    private const string ModelScopeEdgeLabel = "model scope";
    private const string DefaultModelEdgeLabel = "related";

    public static void AddModelExtraction(
        string content,
        IEnumerable<ScriptKnowledgeGraphSemanticScope> scopes,
        ScriptKnowledgeGraphSemanticExtraction extraction,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var scopeByLabel = scopes
            .GroupBy(static scope => scope.Label, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);
        var nodeIdByLabel = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in extraction.Nodes)
        {
            if (TryCreateModelNode(content, item, scopeByLabel, nodes, edges, ranges, out var nodeId))
            {
                nodeIdByLabel.TryAdd(item.Label, nodeId);
            }
        }

        AddModelLinks(extraction, nodeIdByLabel, edges);
    }

    private static void AddModelLinks(
        ScriptKnowledgeGraphSemanticExtraction extraction,
        IReadOnlyDictionary<string, string> nodeIdByLabel,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges)
    {
        foreach (var link in extraction.Links)
        {
            if (nodeIdByLabel.TryGetValue(link.SourceLabel, out var sourceId) &&
                nodeIdByLabel.TryGetValue(link.TargetLabel, out var targetId))
            {
                ScriptKnowledgeGraphEdges.Add(edges, sourceId, targetId, NormalizeModelEdgeLabel(link.Label));
            }
        }
    }

    private static bool TryCreateModelNode(
        string content,
        ScriptKnowledgeGraphSemanticNode item,
        IReadOnlyDictionary<string, ScriptKnowledgeGraphSemanticScope> scopeByLabel,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges,
        out string nodeId)
    {
        var kind = NormalizeModelKind(item.Kind);
        var label = item.Label.Trim();
        nodeId = CreateModelNodeId(kind, label);
        if (string.IsNullOrWhiteSpace(label) || scopeByLabel.Count == 0)
        {
            return false;
        }

        var scope = ResolveModelScope(item.ScopeLabel, scopeByLabel);
        nodes.TryAdd(
            nodeId,
            new ScriptKnowledgeGraphNode(
                nodeId,
                label,
                kind,
                SemanticNodeGroup,
                item.Detail,
                CreateModelAttributes(kind, scope.Label, item.Confidence)));
        ScriptKnowledgeGraphEdges.Add(edges, scope.NodeId, nodeId, ModelScopeEdgeLabel);
        AddSourceRangeFromProbes(content, nodeId, [item.SourceQuote, item.Label], ranges);
        return true;
    }

    private static ScriptKnowledgeGraphSemanticScope ResolveModelScope(
        string? scopeLabel,
        IReadOnlyDictionary<string, ScriptKnowledgeGraphSemanticScope> scopeByLabel)
    {
        if (!string.IsNullOrWhiteSpace(scopeLabel) &&
            scopeByLabel.TryGetValue(scopeLabel.Trim(), out var scope))
        {
            return scope;
        }

        return scopeByLabel.Values.FirstOrDefault(static scope => scope.Label == "Document")
               ?? scopeByLabel.Values.First();
    }

    private static IReadOnlyDictionary<string, string> CreateModelAttributes(
        string kind,
        string scopeLabel,
        double? confidence)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["category"] = kind.ToLowerInvariant(),
            ["scopeLabel"] = scopeLabel,
            ["source"] = "llm"
        };
        if (confidence is { } value)
        {
            attributes["confidence"] = value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        return attributes;
    }

    private static string NormalizeModelKind(string kind)
    {
        var normalized = string.Join(
            ' ',
            kind.Trim().Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Semantic";
        }

        return string.Concat(
            normalized[..1].ToUpperInvariant(),
            normalized.Length == 1 ? string.Empty : normalized[1..]);
    }

    private static string CreateModelNodeId(string kind, string label)
    {
        var key = NormalizeKnowledgeKey($"{kind}:{label}");
        return SemanticNodePrefix + StableHash(key);
    }

    private static string NormalizeModelEdgeLabel(string label)
    {
        var normalized = string.Join(' ', label.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length == 0 ? DefaultModelEdgeLabel : normalized;
    }

    private static void AddSourceRangeFromProbes(
        string content,
        string nodeId,
        IEnumerable<string?> probes,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        foreach (var probe in probes)
        {
            if (string.IsNullOrWhiteSpace(probe) ||
                !TryAddSourceRange(content, nodeId, probe.Trim(), ranges))
            {
                continue;
            }

            return;
        }
    }

    private static bool TryAddSourceRange(
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

        ranges.TryAdd(nodeId, ScriptKnowledgeGraphSourceRanges.CreateSourceRange(nodeId, content, start, start + label.Length));
        return true;
    }

    private static string NormalizeKnowledgeKey(string value) =>
        string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static string StableHash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..16];
}
