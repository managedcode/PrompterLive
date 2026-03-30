using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using PrompterLive.Core.Models.Editor;

namespace PrompterLive.Shared.Components.Editor;

public partial class EditorSourcePanel
{
    private const string FocusSelectionFailureMessage = "Editor selection focus interop failed during source panel update.";
    private const string InitializeSurfaceFailureMessage = "Editor surface interop failed during source panel initialization.";
    private const string RefreshSelectionFailureMessage = "Editor selection refresh interop failed during source panel update.";
    private const string RedoKeyLower = "y";
    private const string ScrollSyncFailureMessage = "Editor overlay scroll sync interop failed during source panel update.";
    private const string UndoKeyLower = "z";
    private ElementReference _overlayRef;
    private ElementReference _textareaRef;
    private bool _hasPendingLocalInputText;
    private bool _lastRenderedCanRedo;
    private bool _lastRenderedCanUndo;
    private string? _lastRenderedErrorMessage;
    private EditorSelectionViewModel _lastRenderedSelection = EditorSelectionViewModel.Empty;
    private string _lastRenderedText = string.Empty;
    private string _lastTypedText = string.Empty;
    private bool _skipNextRender;
    private bool _syncOverlayAfterRender = true;
    private bool _surfaceInteropReady;
    private bool _syncScrollAfterRender = true;

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

    [Inject] private ILogger<EditorSourcePanel> Logger { get; set; } = NullLogger<EditorSourcePanel>.Instance;

    protected override void OnParametersSet()
    {
        var textChanged = !string.Equals(Text, _lastRenderedText, StringComparison.Ordinal);
        var isLocalTextEcho = _hasPendingLocalInputText &&
                              textChanged &&
                              string.Equals(Text, _lastTypedText, StringComparison.Ordinal);
        var selectionNeedsRender = Selection.HasSelection || _lastRenderedSelection.HasSelection;

        _skipNextRender = _surfaceInteropReady && isLocalTextEcho && !selectionNeedsRender;
        _syncOverlayAfterRender |= textChanged;
        _syncScrollAfterRender |= textChanged;

        if (!isLocalTextEcho)
        {
            _hasPendingLocalInputText = false;
        }

        UpdateFloatingBarAnchor();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await EnsureSurfaceInteropReadyAsync();

        if (_surfaceInteropReady && (firstRender || _syncOverlayAfterRender))
        {
            _syncOverlayAfterRender = false;
            await SafeRenderOverlayAsync();
        }

        if (!firstRender && !_syncScrollAfterRender)
        {
            return;
        }

        _syncScrollAfterRender = false;
        await SafeSyncScrollAsync();
    }

    protected override bool ShouldRender()
    {
        if (_skipNextRender)
        {
            _skipNextRender = false;
            return false;
        }

        _lastRenderedText = Text;
        _lastRenderedSelection = Selection;
        _lastRenderedCanUndo = CanUndo;
        _lastRenderedCanRedo = CanRedo;
        _lastRenderedErrorMessage = ErrorMessage;
        return true;
    }

    public async Task FocusRangeAsync(int start, int end)
    {
        var selection = await RunSelectionInteropAsync(
            () => Interop.SetSelectionAsync(_textareaRef, start, end),
            FocusSelectionFailureMessage);

        if (selection is null)
        {
            return;
        }

        await OnSelectionChanged.InvokeAsync(selection);
        _syncScrollAfterRender = true;
        StateHasChanged();
    }

    protected Task RequestInsertAsync(string token, int? caretOffset = null) =>
        OnCommandRequested.InvokeAsync(new EditorCommandRequest(EditorCommandKind.Insert, token, CaretOffset: caretOffset));

    protected Task RequestHistoryAsync(EditorHistoryCommand command) =>
        OnHistoryRequested.InvokeAsync(command);

    protected Task RequestWrapAsync(string openingToken, string closingToken, string placeholder = "text") =>
        OnCommandRequested.InvokeAsync(new EditorCommandRequest(EditorCommandKind.Wrap, openingToken, closingToken, placeholder));

    private Task OnSourceKeyDownAsync(KeyboardEventArgs args)
    {
        var key = (args.Key ?? string.Empty).ToLowerInvariant();
        var hasModifier = args.CtrlKey || args.MetaKey;
        var isUndo = hasModifier && !args.ShiftKey && key == UndoKeyLower;
        var isRedo = hasModifier && (key == RedoKeyLower || (args.ShiftKey && key == UndoKeyLower));

        if (!isUndo && !isRedo)
        {
            return Task.CompletedTask;
        }

        return RequestHistoryAsync(isRedo ? EditorHistoryCommand.Redo : EditorHistoryCommand.Undo);
    }

    private async Task OnScrollAsync()
    {
        await SafeSyncScrollAsync();
        if (Selection.HasSelection)
        {
            await RefreshSelectionAsync();
        }
    }

    private async Task OnSelectionInteractionAsync()
    {
        RequestFloatingBarReanchor();
        CloseToolbarPanels();
        await RefreshSelectionAsync();
    }

    // A late textarea select event can arrive after a toolbar click and should
    // refresh selection state without dismissing the menu the user just opened.
    private Task OnSourceSelectAsync()
    {
        RequestFloatingBarReanchor();
        return RefreshSelectionAsync();
    }

    private async Task OnSourceInputAsync(ChangeEventArgs args)
    {
        var hadOpenToolbarMenu = HasOpenToolbarMenu;
        CloseToolbarPanels();

        _lastTypedText = args.Value?.ToString() ?? string.Empty;
        _hasPendingLocalInputText = true;
        _skipNextRender = _surfaceInteropReady && !hadOpenToolbarMenu;
        await OnTextChanged.InvokeAsync(_lastTypedText);
    }

    private async Task RefreshSelectionAsync(bool requestComponentRender = true)
    {
        var selection = await RunSelectionInteropAsync(
            () => Interop.GetSelectionAsync(_textareaRef),
            RefreshSelectionFailureMessage);

        if (selection is null)
        {
            return;
        }

        await OnSelectionChanged.InvokeAsync(selection);
        if (requestComponentRender)
        {
            _syncScrollAfterRender = Selection.HasSelection || selection.HasSelection;
            StateHasChanged();
        }
    }

    private async Task EnsureSurfaceInteropReadyAsync()
    {
        if (_surfaceInteropReady)
        {
            return;
        }

        _surfaceInteropReady = await RunInitializationInteropAsync(
            () => Interop.InitializeAsync(_textareaRef, _overlayRef),
            InitializeSurfaceFailureMessage);
    }

    private async Task SafeSyncScrollAsync()
    {
        _ = await RunInteropAsync(
            () => Interop.SyncScrollAsync(_textareaRef, _overlayRef),
            ScrollSyncFailureMessage);
    }

    private async Task SafeRenderOverlayAsync()
    {
        _ = await RunInteropAsync(
            () => Interop.RenderOverlayAsync(_overlayRef, Text),
            InitializeSurfaceFailureMessage);
    }

    private async Task<bool> RunInitializationInteropAsync(Func<ValueTask<bool>> operation, string failureMessage)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (IsExpectedInteropException(exception))
        {
            Logger.LogDebug(exception, failureMessage);
            return false;
        }
    }

    private async Task<EditorSelectionViewModel?> RunSelectionInteropAsync(
        Func<Task<EditorSelectionViewModel>> operation,
        string failureMessage)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (IsExpectedInteropException(exception))
        {
            Logger.LogDebug(exception, failureMessage);
            return null;
        }
    }

    private async Task<bool> RunInteropAsync(Func<ValueTask> operation, string failureMessage)
    {
        try
        {
            await operation();
            return true;
        }
        catch (Exception exception) when (IsExpectedInteropException(exception))
        {
            Logger.LogDebug(exception, failureMessage);
            return false;
        }
    }

    private static bool IsExpectedInteropException(Exception exception) =>
        exception is InvalidOperationException or JSException or ObjectDisposedException or TaskCanceledException;
}
