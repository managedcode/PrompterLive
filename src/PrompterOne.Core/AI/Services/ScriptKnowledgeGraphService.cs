using ManagedCode.MarkdownLd.Kb.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PrompterOne.Core.AI.Abstractions;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

public sealed class ScriptKnowledgeGraphService(
    IScriptKnowledgeGraphSemanticExtractor? semanticExtractor = null,
    ScriptKnowledgeGraphTokenizerSimilarityExtractor? tokenizerSimilarityExtractor = null,
    ILogger<ScriptKnowledgeGraphService>? logger = null)
{
    private const string DocumentNodeId = "prompterone:document";
    private const string ContainsEdgeLabel = "contains";
    private readonly IScriptKnowledgeGraphSemanticExtractor? _semanticExtractor = semanticExtractor;
    private readonly ScriptKnowledgeGraphTokenizerSimilarityExtractor _tokenizerSimilarityExtractor = tokenizerSimilarityExtractor ?? new();
    private readonly ILogger<ScriptKnowledgeGraphService> _logger = logger ?? NullLogger<ScriptKnowledgeGraphService>.Instance;

    public async Task<ScriptKnowledgeGraphArtifact> BuildAsync(
        ScriptKnowledgeGraphBuildRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var content = request.Content ?? string.Empty;
        var compiledDocument = ScriptKnowledgeGraphCompiledDocument.Create(content, request.Title);
        var pipeline = new MarkdownKnowledgePipeline();
        var kbResult = await pipeline
            .BuildFromMarkdownAsync(
                compiledDocument.DisplayMarkdown,
                CreateSourcePath(request.DocumentId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var nodes = new Dictionary<string, ScriptKnowledgeGraphNode>(StringComparer.Ordinal);
        var edges = new Dictionary<string, ScriptKnowledgeGraphEdge>(StringComparer.Ordinal);
        var ranges = new Dictionary<string, ScriptKnowledgeGraphSourceRange>(StringComparer.Ordinal);
        var semanticScopes = new List<ScriptKnowledgeGraphSemanticScope>();

        ScriptKnowledgeGraphDocumentBuilder.AddDocumentGraph(
            DocumentNodeId,
            ContainsEdgeLabel,
            request,
            content,
            compiledDocument.DisplayText,
            semanticScopes,
            nodes,
            edges,
            ranges);
        ScriptKnowledgeGraphTpsEnricher.AddTpsGraph(
            DocumentNodeId,
            ContainsEdgeLabel,
            content,
            compiledDocument,
            semanticScopes,
            nodes,
            edges,
            ranges);
        AddKnowledgeBankGraph(kbResult.Graph.ToSnapshot(), content, nodes, edges, ranges);
        var semanticStatus = await TryAddModelSemanticGraphAsync(
                request,
                compiledDocument.DisplayMarkdown,
                semanticScopes,
                nodes,
                edges,
                ranges,
                cancellationToken)
            .ConfigureAwait(false);
        if (semanticStatus != ScriptKnowledgeGraphSemanticStatus.Model &&
            request.SemanticMode == ScriptKnowledgeGraphSemanticMode.TokenizerSimilarity &&
            await _tokenizerSimilarityExtractor
                .AddTokenizerSimilarityAsync(
                    content,
                    compiledDocument.DisplayMarkdown,
                    nodes,
                    edges,
                    ranges,
                    cancellationToken)
                .ConfigureAwait(false))
        {
            semanticStatus = ScriptKnowledgeGraphSemanticStatus.TokenizerSimilarity;
        }

        ScriptKnowledgeGraphRelationshipEnricher.AddRelationships(nodes, edges);

        return new ScriptKnowledgeGraphArtifact(
            request.DocumentId,
            request.Title,
            request.Revision,
            nodes.Values.ToArray(),
            edges.Values.ToArray(),
            ranges.Values.ToArray(),
            kbResult.Graph.SerializeJsonLd(),
            kbResult.Graph.SerializeTurtle(),
            semanticStatus,
            request.SemanticMode);
    }

    private async Task<ScriptKnowledgeGraphSemanticStatus> TryAddModelSemanticGraphAsync(
        ScriptKnowledgeGraphBuildRequest request,
        string content,
        IReadOnlyList<ScriptKnowledgeGraphSemanticScope> semanticScopes,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges,
        CancellationToken cancellationToken)
    {
        if (_semanticExtractor is null)
        {
            return ScriptKnowledgeGraphSemanticStatus.ModelUnavailable;
        }

        try
        {
            var extraction = await _semanticExtractor
                .ExtractAsync(
                    new ScriptKnowledgeGraphSemanticExtractionRequest(
                        request.DocumentId,
                        request.Title,
                        content,
                        request.Revision,
                        semanticScopes),
                    cancellationToken)
                .ConfigureAwait(false);
            if (extraction is null || extraction.IsEmpty)
            {
                return ScriptKnowledgeGraphSemanticStatus.ModelUnavailable;
            }

            ScriptKnowledgeGraphModelSemanticMapper.AddModelExtraction(content, semanticScopes, extraction, nodes, edges, ranges);
            return ScriptKnowledgeGraphSemanticStatus.Model;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Script knowledge graph model extraction failed.");
            return ScriptKnowledgeGraphSemanticStatus.ModelFailed;
        }
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
            if (IsVisualKnowledgeNoise(node))
            {
                continue;
            }

            nodes.TryAdd(node.Id, new ScriptKnowledgeGraphNode(node.Id, node.Label, node.Kind.ToString(), "knowledge"));
            ScriptKnowledgeGraphSourceRanges.AddRangeIfFound(content, node.Id, node.Label, ranges);
        }

        foreach (var edge in snapshot.Edges)
        {
            if (!nodes.ContainsKey(edge.SubjectId) || !nodes.ContainsKey(edge.ObjectId))
            {
                continue;
            }

            var id = $"{edge.SubjectId}|{edge.PredicateId}|{edge.ObjectId}";
            edges.TryAdd(id, new ScriptKnowledgeGraphEdge(id, edge.SubjectId, edge.ObjectId, edge.PredicateLabel));
        }
    }

    private static bool IsVisualKnowledgeNoise(KnowledgeGraphNode node) =>
        IsTpsHeaderLabel(node.Label) || IsSchemaUriNode(node);

    private static bool IsTpsHeaderLabel(string label) =>
        label.StartsWith("[", StringComparison.Ordinal) &&
        label.EndsWith("]", StringComparison.Ordinal) &&
        label.Contains('|', StringComparison.Ordinal);

    private static bool IsSchemaUriNode(KnowledgeGraphNode node) =>
        node.Kind == KnowledgeGraphNodeKind.Uri &&
        node.Id.StartsWith("https://schema.org/", StringComparison.Ordinal);

    private static string CreateSourcePath(string? documentId) =>
        string.IsNullOrWhiteSpace(documentId) ? "script.tps.md" : $"{documentId}.tps.md";
}
