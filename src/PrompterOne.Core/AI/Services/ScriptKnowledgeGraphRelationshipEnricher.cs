using System.Globalization;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

internal static class ScriptKnowledgeGraphRelationshipEnricher
{
    private const int MaximumCoOccurrenceNodesPerScope = 10;
    private const string CoOccursEdgeLabel = "co-occurs";

    public static void AddRelationships(
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges)
    {
        var scopeNodeIds = nodes.Values
            .Where(IsScopeNode)
            .Select(static node => node.Id)
            .ToHashSet(StringComparer.Ordinal);
        var semanticNodeIds = nodes.Values
            .Where(IsSemanticNode)
            .Select(static node => node.Id)
            .ToHashSet(StringComparer.Ordinal);

        AddCoOccurrenceEdges(scopeNodeIds, semanticNodeIds, edges);
        AnnotateSemanticNodeWeights(nodes, edges.Values, scopeNodeIds, semanticNodeIds);
        AnnotateCentrality(nodes, edges.Values);
    }

    private static void AddCoOccurrenceEdges(
        IReadOnlySet<string> scopeNodeIds,
        IReadOnlySet<string> semanticNodeIds,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges)
    {
        var targetsByScope = edges.Values
            .Where(edge => scopeNodeIds.Contains(edge.SourceId) && semanticNodeIds.Contains(edge.TargetId))
            .GroupBy(static edge => edge.SourceId, StringComparer.Ordinal);

        foreach (var scope in targetsByScope)
        {
            var targetIds = scope
                .Select(static edge => edge.TargetId)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Take(MaximumCoOccurrenceNodesPerScope)
                .ToArray();

            for (var left = 0; left < targetIds.Length; left++)
            {
                for (var right = left + 1; right < targetIds.Length; right++)
                {
                    ScriptKnowledgeGraphEdges.Add(edges, targetIds[left], targetIds[right], CoOccursEdgeLabel);
                }
            }
        }
    }

    private static void AnnotateSemanticNodeWeights(
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IEnumerable<ScriptKnowledgeGraphEdge> edges,
        IReadOnlySet<string> scopeNodeIds,
        IReadOnlySet<string> semanticNodeIds)
    {
        var scopesByNode = edges
            .Where(edge => scopeNodeIds.Contains(edge.SourceId) && semanticNodeIds.Contains(edge.TargetId))
            .GroupBy(static edge => edge.TargetId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                group => group.Select(static edge => edge.SourceId).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        foreach (var (nodeId, scopeIds) in scopesByNode)
        {
            if (!nodes.TryGetValue(nodeId, out var node))
            {
                continue;
            }

            var scopeLabels = scopeIds
                .Select(scopeId => nodes.TryGetValue(scopeId, out var scopeNode) ? scopeNode.Label : string.Empty)
                .Where(static label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            nodes[nodeId] = node with
            {
                Attributes = WithAttributes(
                    node.Attributes,
                    ("weight", scopeIds.Length.ToString(CultureInfo.InvariantCulture)),
                    ("scopeCount", scopeIds.Length.ToString(CultureInfo.InvariantCulture)),
                    ("firstScope", scopeLabels.FirstOrDefault() ?? string.Empty),
                    ("lastScope", scopeLabels.LastOrDefault() ?? string.Empty))
            };
        }
    }

    private static void AnnotateCentrality(
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IEnumerable<ScriptKnowledgeGraphEdge> edges)
    {
        var degreeByNode = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var edge in edges)
        {
            Increment(degreeByNode, edge.SourceId);
            Increment(degreeByNode, edge.TargetId);
        }

        foreach (var (nodeId, degree) in degreeByNode)
        {
            if (!nodes.TryGetValue(nodeId, out var node) || !IsSemanticNode(node))
            {
                continue;
            }

            nodes[nodeId] = node with
            {
                Attributes = WithAttributes(node.Attributes, ("centrality", degree.ToString(CultureInfo.InvariantCulture)))
            };
        }
    }

    private static IReadOnlyDictionary<string, string> WithAttributes(
        IReadOnlyDictionary<string, string>? attributes,
        params (string Key, string Value)[] additions)
    {
        var next = attributes is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : attributes.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
        foreach (var (key, value) in additions)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                next[key] = value;
            }
        }

        return next;
    }

    private static void Increment(IDictionary<string, int> values, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        values[key] = values.TryGetValue(key, out var count) ? count + 1 : 1;
    }

    private static bool IsScopeNode(ScriptKnowledgeGraphNode node) =>
        node.Attributes?.ContainsKey("scope") == true ||
        node.Attributes?.ContainsKey("headingLevel") == true;

    private static bool IsSemanticNode(ScriptKnowledgeGraphNode node) =>
        !IsScopeNode(node) &&
        (string.Equals(node.Group, "model", StringComparison.Ordinal) ||
         string.Equals(node.Group, "story", StringComparison.Ordinal) ||
         string.Equals(node.Group, "knowledge", StringComparison.Ordinal) ||
         string.Equals(node.Kind, "Character", StringComparison.Ordinal));
}
