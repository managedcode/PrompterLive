using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Shared.Components.Editor;

public partial class EditorSourcePanel : IAsyncDisposable
{
    private const string FocusSelectionFailureMessage = "Editor selection focus interop failed during source panel update.";
    private const string InitializeSurfaceFailureMessage = "Editor surface interop failed during source panel initialization.";
    private const string RefreshSelectionFailureMessage = "Editor selection refresh interop failed during source panel update.";
    private const string RedoKeyLower = "y";
    private const string SurfaceSyncFailureMessage = "Editor surface sync interop failed during source panel update.";
    private const string UndoKeyLower = "z";
    private EditorMonacoCallbackBridge? _callbackBridge;
    private DotNetObjectReference<EditorMonacoCallbackBridge>? _callbackBridgeReference;
    private ElementReference _editorHostRef;
    private ElementReference _semanticSnapshotRef;
    private ElementReference _textareaRef;
    private bool _hasPendingLocalInputText;
    private bool _hasPendingLocalSelection;
    private EditorSelectionRange _lastLocalSelectionRange = EditorSelectionRange.Empty;
    private bool _lastRenderedCanRedo;
    private bool _lastRenderedCanUndo;
    private string? _lastRenderedErrorMessage;
    private EditorSelectionViewModel _lastRenderedSelection = EditorSelectionViewModel.Empty;
    private string _lastRenderedText = string.Empty;
    private string _lastTypedText = string.Empty;
    private bool _skipNextRender;
    private bool _syncOverlayAfterRender = true;
    private bool _syncSurfaceAfterRender = true;
    private bool _surfaceInteropReady;
    private bool _visibleCanRedo;
    private bool _visibleCanUndo;
    private static readonly object SourceCueContracts = new
    {
        volumeAttributeName = TpsVisualCueContracts.VolumeAttributeName,
        deliveryAttributeName = TpsVisualCueContracts.DeliveryAttributeName,
        speedAttributeName = TpsVisualCueContracts.SpeedAttributeName,
        stressAttributeName = TpsVisualCueContracts.StressAttributeName,
        stressAttributeValue = TpsVisualCueContracts.StressAttributeValue
    };

    [Parameter] public bool CanRedo { get; set; }

    [Parameter] public bool CanUndo { get; set; }

    [Parameter] public bool CanApplyWrapCommands { get; set; } = true;

    [Parameter] public bool CanRenderFloatingToolbar { get; set; } = true;

    [Parameter] public string? ErrorMessage { get; set; }

    [Parameter] public EventCallback<EditorCommandRequest> OnCommandRequested { get; set; }

    [Parameter] public EventCallback<EditorAiAssistAction> OnAiActionRequested { get; set; }

    [Parameter] public EventCallback<EditorDroppedFilesRequest> OnFilesDropped { get; set; }

    [Parameter] public EventCallback<EditorHistoryCommand> OnHistoryRequested { get; set; }

    [Parameter] public EventCallback<EditorSelectionViewModel> OnSelectionChanged { get; set; }

    [Parameter] public EventCallback<string> OnTextChanged { get; set; }

    [Parameter] public EditorSelectionViewModel Selection { get; set; } = EditorSelectionViewModel.Empty;

    [Parameter] public EditorStatusViewModel Status { get; set; } = new(1, 1, "Actor", 140, 0, 0, 0, "0:00", "1.0");

    [Parameter] public string Text { get; set; } = string.Empty;

    [Inject] private ILogger<EditorSourcePanel> Logger { get; set; } = NullLogger<EditorSourcePanel>.Instance;

    protected override void OnParametersSet()
    {
        EnsureToolbarCatalogs();
        var textChanged = !string.Equals(Text, _lastRenderedText, StringComparison.Ordinal);
        var isLocalTextEcho = _hasPendingLocalInputText &&
                              textChanged &&
                              string.Equals(Text, _lastTypedText, StringComparison.Ordinal);
        var isLocalSelectionEcho = _hasPendingLocalSelection && Selection.Range == _lastLocalSelectionRange;
        var selectionChanged = Selection.Range != _lastRenderedSelection.Range;
        var selectionNeedsRender = Selection.HasSelection || _lastRenderedSelection.HasSelection;

        _skipNextRender = _surfaceInteropReady && isLocalTextEcho && !selectionNeedsRender;
        _syncOverlayAfterRender |= textChanged;
        _syncSurfaceAfterRender |= textChanged || (selectionChanged && selectionNeedsRender && !isLocalSelectionEcho);

        if (!isLocalTextEcho)
        {
            _hasPendingLocalInputText = false;
        }

        if (isLocalSelectionEcho || selectionChanged)
        {
            _hasPendingLocalSelection = false;
        }

        _visibleCanUndo = CanUndo;
        _visibleCanRedo = CanRedo;
        RefreshFindMatchesForCurrentText();
        UpdateFloatingBarAnchor();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (await TryLoadInitialAiAvailabilityAfterRenderAsync(firstRender))
        {
            return;
        }

        var surfaceBecameReady = await EnsureSurfaceInteropReadyAsync();
        if (surfaceBecameReady)
        {
            _syncOverlayAfterRender = true;
            _syncSurfaceAfterRender = true;
            StateHasChanged();
            return;
        }

        if (_surfaceInteropReady && (firstRender || _syncSurfaceAfterRender))
        {
            _syncSurfaceAfterRender = false;
            await SafeSyncSurfaceAsync();
        }

        if (_surfaceInteropReady && (firstRender || _syncOverlayAfterRender))
        {
            _syncOverlayAfterRender = false;
            await SafeRenderOverlayAsync();
        }
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

    public async Task FocusRangeAsync(int start, int end, bool revealSelection = true)
    {
        var selection = await RunSelectionInteropAsync(
            () => MonacoInterop.SetSelectionAsync(_editorHostRef, start, end, revealSelection),
            FocusSelectionFailureMessage);

        if (selection is null)
        {
            return;
        }

        TrackLocalSelectionEcho(selection);
        await OnSelectionChanged.InvokeAsync(selection);
        _syncSurfaceAfterRender = false;
        _skipNextRender = false;
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

    private async Task OnSelectionInteractionAsync()
    {
        await RefreshSelectionAsync(dismissMenus: true);
    }

    // A late textarea select event can arrive after a toolbar click and should
    // refresh selection state without dismissing the menu the user just opened.
    private async Task OnSourceSelectAsync()
    {
        await RefreshSelectionAsync(dismissMenus: false);
    }

    private async Task OnSourceInputAsync(ChangeEventArgs args)
    {
        var historyStateChanged = !_visibleCanUndo || _visibleCanRedo;
        CloseToolbarPanels();

        _lastTypedText = args.Value?.ToString() ?? string.Empty;
        _hasPendingLocalInputText = false;
        _visibleCanUndo = true;
        _visibleCanRedo = false;
        _skipNextRender = false;
        await OnTextChanged.InvokeAsync(_lastTypedText);

        if (historyStateChanged)
        {
            StateHasChanged();
        }
    }

    private async Task RefreshSelectionAsync(bool dismissMenus, bool requestComponentRender = true)
    {
        var selection = await RunSelectionInteropAsync(
            () => MonacoInterop.GetSelectionAsync(_editorHostRef),
            RefreshSelectionFailureMessage);

        if (selection is null)
        {
            return;
        }

        TrackLocalSelectionEcho(selection);
        if (dismissMenus)
        {
            RequestFloatingBarReanchor();
            CloseToolbarPanels();
        }
        else if (!_floatingBarAnchor.HasSelection || selection.Range != Selection.Range)
        {
            RequestFloatingBarReanchor();
        }

        await OnSelectionChanged.InvokeAsync(selection);
        if (requestComponentRender)
        {
            _skipNextRender = false;
            _syncSurfaceAfterRender = false;
            StateHasChanged();
        }
    }

    private async Task<bool> EnsureSurfaceInteropReadyAsync()
    {
        if (_surfaceInteropReady)
        {
            return false;
        }

        _callbackBridge ??= new EditorMonacoCallbackBridge(
            HandleMonacoFilesDroppedAsync,
            HandleMonacoHistoryRequestedAsync,
            HandleMonacoTextChangedAsync,
            HandleMonacoSelectionChangedAsync);
        _callbackBridgeReference ??= DotNetObjectReference.Create(_callbackBridge);
        var interopReady = await RunInitializationInteropAsync(
            () => MonacoInterop.InitializeAsync(
                _editorHostRef,
                _textareaRef,
                _semanticSnapshotRef,
                _callbackBridgeReference,
                CreateInitializationOptions()),
            InitializeSurfaceFailureMessage);

        if (!interopReady)
        {
            return false;
        }

        _surfaceInteropReady = true;
        return true;
    }

    private async Task SafeRenderOverlayAsync()
    {
        _ = await RunInteropAsync(
            () => SemanticInterop.RenderOverlayAsync(_semanticSnapshotRef, Text, SourceCueContracts),
            InitializeSurfaceFailureMessage);
    }

    private async Task SafeSyncSurfaceAsync()
    {
        _ = await RunInteropAsync(
            () => MonacoInterop.SyncEditorStateAsync(_editorHostRef, Text, Selection),
            SurfaceSyncFailureMessage);
    }

    private object CreateInitializationOptions() => new
    {
        browserHarnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
        cueContracts = SourceCueContracts,
        darkThemeName = EditorMonacoRuntimeContract.DarkThemeName,
        editorEngineAttributeName = EditorMonacoRuntimeContract.EditorEngineAttributeName,
        editorEngineAttributeValue = EditorMonacoRuntimeContract.EditorEngineAttributeValue,
        filesDroppedCallbackName = EditorMonacoInteropMethodNames.NotifyFilesDropped,
        proxyChangedEventName = EditorMonacoRuntimeContract.EditorProxyChangedEventName,
        editorReadyAttributeName = EditorMonacoRuntimeContract.EditorReadyAttributeName,
        historyRequestedCallbackName = EditorMonacoInteropMethodNames.NotifyHistoryRequested,
        languageId = EditorMonacoRuntimeContract.TpsLanguageId,
        lightThemeName = EditorMonacoRuntimeContract.LightThemeName,
        monacoLoaderPath = EditorMonacoRuntimeContract.MonacoLoaderPath,
        monacoStylesheetPath = EditorMonacoRuntimeContract.MonacoStylesheetPath,
        monacoVsPath = EditorMonacoRuntimeContract.MonacoVsPath,
        placeholder = PlaceholderText,
        selectionChangedCallbackName = EditorMonacoInteropMethodNames.NotifySelectionChanged,
        sourceGutterTestId = UiTestIds.Editor.SourceGutter,
        sourceMinimapTestId = UiTestIds.Editor.SourceMinimap,
        supportedFileNameSuffixes = ScriptDocumentFileTypes.SupportedFileNameSuffixes,
        tpsCatalog = EditorTpsCatalog.Current,
        textChangedCallbackName = EditorMonacoInteropMethodNames.NotifyTextChanged
    };

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

    private void TrackLocalSelectionEcho(EditorSelectionViewModel selection)
    {
        _lastLocalSelectionRange = selection.Range;
        _hasPendingLocalSelection = true;
    }

    private static bool IsExpectedInteropException(Exception exception) =>
        exception is InvalidOperationException or JSException or ObjectDisposedException or TaskCanceledException;

    public async ValueTask DisposeAsync()
    {
        BrowserSettingsChangeNotifier.Changed -= HandleBrowserSettingsChangedAsync;

        if (_surfaceInteropReady)
        {
            try
            {
                await MonacoInterop.DisposeEditorAsync(_editorHostRef);
            }
            catch (Exception exception) when (IsExpectedInteropException(exception))
            {
                Logger.LogDebug(exception, InitializeSurfaceFailureMessage);
            }
        }

        _callbackBridgeReference?.Dispose();
    }
}
