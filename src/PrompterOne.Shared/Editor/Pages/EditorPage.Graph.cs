using PrompterOne.Core.AI.Models;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Components.Editor;
using Microsoft.AspNetCore.Components.Web;

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
            _metadataRailSelectedTab = EditorMetadataRailTab.Graph;
            _isMetadataRailCollapsed = true;
            await RebuildScriptGraphAsync();
        }
        else if (_metadataRailSelectedTab == EditorMetadataRailTab.Graph)
        {
            _metadataRailSelectedTab = EditorMetadataRailTab.Metadata;
        }
    }

    private async Task RebuildScriptGraphAsync(bool force = false)
    {
        var revision = ScriptDocumentRevision.Create(_sourceText);
        if (!force && _scriptGraphArtifact?.Revision == revision)
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
                revision,
                _graphSemanticMode));

        _isGraphLoading = false;
        PublishEditorAiContext();
        await InvokeAsync(StateHasChanged);
    }

    private Task OnGraphAnalyzeRequestedAsync()
    {
        _graphSemanticMode = ScriptKnowledgeGraphSemanticMode.ModelOnly;
        return RebuildScriptGraphAsync(force: true);
    }

    private Task OnGraphTokenizerAnalysisRequestedAsync()
    {
        _graphSemanticMode = ScriptKnowledgeGraphSemanticMode.TokenizerSimilarity;
        return RebuildScriptGraphAsync(force: true);
    }

    private void InvalidateScriptGraph()
    {
        _scriptGraphArtifact = null;
        PublishEditorAiContext();
    }

    private async Task OnGraphSourceRangeRequestedAsync(ScriptKnowledgeGraphSourceRange sourceRange)
    {
        if (_workspaceTab != EditorWorkspaceTab.Graph)
        {
            _workspaceTab = EditorWorkspaceTab.Graph;
        }

        _graphOnlyMode = false;

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

    private Task OnGraphOnlyModeChanged(bool graphOnlyMode)
    {
        _graphOnlyMode = graphOnlyMode;
        return Task.CompletedTask;
    }

    private async Task OnGraphSplitPointerDownAsync(PointerEventArgs args)
    {
        _graphSplitBounds = await GraphViewerInterop.MeasureAsync(_graphSplitRef);
        _isGraphSplitDragging = _graphSplitBounds is { Width: > 0 };
        if (_isGraphSplitDragging)
        {
            _isGraphSplitVertical = _graphSplitBounds!.Height > _graphSplitBounds.Width;
            UpdateGraphSourcePanePercent(args.ClientX, args.ClientY);
        }
    }

    private Task OnGraphSplitPointerMoveAsync(PointerEventArgs args)
    {
        if (_isGraphSplitDragging)
        {
            UpdateGraphSourcePanePercent(args.ClientX, args.ClientY);
        }

        return Task.CompletedTask;
    }

    private Task OnGraphSplitPointerUpAsync()
    {
        _isGraphSplitDragging = false;
        _isGraphSplitVertical = false;
        _graphSplitBounds = null;
        return Task.CompletedTask;
    }

    private void UpdateGraphSourcePanePercent(double clientX, double clientY)
    {
        if (_graphSplitBounds is not { Width: > 0 } bounds)
        {
            return;
        }

        var position = _isGraphSplitVertical ? clientY - bounds.Top : clientX - bounds.Left;
        var length = _isGraphSplitVertical ? bounds.Height : bounds.Width;
        if (length <= 0)
        {
            return;
        }

        var percent = (position / length) * 100;
        _graphSourcePanePercent = Math.Clamp(
            percent,
            GraphSourcePaneMinimumPercent,
            GraphSourcePaneMaximumPercent);
    }

    private string GetWorkspaceTabCss(EditorWorkspaceTab tab) =>
        _workspaceTab == tab
            ? "editor-workspace-tab active"
            : "editor-workspace-tab";

    private string GetEditorLayoutCss() =>
        _workspaceTab == EditorWorkspaceTab.Graph
            ? _graphOnlyMode
                ? "ed-layout ed-layout--graph ed-layout--graph-only"
                : "ed-layout ed-layout--graph"
            : "ed-layout";

    private bool ShouldShowGraphOnlyWorkspaceChrome =>
        _workspaceTab == EditorWorkspaceTab.Graph && _graphOnlyMode;

    private string GetGraphSplitCss() =>
        _graphOnlyMode
            ? "editor-graph-split editor-graph-split--graph-only"
            : _isGraphSplitDragging
                ? "editor-graph-split editor-graph-split--resizing"
                : "editor-graph-split";

    private string GetGraphSplitStyle() =>
        _graphOnlyMode
            ? string.Empty
            : $"--editor-graph-source-pane:{_graphSourcePanePercent.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}%;";
}
