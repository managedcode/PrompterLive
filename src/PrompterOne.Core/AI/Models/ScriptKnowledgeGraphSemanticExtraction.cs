namespace PrompterOne.Core.AI.Models;

public sealed record ScriptKnowledgeGraphSemanticExtractionRequest(
    string? DocumentId,
    string? Title,
    string Content,
    ScriptDocumentRevision Revision,
    IReadOnlyList<ScriptKnowledgeGraphSemanticScope> Scopes);

public sealed record ScriptKnowledgeGraphSemanticScope(
    string NodeId,
    string Label,
    string Content);

public sealed record ScriptKnowledgeGraphSemanticExtraction(
    IReadOnlyList<ScriptKnowledgeGraphSemanticNode> Nodes,
    IReadOnlyList<ScriptKnowledgeGraphSemanticLink> Links)
{
    public bool IsEmpty => Nodes.Count == 0 && Links.Count == 0;
}

public sealed record ScriptKnowledgeGraphSemanticNode(
    string Kind,
    string Label,
    string? Detail = null,
    string? ScopeLabel = null,
    string? SourceQuote = null,
    double? Confidence = null);

public sealed record ScriptKnowledgeGraphSemanticLink(
    string SourceLabel,
    string TargetLabel,
    string Label);
