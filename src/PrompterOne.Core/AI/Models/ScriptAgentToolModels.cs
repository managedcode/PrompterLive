namespace PrompterOne.Core.AI.Models;

public sealed record ScriptAgentContextSnapshot(
    string? Screen,
    string? Route,
    string? Title,
    string? DocumentId,
    string? DocumentTitle,
    ScriptDocumentRevision Revision,
    ScriptDocumentRange? SelectedRange,
    IReadOnlyList<int> SelectedLineNumbers,
    int ContentLength,
    ScriptKnowledgeGraphContext? Graph);

public sealed record ScriptAgentRangeReadResult(
    ScriptDocumentRange Range,
    ScriptDocumentPosition Start,
    ScriptDocumentPosition End,
    string Text);

public sealed record ScriptAgentEditPreviewResult(
    string? Reason,
    ScriptDocumentEditPlan Plan);

public sealed record ScriptAgentAppliedEditPreviewResult(
    string? Reason,
    ScriptDocumentEditResult Result);

public sealed record ScriptAgentGraphSummaryResult(
    ScriptDocumentRevision Revision,
    int NodeCount,
    int EdgeCount,
    IReadOnlyList<string> FocusLabels);
