using PrompterOne.Core.AI.Models;
using PrompterOne.Core.Models.Editor;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private async Task OnWorkspaceTabChangedAsync(EditorWorkspaceTab tab)
    {
        if (_workspaceTab == tab)
        {
            return;
        }

        _workspaceTab = tab;
        if (tab == EditorWorkspaceTab.Graph)
        {
            await RebuildScriptGraphAsync();
        }
    }

    private async Task RebuildScriptGraphAsync()
    {
        var revision = ScriptDocumentRevision.Create(_sourceText);
        if (_scriptGraphArtifact?.Revision == revision)
        {
            PublishEditorAiContext();
            return;
        }

        _isGraphLoading = true;
        await InvokeAsync(StateHasChanged);

        _scriptGraphArtifact = await ScriptKnowledgeGraphService.BuildAsync(
            new ScriptKnowledgeGraphBuildRequest(
                SessionService.State.ScriptId,
                _screenTitle,
                _sourceText,
                revision));

        _isGraphLoading = false;
        PublishEditorAiContext();
        await InvokeAsync(StateHasChanged);
    }

    private void InvalidateScriptGraph()
    {
        _scriptGraphArtifact = null;
        PublishEditorAiContext();
    }

    private async Task OnGraphSourceRangeRequestedAsync(ScriptKnowledgeGraphSourceRange sourceRange)
    {
        _workspaceTab = EditorWorkspaceTab.Source;
        _selection = _selection with
        {
            Range = new EditorSelectionRange(sourceRange.Range.Start, sourceRange.Range.End)
        };

        await InvokeAsync(StateHasChanged);
        await Task.Yield();
        await FocusSourceRangeAsync(sourceRange.Range.Start, sourceRange.Range.End);
        RefreshSelectionState();
        PublishEditorAiContext();
        await InvokeAsync(StateHasChanged);
    }

    private string GetWorkspaceTabCss(EditorWorkspaceTab tab) =>
        _workspaceTab == tab
            ? "editor-workspace-tab active"
            : "editor-workspace-tab";
}
