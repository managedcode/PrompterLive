using ManagedCode.MarkdownLd.Kb.Pipeline;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

public sealed class ScriptKnowledgeGraphService
{
    private const string DocumentNodeId = "prompterone:document";
    private const string ContainsEdgeLabel = "contains";

    public async Task<ScriptKnowledgeGraphArtifact> BuildAsync(
        ScriptKnowledgeGraphBuildRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var content = request.Content ?? string.Empty;
        var pipeline = new MarkdownKnowledgePipeline();
        var kbResult = await pipeline
            .BuildFromMarkdownAsync(content, CreateSourcePath(request.DocumentId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var nodes = new Dictionary<string, ScriptKnowledgeGraphNode>(StringComparer.Ordinal);
        var edges = new Dictionary<string, ScriptKnowledgeGraphEdge>(StringComparer.Ordinal);
        var ranges = new Dictionary<string, ScriptKnowledgeGraphSourceRange>(StringComparer.Ordinal);

        ScriptKnowledgeGraphDocumentBuilder.AddDocumentGraph(
            DocumentNodeId,
            ContainsEdgeLabel,
            request,
            content,
            nodes,
            edges,
            ranges);
        ScriptKnowledgeGraphTpsEnricher.AddTpsGraph(
            DocumentNodeId,
            ContainsEdgeLabel,
            content,
            nodes,
            edges,
            ranges);
        AddKnowledgeBankGraph(kbResult.Graph.ToSnapshot(), content, nodes, edges, ranges);

        return new ScriptKnowledgeGraphArtifact(
            request.DocumentId,
            request.Title,
            request.Revision,
            nodes.Values.ToArray(),
            edges.Values.ToArray(),
            ranges.Values.ToArray(),
            kbResult.Graph.SerializeJsonLd(),
            kbResult.Graph.SerializeTurtle());
    }

    private static void AddKnowledgeBankGraph(
        KnowledgeGraphSnapshot snapshot,
        string content,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        foreach (var node in snapshot.Nodes)
        {
            nodes.TryAdd(node.Id, new ScriptKnowledgeGraphNode(node.Id, node.Label, node.Kind.ToString(), "knowledge"));
            ScriptKnowledgeGraphSourceRanges.AddRangeIfFound(content, node.Id, node.Label, ranges);
        }

        foreach (var edge in snapshot.Edges)
        {
            var id = $"{edge.SubjectId}|{edge.PredicateId}|{edge.ObjectId}";
            edges.TryAdd(id, new ScriptKnowledgeGraphEdge(id, edge.SubjectId, edge.ObjectId, edge.PredicateLabel));
        }
    }

    private static string CreateSourcePath(string? documentId) =>
        string.IsNullOrWhiteSpace(documentId) ? "script.tps.md" : $"{documentId}.tps.md";
}
