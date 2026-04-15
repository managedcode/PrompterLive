namespace PrompterOne.Core.AI.Models;

public sealed record ScriptKnowledgeGraphBuildRequest(
    string? DocumentId,
    string? Title,
    string? Content,
    ScriptDocumentRevision Revision,
    ScriptKnowledgeGraphSemanticMode SemanticMode = ScriptKnowledgeGraphSemanticMode.ModelOnly);

public sealed record ScriptKnowledgeGraphArtifact(
    string? DocumentId,
    string? Title,
    ScriptDocumentRevision Revision,
    IReadOnlyList<ScriptKnowledgeGraphNode> Nodes,
    IReadOnlyList<ScriptKnowledgeGraphEdge> Edges,
    IReadOnlyList<ScriptKnowledgeGraphSourceRange> SourceRanges,
    string JsonLd,
    string Turtle,
    ScriptKnowledgeGraphSemanticStatus SemanticStatus = ScriptKnowledgeGraphSemanticStatus.StructuralOnly,
    ScriptKnowledgeGraphSemanticMode SemanticMode = ScriptKnowledgeGraphSemanticMode.ModelOnly)
{
    public bool IsEmpty => Nodes.Count == 0 && Edges.Count == 0;
}

public enum ScriptKnowledgeGraphSemanticMode
{
    StructuralOnly,
    ModelOnly,
    TokenizerSimilarity
}

public enum ScriptKnowledgeGraphSemanticStatus
{
    StructuralOnly,
    Model,
    ModelUnavailable,
    ModelFailed,
    TokenizerSimilarity
}

public sealed record ScriptKnowledgeGraphNode(
    string Id,
    string Label,
    string Kind,
    string? Group = null,
    string? Detail = null,
    IReadOnlyDictionary<string, string>? Attributes = null);

public sealed record ScriptKnowledgeGraphEdge(
    string Id,
    string SourceId,
    string TargetId,
    string Label,
    IReadOnlyDictionary<string, string>? Attributes = null);

public sealed record ScriptKnowledgeGraphSourceRange(
    string NodeId,
    ScriptDocumentRange Range,
    ScriptDocumentPosition Start,
    ScriptDocumentPosition End);
