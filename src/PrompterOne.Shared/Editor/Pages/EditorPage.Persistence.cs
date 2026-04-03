using PrompterOne.Core.Services.Workspace;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private async Task PersistDraftAsync(string text)
    {
        CancelDraftAnalysis();
        CancelAutosave();
        var revision = PrepareDraftPersistence(text);
        await PersistPreparedDraftAsync(text, revision, CancellationToken.None);
    }

    private long PrepareDraftPersistence(string text)
    {
        _sourceText = text ?? string.Empty;
        RefreshDraftViewFromSource();
        _ = InvokeAsync(StateHasChanged);
        return checked(++_draftRevision);
    }

    private void PersistDraftInBackground(string text)
    {
        CancelDraftAnalysis();
        CancelAutosave();
        var revision = PrepareDraftPersistence(text);
        _ = InvokeAsync(() => PersistPreparedDraftAsync(text, revision, CancellationToken.None));
    }

    private Task PersistPreparedDraftAsync(string text, long revision, CancellationToken cancellationToken) =>
        UpdateDraftStateAsync(text, persistDocument: true, cancellationToken, revision);

    private async Task UpdateDraftStateAsync(
        string text,
        bool persistDocument,
        CancellationToken cancellationToken,
        long revision)
    {
        var persistedText = BuildPersistedDocument(_sourceText);
        var title = _screenTitle;
        var assignScriptId = string.IsNullOrWhiteSpace(ScriptId);
        await Diagnostics.RunAsync(
            PersistDraftOperation,
            PersistDraftMessage,
            async () =>
            {
                await SessionService.UpdateDraftAsync(
                    title,
                    persistedText,
                    SessionService.State.DocumentName,
                    SessionService.State.ScriptId,
                    cancellationToken);

                if (persistDocument)
                {
                    var savedDocument = await SessionService.SaveAsync(cancellationToken);
                    if (assignScriptId)
                    {
                        if (revision != _draftRevision)
                        {
                            StageDraftIdentity(savedDocument.Id, savedDocument.DocumentName);
                        }

                        ScriptId = savedDocument.Id;
                        Navigation.NavigateTo($"/editor?id={Uri.EscapeDataString(savedDocument.Id)}", replace: true);
                    }
                }

                ApplyPersistedDraftState(revision);
            },
            clearRecoverableOnSuccess: string.IsNullOrWhiteSpace(SessionService.State.ErrorMessage));
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

        PopulateEditorState();
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
                return PersistDraftAsync(_sourceText);
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
        if (!string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            return true;
        }

        return _sourceText.Trim().Length >= UntitledDraftAutosaveCharacterThreshold;
    }

    private bool IsLargeDraft() =>
        _sourceText.Length >= LargeDraftDebounceThreshold;
}
