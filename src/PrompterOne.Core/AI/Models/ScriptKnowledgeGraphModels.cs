namespace PrompterOne.Core.AI.Models;

public sealed record ScriptKnowledgeGraphBuildRequest(
    string? DocumentId,
    string? Title,
    string? Content,
    ScriptDocumentRevision Revision);

public sealed record ScriptKnowledgeGraphArtifact(
    string? DocumentId,
    string? Title,
    ScriptDocumentRevision Revision,
    IReadOnlyList<ScriptKnowledgeGraphNode> Nodes,
    IReadOnlyList<ScriptKnowledgeGraphEdge> Edges,
    IReadOnlyList<ScriptKnowledgeGraphSourceRange> SourceRanges,
    string JsonLd,
    string Turtle)
{
    public bool IsEmpty => Nodes.Count == 0 && Edges.Count == 0;
}

public sealed record ScriptKnowledgeGraphNode(
    string Id,
    string Label,
    string Kind,
    string? Group = null);

public sealed record ScriptKnowledgeGraphEdge(
    string Id,
    string SourceId,
    string TargetId,
    string Label);

public sealed record ScriptKnowledgeGraphSourceRange(
    string NodeId,
    ScriptDocumentRange Range,
    ScriptDocumentPosition Start,
    ScriptDocumentPosition End);
