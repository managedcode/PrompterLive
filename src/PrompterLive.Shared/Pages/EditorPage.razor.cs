using Microsoft.AspNetCore.Components;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Editor;
using PrompterLive.Core.Models.Workspace;
using PrompterLive.Core.Services.Editor;
using PrompterLive.Core.Services.Samples;
using PrompterLive.Shared.Components.Editor;
using PrompterLive.Shared.Services;
using PrompterLive.Shared.Services.Diagnostics;
using PrompterLive.Shared.Services.Editor;

namespace PrompterLive.Shared.Pages;

public partial class EditorPage
{
    private const string LoadEditorOperation = "Editor load";
    private const string LoadEditorMessage = "Unable to load the editor right now.";
    private const string PersistDraftOperation = "Editor save draft";
    private const string PersistDraftMessage = "Unable to save the current draft.";
    private const string EditorSyntaxOperation = "Editor syntax";
    private const string EditorSyntaxMessage = "The TPS draft has a syntax issue. Fix it and keep writing.";
    private const int AutosaveDelayMilliseconds = 450;

    private readonly EditorDocumentHistory _history = new();
    private readonly TpsFrontMatterDocumentService _frontMatterService = new();
    private CancellationTokenSource? _autosaveCancellationSource;
    private bool _loadState = true;
    private int? _activeBlockIndex;
    private int _activeSegmentIndex;
    private string _author = "PrompterLive";
    private int _baseWpm = 140;
    private string _createdDate = DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    private string? _errorMessage;
    private string _profile = "Actor";
    private string _screenTitle = "Product Launch";
    private EditorSelectionViewModel _selection = EditorSelectionViewModel.Empty;
    private IReadOnlyList<EditorOutlineSegmentViewModel> _segments = [];
    private EditorSourcePanel? _sourcePanel;
    private string _sourceText = string.Empty;
    private EditorStatusViewModel _status = new(1, 1, "Actor", 140, 0, 0, 0, "0:00", "1.0");
    private string _version = "1.0";

    [Inject] private AppBootstrapper Bootstrapper { get; set; } = null!;
    [Inject] private UiDiagnosticsService Diagnostics { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private EditorOutlineBuilder OutlineBuilder { get; set; } = null!;
    [Inject] private EditorLocalAssistant LocalAssistant { get; set; } = null!;
    [Inject] private IScriptRepository ScriptRepository { get; set; } = null!;
    [Inject] private IScriptSessionService SessionService { get; set; } = null!;
    [Inject] private TpsStructureEditor StructureEditor { get; set; } = null!;
    [Inject] private TpsTextEditor TextEditor { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "id")]
    public string? ScriptId { get; set; }

    protected override Task OnParametersSetAsync()
    {
        _loadState = true;
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_loadState)
        {
            _loadState = false;
            await Diagnostics.RunAsync(
                LoadEditorOperation,
                LoadEditorMessage,
                async () =>
                {
                    await Bootstrapper.EnsureReadyAsync();
                    await EnsureSessionLoadedAsync();
                    PopulateEditorState(resetHistory: true);
                    StateHasChanged();
                });
        }
    }

    private async Task EnsureSessionLoadedAsync()
    {
        if (!string.IsNullOrWhiteSpace(ScriptId))
        {
            var document = await ScriptRepository.GetAsync(ScriptId);
            if (document is not null &&
                !string.Equals(SessionService.State.ScriptId, document.Id, StringComparison.Ordinal))
            {
                await SessionService.OpenAsync(document);
            }

            if (document is not null)
            {
                _createdDate = document.UpdatedAt.LocalDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            await SessionService.LoadSampleAsync(SampleScriptCatalog.DemoSampleId);
        }
    }

    private async Task OnAuthorChangedAsync(string value)
    {
        _author = value.Trim();
        await PersistMetadataAsync();
    }

    private async Task OnBaseWpmChangedAsync(int value)
    {
        _baseWpm = Math.Clamp(value, 80, 600);
        await PersistMetadataAsync();
    }

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
            EditorCommandKind.ClearColor => TextEditor.ClearColorFormatting(
                _sourceText,
                _selection.Range),
            _ => TextEditor.InsertAtSelection(
                _sourceText,
                _selection.Range,
                request.PrimaryToken,
                request.CaretOffset)
        };

        _selection = _selection with { Range = mutation.Selection };
        _history.TryRecord(mutation.Text, mutation.Selection);
        await PersistDraftAsync(mutation.Text);

        if (_sourcePanel is not null)
        {
            await _sourcePanel.FocusRangeAsync(mutation.Selection.Start, mutation.Selection.End);
        }
    }

    private async Task OnCreatedDateChangedAsync(string value)
    {
        _createdDate = value.Trim();
        await PersistMetadataAsync();
    }

    private async Task OnNavigateAsync(EditorNavigationTarget target)
    {
        _activeSegmentIndex = target.SegmentIndex;
        _activeBlockIndex = target.BlockIndex;
        _selection = _selection with { Range = new EditorSelectionRange(target.StartIndex, target.StartIndex) };
        await InvokeAsync(StateHasChanged);

        if (_sourcePanel is not null)
        {
            await _sourcePanel.FocusRangeAsync(target.StartIndex, target.StartIndex);
        }

        UpdateActiveOutlineSelection();
        RefreshStructureAuthoringState();
        UpdateStatus(SessionService.State);
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

        _selection = _selection with { Range = snapshot.Selection };
        await PersistDraftAsync(snapshot.Text);

        if (_sourcePanel is not null)
        {
            await _sourcePanel.FocusRangeAsync(snapshot.Selection.Start, snapshot.Selection.End);
        }
    }

    private async Task OnProfileChangedAsync(string value)
    {
        _profile = string.Equals(value, "RSVP", StringComparison.Ordinal) ? "RSVP" : "Actor";
        await PersistMetadataAsync();
    }

    private Task OnSelectionChangedAsync(EditorSelectionViewModel selection)
    {
        _selection = selection;
        _history.UpdateSelection(selection.Range);
        UpdateActiveOutlineSelection();
        RefreshStructureAuthoringState();
        UpdateStatus(SessionService.State);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task OnSourceChangedAsync(string text)
    {
        _sourceText = text ?? string.Empty;
        _history.TryRecord(_sourceText, _selection.Range);
        await UpdateDraftStateAsync(_sourceText, persistDocument: false);
        QueueAutosave();
    }

    private async Task OnVersionChangedAsync(string value)
    {
        _version = value.Trim();
        await PersistMetadataAsync();
    }

    private void PopulateEditorState(bool resetHistory = false)
    {
        var state = SessionService.State;
        var document = _frontMatterService.Parse(state.Text);
        var metadata = document.Metadata;

        _sourceText = document.Body;
        _screenTitle = _frontMatterService.ResolveTitle(state.Text, state.Title);
        _author = GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Author, "PrompterLive");
        _baseWpm = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.BaseWpm, state.ScriptData?.TargetWpm ?? 140);
        _profile = GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Profile, _baseWpm >= 250 ? "RSVP" : "Actor");
        _version = GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Version, "1.0");
        _createdDate = GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Created, _createdDate);
        _xslowOffset = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.XslowOffset, DefaultXslowOffset);
        _slowOffset = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.SlowOffset, DefaultSlowOffset);
        _fastOffset = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.FastOffset, DefaultFastOffset);
        _xfastOffset = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.XfastOffset, DefaultXfastOffset);
        _segments = OutlineBuilder.Build(state.ScriptData, document.Body, 0);
        _errorMessage = state.ErrorMessage;
        if (string.IsNullOrWhiteSpace(_errorMessage))
        {
            Diagnostics.ClearRecoverable(EditorSyntaxOperation);
        }
        else
        {
            Diagnostics.ReportRecoverable(EditorSyntaxOperation, EditorSyntaxMessage, _errorMessage);
        }

        if (resetHistory || !_history.IsInitialized)
        {
            _history.Reset(_sourceText, _selection.Range);
        }

        UpdateActiveOutlineSelection();
        RefreshStructureAuthoringState();
        UpdateStatus(state);
    }

    private async Task PersistDraftAsync(string text)
    {
        CancelAutosave();
        await UpdateDraftStateAsync(text, persistDocument: true);
    }

    private async Task UpdateDraftStateAsync(string text, bool persistDocument)
    {
        _sourceText = text ?? string.Empty;
        var persistedText = BuildPersistedDocument(_sourceText);
        var title = _screenTitle;
        await Diagnostics.RunAsync(
            PersistDraftOperation,
            PersistDraftMessage,
            async () =>
            {
                await SessionService.UpdateDraftAsync(
                    title,
                    persistedText,
                    SessionService.State.DocumentName,
                    SessionService.State.ScriptId);

                if (persistDocument)
                {
                    var savedDocument = await SessionService.SaveAsync();
                    if (string.IsNullOrWhiteSpace(ScriptId))
                    {
                        ScriptId = savedDocument.Id;
                        Navigation.NavigateTo($"/editor?id={Uri.EscapeDataString(savedDocument.Id)}", replace: true);
                    }
                }

                PopulateEditorState();
            },
            clearRecoverableOnSuccess: string.IsNullOrWhiteSpace(SessionService.State.ErrorMessage));
    }

    private void QueueAutosave()
    {
        CancelAutosave();
        _autosaveCancellationSource = new CancellationTokenSource();
        var autosaveToken = _autosaveCancellationSource.Token;
        _ = RunAutosaveAsync(autosaveToken);
    }

    private async Task RunAutosaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(AutosaveDelayMilliseconds, cancellationToken);
            await InvokeAsync(() => PersistDraftAsync(_sourceText));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelAutosave()
    {
        if (_autosaveCancellationSource is null)
        {
            return;
        }

        _autosaveCancellationSource.Cancel();
        _autosaveCancellationSource.Dispose();
        _autosaveCancellationSource = null;
    }

    private async Task PersistMetadataAsync()
    {
        _history.TryRecord(_sourceText, _selection.Range);
        await PersistDraftAsync(_sourceText);
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

    private void UpdateStatus(ScriptWorkspaceState state)
    {
        _status = new EditorStatusViewModel(
            _selection.Line,
            _selection.Column,
            _profile,
            _baseWpm,
            _segments.Count,
            _segments.Sum(segment => segment.Blocks.Count),
            state.WordCount,
            FormatDuration(state.EstimatedDuration),
            _version);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var safeDuration = duration <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : duration;
        return $"{(int)safeDuration.TotalMinutes}:{safeDuration.Seconds:00}";
    }

    private static string GetMetadata(IReadOnlyDictionary<string, string> metadata, string key, string fallback)
    {
        if (metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return fallback;
    }

    private static int TryGetInt(IReadOnlyDictionary<string, string> metadata, string key, int fallback)
    {
        return metadata.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}
