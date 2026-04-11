using PrompterOne.Core.AI.Models;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private void PublishEditorAiContext()
    {
        var revision = ScriptDocumentRevision.Create(_sourceText);
        var selectedRange = ResolveSelectedScriptRange();
        var context = new ScriptArticleContext(
            Title: _screenTitle,
            Content: _sourceText,
            Source: "PrompterOne Editor",
            Route: Navigation.Uri,
            Screen: AppShellScreen.Editor.ToString(),
            Editor: new ScriptEditorContext(
                DocumentId: SessionService.State.ScriptId,
                DocumentTitle: _screenTitle,
                Content: _sourceText,
                Revision: revision,
                Cursor: ScriptDocumentPosition.FromOffset(_sourceText, _selection.Range.OrderedStart),
                SelectedRange: selectedRange,
                SelectedText: ResolveSelectedText(selectedRange),
                SelectedLineNumbers: ResolveSelectedLineNumbers(selectedRange)),
            Graph: ResolveGraphContext());

        AiSpotlight.SetContext(context);
    }

    private ScriptKnowledgeGraphContext? ResolveGraphContext() =>
        _scriptGraphArtifact is null
            ? null
            : new ScriptKnowledgeGraphContext(
                _scriptGraphArtifact.Revision,
                _scriptGraphArtifact.Nodes.Count,
                _scriptGraphArtifact.Edges.Count,
                _scriptGraphArtifact.Nodes.Take(8).Select(node => node.Label).ToArray());

    private ScriptDocumentRange? ResolveSelectedScriptRange()
    {
        if (!_selection.HasSelection)
        {
            return null;
        }

        var start = Math.Clamp(_selection.Range.OrderedStart, 0, _sourceText.Length);
        var end = Math.Clamp(_selection.Range.OrderedEnd, start, _sourceText.Length);
        return end > start
            ? new ScriptDocumentRange(start, end)
            : null;
    }

    private string? ResolveSelectedText(ScriptDocumentRange? selectedRange) =>
        selectedRange is null
            ? null
            : _sourceText.Substring(selectedRange.Value.Start, selectedRange.Value.Length);

    private IReadOnlyList<int> ResolveSelectedLineNumbers(ScriptDocumentRange? selectedRange)
    {
        if (selectedRange is null)
        {
            return [];
        }

        var start = ScriptDocumentPosition.FromOffset(_sourceText, selectedRange.Value.Start).Line;
        var endOffset = Math.Max(selectedRange.Value.Start, selectedRange.Value.End - 1);
        var end = ScriptDocumentPosition.FromOffset(_sourceText, endOffset).Line;
        return Enumerable.Range(start, end - start + 1).ToArray();
    }
}
