using PrompterOne.Core.Services.Workspace;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private async Task PersistDraftAsync(
        string text,
        string? documentNameOverride = null,
        CancellationToken cancellationToken = default)
    {
        CancelDraftAnalysis();
        CancelAutosave();
        var revision = PrepareDraftPersistence(text, documentNameOverride);
        await PersistPreparedDraftAsync(revision, cancellationToken);
    }

    private long PrepareDraftPersistence(string text, string? documentNameOverride = null)
    {
        _sourceText = text ?? string.Empty;
        RefreshDraftViewFromSource(documentNameOverride);
        _ = InvokeAsync(StateHasChanged);
        return checked(++_draftRevision);
    }

    private void PersistDraftInBackground(
        string text,
        string? documentNameOverride = null,
        CancellationToken cancellationToken = default)
    {
        CancelDraftAnalysis();
        CancelAutosave();
        var revision = PrepareDraftPersistence(text, documentNameOverride);
        _ = InvokeAsync(() => PersistPreparedDraftAsync(revision, cancellationToken));
    }

    private Task PersistPreparedDraftAsync(long revision, CancellationToken cancellationToken) =>
        TryPersistDraftStateAsync(PersistDraftOperation, PersistDraftMessage, persistDocument: true, cancellationToken, revision);

    private Task<bool> TryPersistDraftStateAsync(
        string operation,
        string message,
        bool persistDocument,
        CancellationToken cancellationToken,
        long revision) =>
        Diagnostics.RunAsync(
            operation,
            message,
            () => PersistDraftStateCoreAsync(persistDocument, cancellationToken, revision),
            clearRecoverableOnSuccess: string.IsNullOrWhiteSpace(SessionService.State.ErrorMessage));

    private async Task PersistDraftStateCoreAsync(
        bool persistDocument,
        CancellationToken cancellationToken,
        long revision)
    {
        var persistedText = BuildPersistedDocument(_sourceText);
        var title = _screenTitle;
        var assignScriptId = string.IsNullOrWhiteSpace(ScriptId);

        await SessionService.UpdateDraftAsync(
            title,
            persistedText,
            SessionService.State.DocumentName,
            SessionService.State.ScriptId,
            cancellationToken);

        if (persistDocument)
        {
            var savedDocument = await SessionService.SaveAsync(cancellationToken);
            await CaptureLocalRevisionAsync(savedDocument, cancellationToken);
            if (assignScriptId)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (revision != _draftRevision)
                {
                    StageDraftIdentity(savedDocument.Id, savedDocument.DocumentName);
                }

                _pendingAutosaveSelfNavigationScriptId = savedDocument.Id;
                _preserveSplitFeedbackOnNextLoad = _splitFeedback is not null || _splitOperationInProgress;
                ScriptId = savedDocument.Id;
                Navigation.NavigateTo($"/editor?id={Uri.EscapeDataString(savedDocument.Id)}", replace: true);
            }
        }

        ApplyPersistedDraftState(revision);
    }

    private void StageDraftIdentity(string scriptId, string documentName)
    {
        if (SessionService is not ScriptSessionService sessionService)
        {
            return;
        }

        sessionService.StageDraftText(
            _screenTitle,
            BuildPersistedDocument(_sourceText),
            documentName,
            scriptId,
            _errorMessage);
    }

    private void ApplyPersistedDraftState(long revision)
    {
        if (revision != _draftRevision)
        {
            return;
        }

        PopulateEditorState(clearSplitFeedback: false);
        _ = InvokeAsync(StateHasChanged);
    }

    private void QueueAutosave()
    {
        CancelAutosave();
        if (!ShouldQueueAutosave())
        {
            return;
        }

        _autosaveCancellationSource = new CancellationTokenSource();
        _ = RunAutosaveAsync(_autosaveCancellationSource.Token);
    }

    private void QueueDraftAnalysis()
    {
        CancelDraftAnalysis();
        _draftAnalysisCancellationSource = new CancellationTokenSource();
        _ = RunDraftAnalysisAsync(_draftAnalysisCancellationSource.Token);
    }

    private async Task RunAutosaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(GetAutosaveDelayMilliseconds(), cancellationToken);
            await InvokeAsync(() =>
            {
                _skipNextRenderFromTyping = false;
                return PersistDraftAsync(_sourceText, cancellationToken: cancellationToken);
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private async Task RunDraftAnalysisAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(GetDraftAnalysisDelayMilliseconds(), cancellationToken);
            await InvokeAsync(() =>
            {
                _skipNextRenderFromTyping = false;
                RefreshDraftViewFromSource();
                StateHasChanged();
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
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

    private void CancelDraftAnalysis()
    {
        if (_draftAnalysisCancellationSource is null)
        {
            return;
        }

        _draftAnalysisCancellationSource.Cancel();
        _draftAnalysisCancellationSource.Dispose();
        _draftAnalysisCancellationSource = null;
    }

    private int GetAutosaveDelayMilliseconds() =>
        IsLargeDraft()
            ? LargeDraftAutosaveDelayMilliseconds
            : string.IsNullOrWhiteSpace(SessionService.State.ScriptId)
                ? UntitledDraftAutosaveDelayMilliseconds
                : AutosaveDelayMilliseconds;

    private int GetDraftAnalysisDelayMilliseconds() =>
        IsLargeDraft()
            ? LargeDraftAnalysisDelayMilliseconds
            : DraftAnalysisDelayMilliseconds;

    private bool ShouldQueueAutosave()
    {
        if (!_fileStorageSettings.FileAutoSaveEnabled)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            return true;
        }

        return _sourceText.Trim().Length >= UntitledDraftAutosaveCharacterThreshold;
    }

    private bool IsLargeDraft() =>
        _sourceText.Length >= LargeDraftDebounceThreshold;
}
