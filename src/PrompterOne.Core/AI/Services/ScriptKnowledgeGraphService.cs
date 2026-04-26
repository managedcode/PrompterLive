using System.Globalization;
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
    private const string MarkdownKnowledgeSource = "markdown-ld-kb";
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
                compiledDocument.KnowledgeMarkdown,
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
        AddKnowledgeBankGraph(kbResult, content, nodes, edges, ranges);
        var semanticStatus = request.SemanticMode == ScriptKnowledgeGraphSemanticMode.StructuralOnly
            ? ScriptKnowledgeGraphSemanticStatus.StructuralOnly
            : await TryAddModelSemanticGraphAsync(
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
                    compiledDocument.KnowledgeMarkdown,
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
        MarkdownKnowledgeBuildResult result,
        string content,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var snapshot = result.Graph.ToSnapshot();
        var factsById = result.Facts.Entities
            .Where(static entity => !string.IsNullOrWhiteSpace(entity.Id))
            .GroupBy(static entity => entity.Id!, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
        foreach (var node in snapshot.Nodes)
        {
            if (IsVisualKnowledgeNoise(node))
            {
                continue;
            }

            factsById.TryGetValue(node.Id, out var fact);
            var kind = ResolveKnowledgeKind(node, fact);
            var label = string.IsNullOrWhiteSpace(fact?.Label) ? node.Label : fact!.Label;
            nodes.TryAdd(
                node.Id,
                new ScriptKnowledgeGraphNode(
                    node.Id,
                    label,
                    kind,
                    "knowledge",
                    CreateKnowledgeDetail(node, fact),
                    CreateKnowledgeAttributes(node, fact)));
            ScriptKnowledgeGraphSourceRanges.AddRangeIfFound(content, node.Id, label, ranges);
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

    private static string ResolveKnowledgeKind(KnowledgeGraphNode node, KnowledgeEntityFact? fact)
    {
        var type = fact?.Type ?? string.Empty;
        if (ContainsType(type, "Person"))
        {
            return "Character";
        }

        if (ContainsType(type, "DefinedTerm"))
        {
            return "Term";
        }

        if (ContainsType(type, "Claim"))
        {
            return "Claim";
        }

        if (ContainsType(type, "CreativeWork") ||
            ContainsType(type, "Article") ||
            ContainsType(type, "TextDigitalDocument"))
        {
            return "Story";
        }

        return node.Kind switch
        {
            KnowledgeGraphNodeKind.Literal => "Literal",
            KnowledgeGraphNodeKind.Blank => "Custom",
            _ => "Entity",
        };
    }

    private static bool ContainsType(string type, string value) =>
        type.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static string? CreateKnowledgeDetail(KnowledgeGraphNode node, KnowledgeEntityFact? fact)
    {
        var detailParts = new List<string>();
        AddDetail(detailParts, "type", fact?.Type);
        AddDetail(detailParts, "source", fact?.Source);
        if (node.Kind != KnowledgeGraphNodeKind.Uri)
        {
            AddDetail(detailParts, "rdf", node.Kind.ToString());
        }

        return detailParts.Count == 0 ? null : string.Join(" | ", detailParts);
    }

    private static IReadOnlyDictionary<string, string> CreateKnowledgeAttributes(
        KnowledgeGraphNode node,
        KnowledgeEntityFact? fact)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = MarkdownKnowledgeSource,
            ["rdfKind"] = node.Kind.ToString(),
        };
        AddAttribute(attributes, "entityType", fact?.Type);
        AddAttribute(attributes, "sourceDocument", fact?.Source);
        if (fact is not null)
        {
            AddAttribute(
                attributes,
                "confidence",
                fact.Confidence.ToString("0.###", CultureInfo.InvariantCulture));
            if (fact.SameAs.Count > 0)
            {
                AddAttribute(attributes, "sameAs", string.Join(", ", fact.SameAs));
            }
        }

        return attributes;
    }

    private static void AddDetail(ICollection<string> details, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            details.Add($"{label}: {value.Trim()}");
        }
    }

    private static void AddAttribute(IDictionary<string, string> attributes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            attributes[key] = value.Trim();
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
