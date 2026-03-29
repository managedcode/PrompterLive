using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrompterLive.Core.Models.Editor;
using PrompterLive.Shared.Rendering;

namespace PrompterLive.Shared.Components.Editor;

public partial class EditorSourcePanel : IAsyncDisposable
{
    private ElementReference _overlayRef;
    private DotNetObjectReference<EditorSourcePanel>? _shortcutReference;
    private ElementReference _textareaRef;
    private EditorSelectionViewModel _domSelection = EditorSelectionViewModel.Empty;

    [Parameter] public bool CanRedo { get; set; }

    [Parameter] public bool CanUndo { get; set; }

    [Parameter] public string? ErrorMessage { get; set; }

    [Parameter] public EventCallback<EditorCommandRequest> OnCommandRequested { get; set; }

    [Parameter] public EventCallback<EditorAiAssistAction> OnAiActionRequested { get; set; }

    [Parameter] public EventCallback<EditorHistoryCommand> OnHistoryRequested { get; set; }

    [Parameter] public EventCallback<EditorSelectionViewModel> OnSelectionChanged { get; set; }

    [Parameter] public EventCallback<string> OnTextChanged { get; set; }

    [Parameter] public EditorSelectionViewModel Selection { get; set; } = EditorSelectionViewModel.Empty;

    [Parameter] public EditorStatusViewModel Status { get; set; } = new(1, 1, "Actor", 140, 0, 0, 0, "0:00", "1.0");

    [Parameter] public string Text { get; set; } = string.Empty;

    protected MarkupString HighlightMarkup => TpsSourceHighlighter.Render(Text);

    protected string FloatingBarStyle => $"left:{Selection.ToolbarLeft}px; top:{Selection.ToolbarTop}px;";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _shortcutReference = DotNetObjectReference.Create(this);
            await SafeBindHistoryShortcutsAsync();
        }

        await SafeSyncScrollAsync();

        if (Selection.Range != _domSelection.Range)
        {
            try
            {
                _domSelection = await Interop.SetSelectionAsync(_textareaRef, Selection.Range.Start, Selection.Range.End);
            }
            catch
            {
                _domSelection = Selection;
            }
        }
    }

    public async Task FocusRangeAsync(int start, int end)
    {
        try
        {
            _domSelection = await Interop.SetSelectionAsync(_textareaRef, start, end);
            await OnSelectionChanged.InvokeAsync(_domSelection);
            StateHasChanged();
        }
        catch
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        await SafeUnbindHistoryShortcutsAsync();
        _shortcutReference?.Dispose();
    }

    protected Task RequestInsertAsync(string token, int? caretOffset = null) =>
        OnCommandRequested.InvokeAsync(new EditorCommandRequest(EditorCommandKind.Insert, token, CaretOffset: caretOffset));

    protected Task RequestHistoryAsync(EditorHistoryCommand command) =>
        OnHistoryRequested.InvokeAsync(command);

    protected Task RequestWrapAsync(string openingToken, string closingToken, string placeholder = "text") =>
        OnCommandRequested.InvokeAsync(new EditorCommandRequest(EditorCommandKind.Wrap, openingToken, closingToken, placeholder));

    [JSInvokable]
    public Task HandleHistoryShortcut(string command)
    {
        var historyCommand = string.Equals(command, "redo", StringComparison.OrdinalIgnoreCase)
            ? EditorHistoryCommand.Redo
            : EditorHistoryCommand.Undo;

        return RequestHistoryAsync(historyCommand);
    }

    private async Task OnScrollAsync()
    {
        await SafeSyncScrollAsync();
        await RefreshSelectionAsync();
    }

    private async Task OnSelectionInteractionAsync()
    {
        CloseToolbarPanels();
        await RefreshSelectionAsync();
    }

    private async Task OnSourceInputAsync(ChangeEventArgs args)
    {
        CloseToolbarPanels();
        await OnTextChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
        await RefreshSelectionAsync();
    }

    private async Task RefreshSelectionAsync()
    {
        try
        {
            _domSelection = await Interop.GetSelectionAsync(_textareaRef);
            await OnSelectionChanged.InvokeAsync(_domSelection);
            StateHasChanged();
        }
        catch
        {
        }
    }

    private async Task SafeSyncScrollAsync()
    {
        try
        {
            await Interop.SyncScrollAsync(_textareaRef, _overlayRef);
        }
        catch
        {
        }
    }

    private async Task SafeBindHistoryShortcutsAsync()
    {
        if (_shortcutReference is null)
        {
            return;
        }

        try
        {
            await Interop.BindHistoryShortcutsAsync(_textareaRef, _shortcutReference);
        }
        catch
        {
        }
    }

    private async Task SafeUnbindHistoryShortcutsAsync()
    {
        try
        {
            await Interop.UnbindHistoryShortcutsAsync(_textareaRef);
        }
        catch
        {
        }
    }
}
