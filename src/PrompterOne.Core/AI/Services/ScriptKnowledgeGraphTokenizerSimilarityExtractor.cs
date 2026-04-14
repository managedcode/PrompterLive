using System.Globalization;
using ManagedCode.MarkdownLd.Kb.Pipeline;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Core.AI.Services;

public sealed class ScriptKnowledgeGraphTokenizerSimilarityExtractor
{
    private const int MaximumLabelCharacters = 84;
    private const int MaximumDetailCharacters = 220;
    private const int MaximumRelatedSegments = 3;
    private const string SourceName = "markdown-ld-kb";
    private const string SimilarityEdgeLabel = "token similarity";
    private const string AboutEdgeLabel = "about";
    private const string MentionsEdgeLabel = "mentions";
    private const string SimilarityKind = "SimilarityChunk";
    private const string TermKind = "Term";
    private const string EntityKind = "Entity";
    private const string SimilarityGroup = "similarity";
    private const string TokenSegmentIdPart = "/token-segment/";
    private const string TokenTopicIdPart = "/token-topic/";
    private const string TokenEntityHintIdPart = "/token-entity-hint/";
    private const string KbRelatedToPredicate = "kb:relatedTo";
    private const string SchemaAboutPredicate = "schema:about";
    private const string SchemaMentionsPredicate = "schema:mentions";

    public async Task<bool> AddTokenizerSimilarityAsync(
        string content,
        string displayMarkdown,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(displayMarkdown))
        {
            return false;
        }

        var pipeline = new MarkdownKnowledgePipeline(
            extractionMode: MarkdownKnowledgeExtractionMode.Tiktoken,
            tiktokenOptions: new TiktokenKnowledgeGraphOptions
            {
                MaxRelatedSegments = MaximumRelatedSegments,
            });
        var result = await pipeline
            .BuildFromMarkdownAsync(displayMarkdown, "script-display.md", cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (result.ExtractionMode != MarkdownKnowledgeExtractionMode.Tiktoken)
        {
            return false;
        }

        var tokenizerNodes = AddTokenizerNodes(result.Facts.Entities, content, nodes, ranges);
        AddTokenizerEdges(result.Facts.Assertions, tokenizerNodes, edges);
        return tokenizerNodes.Values.Count(static node => node.Kind == SimilarityKind) > 1;
    }

    private static IReadOnlyDictionary<string, ScriptKnowledgeGraphNode> AddTokenizerNodes(
        IEnumerable<KnowledgeEntityFact> entities,
        string content,
        IDictionary<string, ScriptKnowledgeGraphNode> nodes,
        IDictionary<string, ScriptKnowledgeGraphSourceRange> ranges)
    {
        var added = new Dictionary<string, ScriptKnowledgeGraphNode>(StringComparer.Ordinal);
        foreach (var entity in entities)
        {
            if (!TryCreateTokenizerNode(entity, out var node))
            {
                continue;
            }

            nodes.TryAdd(node.Id, node);
            added[node.Id] = node;
            if (!ScriptKnowledgeGraphSourceRanges.AddRangeIfFound(content, node.Id, entity.Label, ranges))
            {
                ScriptKnowledgeGraphSourceRanges.AddSequentialTokenRangeIfFound(content, node.Id, entity.Label, ranges);
            }
        }

        return added;
    }

    private static bool TryCreateTokenizerNode(KnowledgeEntityFact entity, out ScriptKnowledgeGraphNode node)
    {
        var kind = ResolveKind(entity.Id);
        if (kind is null)
        {
            node = new ScriptKnowledgeGraphNode(string.Empty, string.Empty, string.Empty);
            return false;
        }

        var label = TrimForLabel(entity.Label);
        node = new ScriptKnowledgeGraphNode(
            entity.Id!,
            label,
            kind,
            SimilarityGroup,
            TrimForDetail(entity.Label),
            CreateNodeAttributes(entity, kind));
        return true;
    }

    private static string? ResolveKind(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (id.Contains(TokenSegmentIdPart, StringComparison.Ordinal))
        {
            return SimilarityKind;
        }

        if (id.Contains(TokenTopicIdPart, StringComparison.Ordinal))
        {
            return TermKind;
        }

        return id.Contains(TokenEntityHintIdPart, StringComparison.Ordinal)
            ? EntityKind
            : null;
    }

    private static void AddTokenizerEdges(
        IEnumerable<KnowledgeAssertionFact> assertions,
        IReadOnlyDictionary<string, ScriptKnowledgeGraphNode> tokenizerNodes,
        IDictionary<string, ScriptKnowledgeGraphEdge> edges)
    {
        foreach (var assertion in assertions)
        {
            if (!tokenizerNodes.ContainsKey(assertion.SubjectId) || !tokenizerNodes.ContainsKey(assertion.ObjectId))
            {
                continue;
            }

            var label = ResolveEdgeLabel(assertion.Predicate);
            if (label is null)
            {
                continue;
            }

            ScriptKnowledgeGraphEdges.Add(
                edges,
                assertion.SubjectId,
                assertion.ObjectId,
                label,
                CreateEdgeAttributes(assertion, label));
        }
    }

    private static string? ResolveEdgeLabel(string predicate)
    {
        return predicate switch
        {
            KbRelatedToPredicate => SimilarityEdgeLabel,
            SchemaAboutPredicate => AboutEdgeLabel,
            SchemaMentionsPredicate => MentionsEdgeLabel,
            _ => null,
        };
    }

    private static IReadOnlyDictionary<string, string> CreateNodeAttributes(
        KnowledgeEntityFact entity,
        string kind)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = SourceName,
            ["category"] = ResolveCategory(kind),
        };
        AddAttribute(attributes, "confidence", entity.Confidence.ToString("0.###", CultureInfo.InvariantCulture));
        AddAttribute(attributes, "sourceDocument", entity.Source);
        AddAttribute(attributes, "entityType", entity.Type);
        return attributes;
    }

    private static string ResolveCategory(string kind) =>
        kind switch
        {
            SimilarityKind => "token-segment",
            TermKind => "token-topic",
            EntityKind => "token-entity",
            _ => "token",
        };

    private static IReadOnlyDictionary<string, string> CreateEdgeAttributes(
        KnowledgeAssertionFact assertion,
        string label)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = SourceName,
        };
        AddAttribute(attributes, "confidence", assertion.Confidence.ToString("0.###", CultureInfo.InvariantCulture));
        return attributes;
    }

    private static void AddAttribute(IDictionary<string, string> attributes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            attributes[key] = value.Trim();
        }
    }

    private static string TrimForLabel(string text)
    {
        var normalized = NormalizeWhitespace(text);
        return normalized.Length <= MaximumLabelCharacters
            ? normalized
            : string.Concat(normalized.AsSpan(0, MaximumLabelCharacters - 3).Trim(), "...");
    }

    private static string TrimForDetail(string text)
    {
        var normalized = NormalizeWhitespace(text);
        return normalized.Length <= MaximumDetailCharacters
            ? normalized
            : string.Concat(normalized.AsSpan(0, MaximumDetailCharacters - 3).Trim(), "...");
    }

    private static string NormalizeWhitespace(string? text) =>
        string.Join(' ', (text ?? string.Empty).Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
}
