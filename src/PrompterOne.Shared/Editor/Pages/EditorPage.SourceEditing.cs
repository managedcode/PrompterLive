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
        _selection = selection;
        _history.UpdateSelection(selection.Range);
        RefreshSelectionState();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnSourceChangedAsync(string text)
    {
        var sourceText = text ?? string.Empty;
        var importedFrontMatter = TryImportFrontMatterFromSource(sourceText, out var bodyText);
        _sourceText = importedFrontMatter ? bodyText : sourceText;
        _history.TryRecord(_sourceText, _selection.Range);
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

    private async Task ApplyMutationAsync(string text, EditorSelectionRange selection)
    {
        _selection = _selection with { Range = selection };
        _history.TryRecord(text, selection);
        PersistDraftInBackground(text);
        await FocusSourceRangeAsync(selection.Start, selection.End);
    }

    private async Task FocusSourceRangeAsync(int start, int end)
    {
        if (_sourcePanel is not null)
        {
            await _sourcePanel.FocusRangeAsync(start, end);
        }
    }
}
