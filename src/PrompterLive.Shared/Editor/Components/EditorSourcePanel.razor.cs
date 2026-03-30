using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using PrompterLive.Core.Models.Editor;
using PrompterLive.Shared.Rendering;

namespace PrompterLive.Shared.Components.Editor;

public partial class EditorSourcePanel
{
    private const string FocusSelectionFailureMessage = "Editor selection focus interop failed during source panel update.";
    private const string RefreshSelectionFailureMessage = "Editor selection refresh interop failed during source panel update.";
    private const string RedoKeyLower = "y";
    private const string ScrollSyncFailureMessage = "Editor overlay scroll sync interop failed during source panel update.";
    private const string UndoKeyLower = "z";
    private ElementReference _overlayRef;
    private ElementReference _textareaRef;

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

    protected MarkupString HighlightMarkup => TpsSourceHighlighter.Render(Text);

    protected string FloatingBarStyle =>
        $"left:clamp({EditorSourcePanelStyleVariables.FloatingBarEdgePaddingExpression}, {Selection.ToolbarLeft}px, calc(100% - {EditorSourcePanelStyleVariables.FloatingBarEdgePaddingExpression})); top:{Selection.ToolbarTop}px; bottom:auto;";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await SafeSyncScrollAsync();
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
        await RefreshSelectionAsync();
    }

    private async Task OnSelectionInteractionAsync()
    {
        CloseToolbarPanels();
        await RefreshSelectionAsync();
    }

    // A late textarea select event can arrive after a toolbar click and should
    // refresh selection state without dismissing the menu the user just opened.
    private Task OnSourceSelectAsync() =>
        RefreshSelectionAsync();

    private async Task OnSourceInputAsync(ChangeEventArgs args)
    {
        CloseToolbarPanels();
        await OnTextChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    }

    private async Task RefreshSelectionAsync()
    {
        var selection = await RunSelectionInteropAsync(
            () => Interop.GetSelectionAsync(_textareaRef),
            RefreshSelectionFailureMessage);

        if (selection is null)
        {
            return;
        }

        await OnSelectionChanged.InvokeAsync(selection);
        StateHasChanged();
    }

    private Task SafeSyncScrollAsync() =>
        RunInteropAsync(
            () => Interop.SyncScrollAsync(_textareaRef, _overlayRef),
            ScrollSyncFailureMessage);

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

    private async Task RunInteropAsync(Func<ValueTask> operation, string failureMessage)
    {
        try
        {
            await operation();
        }
        catch (Exception exception) when (IsExpectedInteropException(exception))
        {
            Logger.LogDebug(exception, failureMessage);
        }
    }

    private static bool IsExpectedInteropException(Exception exception) =>
        exception is InvalidOperationException or JSException or ObjectDisposedException or TaskCanceledException;
}
