namespace PrompterOne.Core.AI.Models;

public sealed record ScriptArticleContext(
    string? Title = null,
    string? Summary = null,
    string? Content = null,
    string? Source = null,
    string? Route = null,
    string? Screen = null,
    ScriptEditorContext? Editor = null,
    ScriptKnowledgeGraphContext? Graph = null)
{
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Title)
        && string.IsNullOrWhiteSpace(Summary)
        && string.IsNullOrWhiteSpace(Content)
        && string.IsNullOrWhiteSpace(Source)
        && string.IsNullOrWhiteSpace(Route)
        && string.IsNullOrWhiteSpace(Screen)
        && (Editor is null || Editor.IsEmpty)
        && (Graph is null || Graph.IsEmpty);
}

public sealed record ScriptEditorContext(
    string? DocumentId = null,
    string? DocumentTitle = null,
    string? Content = null,
    ScriptDocumentRevision? Revision = null,
    ScriptDocumentPosition? Cursor = null,
    ScriptDocumentRange? SelectedRange = null,
    string? SelectedText = null,
    IReadOnlyList<int>? SelectedLineNumbers = null)
{
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(DocumentId)
        && string.IsNullOrWhiteSpace(DocumentTitle)
        && string.IsNullOrWhiteSpace(Content)
        && Revision is null
        && Cursor is null
        && SelectedRange is null
        && string.IsNullOrWhiteSpace(SelectedText)
        && (SelectedLineNumbers is null || SelectedLineNumbers.Count == 0);
}

public sealed record ScriptKnowledgeGraphContext(
    ScriptDocumentRevision? Revision = null,
    int NodeCount = 0,
    int EdgeCount = 0,
    IReadOnlyList<string>? FocusLabels = null)
{
    public bool IsEmpty =>
        Revision is null
        && NodeCount == 0
        && EdgeCount == 0
        && (FocusLabels is null || FocusLabels.Count == 0);
}
