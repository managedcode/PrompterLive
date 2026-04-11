using System.Globalization;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    protected override Task OnParametersSetAsync()
    {
        _isEditorReady = false;
        _loadState = true;
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_loadState)
        {
            return;
        }

        _loadState = false;
        await Diagnostics.RunAsync(
            LoadEditorOperation,
            Text(UiTextKey.EditorLoadMessage),
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                await EnsureSessionLoadedAsync();
                var preserveHistoryOnLoad = ConsumePreserveHistoryOnNextLoad();
                await LoadEditorFileWorkflowAsync();
                PopulateEditorState(
                    resetHistory: !preserveHistoryOnLoad,
                    clearSplitFeedback: !ConsumePreserveSplitFeedbackOnNextLoad());
                StateHasChanged();
            });

        _isEditorReady = true;
        StateHasChanged();
    }

    private async Task EnsureSessionLoadedAsync()
    {
        var requestedScriptId = ScriptRouteSessionLoader.ResolveRequestedScriptId(ScriptId, Navigation.Uri);
        UpdateDraftSessionOrigin(requestedScriptId);
        if (!string.IsNullOrWhiteSpace(requestedScriptId))
        {
            await LoadScriptFromQueryAsync(requestedScriptId);
        }
    }

    private void UpdateDraftSessionOrigin(string? requestedScriptId)
    {
        if (string.IsNullOrWhiteSpace(requestedScriptId))
        {
            _currentDraftSessionStartedUntitled = true;
            _pendingAutosaveSelfNavigationScriptId = null;
            _preserveHistoryOnNextLoad = false;
            return;
        }

        if (_currentDraftSessionStartedUntitled &&
            string.Equals(requestedScriptId, _pendingAutosaveSelfNavigationScriptId, StringComparison.Ordinal))
        {
            _pendingAutosaveSelfNavigationScriptId = null;
            _preserveHistoryOnNextLoad = true;
            return;
        }

        _currentDraftSessionStartedUntitled = false;
        _pendingAutosaveSelfNavigationScriptId = null;
        _preserveHistoryOnNextLoad = false;
    }

    private async Task LoadScriptFromQueryAsync(string requestedScriptId)
    {
        var didLoadRequestedScript = await ScriptRouteSessionLoader.EnsureRequestedSessionAsync(
            requestedScriptId,
            ScriptRepository,
            SessionService);

        if (didLoadRequestedScript)
        {
            var document = await ScriptRepository.GetAsync(requestedScriptId);
            if (document is not null)
            {
                _createdDate = document.UpdatedAt.LocalDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }
    }

    private void PopulateEditorState(bool resetHistory = false, bool clearSplitFeedback = true)
    {
        var state = SessionService.State;
        var document = _frontMatterService.Parse(state.Text);
        var metadata = document.Metadata;

        if (resetHistory)
        {
            _draftRevision = checked(_draftRevision + 1);
        }

        ResetMetadataDefaults(state);
        if (clearSplitFeedback)
        {
            _splitFeedback = null;
        }
        else if (_splitFeedback is not null)
        {
            _metadataRailSelectedTab = EditorMetadataRailTab.Tools;
        }
        _sourceText = document.Body;
        ApplyLoadedMetadata(metadata, state);
        _segments = OutlineBuilder.Build(state.ScriptData, document.Body, 0);
        _errorMessage = state.ErrorMessage;
        UpdateDraftMetrics(state);

        UpdateSyntaxDiagnostics();
        ResetHistoryIfNeeded(resetHistory);
        UpdateActiveOutlineSelection();
        RefreshStructureAuthoringState();
        UpdateStatus();
        Shell.ShowEditor(_screenTitle, state.ScriptId);
        PublishEditorAiContext();
    }

    private bool ConsumePreserveSplitFeedbackOnNextLoad()
    {
        var shouldPreserveSplitFeedback = _preserveSplitFeedbackOnNextLoad;
        _preserveSplitFeedbackOnNextLoad = false;
        return shouldPreserveSplitFeedback;
    }

    private bool ConsumePreserveHistoryOnNextLoad()
    {
        var shouldPreserveHistory = _preserveHistoryOnNextLoad;
        _preserveHistoryOnNextLoad = false;
        return shouldPreserveHistory;
    }

    private void ResetMetadataDefaults(ScriptWorkspaceState state)
    {
        var defaultBaseWpm = state.ScriptData?.TargetWpm ?? 140;
        _screenTitle = state.Title;
        _author = DefaultAuthor;
        _baseWpm = defaultBaseWpm;
        _profile = defaultBaseWpm >= 250 ? DefaultProfileRsvp : DefaultProfileActor;
        _version = DefaultVersion;
        _createdDate = string.IsNullOrWhiteSpace(state.ScriptId)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : _createdDate;
        _displayDuration = FormatDuration(state.EstimatedDuration);
        _xslowOffset = DefaultXslowOffset;
        _slowOffset = DefaultSlowOffset;
        _fastOffset = DefaultFastOffset;
        _xfastOffset = DefaultXfastOffset;
    }

    private void UpdateSyntaxDiagnostics()
    {
        if (string.IsNullOrWhiteSpace(_errorMessage))
        {
            Diagnostics.ClearRecoverable(EditorSyntaxOperation);
            return;
        }

        Diagnostics.ReportRecoverable(EditorSyntaxOperation, Text(UiTextKey.EditorSyntaxMessage), _errorMessage);
    }

    private void ResetHistoryIfNeeded(bool resetHistory)
    {
        if (resetHistory || !_history.IsInitialized)
        {
            _history.Reset(_sourceText, _selection.Range);
        }
    }
}
