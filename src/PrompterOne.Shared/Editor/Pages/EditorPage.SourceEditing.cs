using PrompterOne.Core.AI.Models;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private async Task OnCommandRequestedAsync(EditorCommandRequest request)
    {
        var mutation = request.Kind switch
        {
            EditorCommandKind.Wrap => TextEditor.WrapSelection(
                _sourceText,
                _selection.Range,
                request.PrimaryToken,
                request.SecondaryToken ?? string.Empty,
                request.PlaceholderText),
            EditorCommandKind.ClearColor => TextEditor.ClearColorFormatting(_sourceText, _selection.Range),
            _ => TextEditor.InsertAtSelection(
                _sourceText,
                _selection.Range,
                request.PrimaryToken,
                request.CaretOffset)
        };

        await ApplyMutationAsync(mutation.Text, mutation.Selection);
    }

    private async Task OnNavigateAsync(EditorNavigationTarget target)
    {
        _activeSegmentIndex = target.SegmentIndex;
        _activeBlockIndex = target.BlockIndex;
        _selection = _selection with { Range = new EditorSelectionRange(target.StartIndex, target.StartIndex) };
        await InvokeAsync(StateHasChanged);
        await FocusSourceRangeAsync(target.StartIndex, target.StartIndex);
        RefreshSelectionState();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnHistoryRequestedAsync(EditorHistoryCommand command)
    {
        EditorHistorySnapshot snapshot;
        var hasSnapshot = command == EditorHistoryCommand.Redo
            ? _history.TryRedo(out snapshot)
            : _history.TryUndo(out snapshot);

        if (!hasSnapshot)
        {
            return;
        }

        await ApplyMutationAsync(snapshot.Text, snapshot.Selection);
    }

    private Task OnSelectionChangedAsync(EditorSelectionViewModel selection)
    {
        var previousSelection = _selection;
        var previousSegmentIndex = _activeSegmentIndex;
        var previousBlockIndex = _activeBlockIndex;

        _selection = selection;
        _history.UpdateSelection(selection.Range);
        RefreshSelectionState();
        PublishEditorAiContext();
        if (ShouldRenderSelectionChange(previousSelection, previousSegmentIndex, previousBlockIndex))
        {
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private bool ShouldRenderSelectionChange(
        EditorSelectionViewModel previousSelection,
        int previousSegmentIndex,
        int? previousBlockIndex)
    {
        if (previousSelection.HasSelection || _selection.HasSelection)
        {
            return true;
        }

        if (previousSegmentIndex != _activeSegmentIndex || previousBlockIndex != _activeBlockIndex)
        {
            return true;
        }

        return previousSelection.Line != _selection.Line;
    }

    private Task OnSourceChangedAsync(string text)
    {
        var sourceText = text ?? string.Empty;
        var importedFrontMatter = TryImportFrontMatterFromSource(sourceText, out var bodyText);
        var nextSourceText = importedFrontMatter ? bodyText : sourceText;
        var sourceChanged = !string.Equals(_sourceText, nextSourceText, StringComparison.Ordinal);

        if (sourceChanged)
        {
            Diagnostics.ClearRecoverable(SplitDraftOperation);
        }

        _sourceText = nextSourceText;
        _history.TryRecord(_sourceText, _selection.Range);
        InvalidateScriptGraph();
        _skipNextRenderFromTyping = !importedFrontMatter;
        QueueDraftAnalysis();
        QueueAutosave();
        return Task.CompletedTask;
    }

    private void RefreshSelectionState()
    {
        UpdateActiveOutlineSelection();
        RefreshStructureAuthoringState();
        UpdateStatus();
    }

    private void UpdateActiveOutlineSelection()
    {
        if (_segments.Count == 0)
        {
            _activeSegmentIndex = 0;
            _activeBlockIndex = null;
            return;
        }

        var caretIndex = _selection.Range.OrderedStart;
        var activeSegment = _segments.FirstOrDefault(segment => caretIndex >= segment.StartIndex && caretIndex <= segment.EndIndex)
                            ?? _segments[0];

        _activeSegmentIndex = activeSegment.Index;
        _activeBlockIndex = activeSegment.Blocks
            .FirstOrDefault(block => caretIndex >= block.StartIndex && caretIndex <= block.EndIndex)
            ?.Index;
    }

    private async Task ApplyMutationAsync(string text, EditorSelectionRange selection, string? documentNameOverride = null)
    {
        _selection = _selection with { Range = selection };
        _history.TryRecord(text, selection);
        if (string.IsNullOrWhiteSpace(ScriptId) || !_fileStorageSettings.FileAutoSaveEnabled)
        {
            _scriptGraphArtifact = null;
            StageMutationForAutosave(text, documentNameOverride);
        }
        else
        {
            _scriptGraphArtifact = null;
            PersistDraftInBackground(text, documentNameOverride);
        }

        await InvokeAsync(StateHasChanged);
        if (_workspaceTab != EditorWorkspaceTab.Editor && _sourcePanel is not null)
        {
            await _sourcePanel.SyncExternalTextAsync(_sourceText, selection);
        }

        if (_workspaceTab != EditorWorkspaceTab.Editor)
        {
            await FocusSourceRangeAsync(selection.Start, selection.End, revealSelection: false);
        }

        PublishEditorAiContext();
    }

    private async Task<ScriptDocumentEditResult> ApplyAiEditPlanAsync(ScriptDocumentEditPlan plan)
    {
        var result = ScriptDocumentEditService.Apply(_sourceText, plan);
        var selection = result.AppliedEdits.Count == 0
            ? _selection.Range
            : new EditorSelectionRange(
                result.AppliedEdits[^1].UpdatedRange.Start,
                result.AppliedEdits[^1].UpdatedRange.End);

        await ApplyMutationAsync(result.Text, selection);
        return result;
    }

    private void StageMutationForAutosave(string text, string? documentNameOverride = null)
    {
        CancelDraftAnalysis();
        CancelAutosave();
        PrepareDraftPersistence(text, documentNameOverride);
        QueueDraftAnalysis();
        QueueAutosave();
    }

    private async Task FocusSourceRangeAsync(int start, int end, bool revealSelection = true)
    {
        if (_sourcePanel is not null)
        {
            await _sourcePanel.FocusRangeAsync(start, end, revealSelection);
        }
    }
}
