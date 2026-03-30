namespace PrompterLive.Shared.Pages;

public partial class EditorPage
{
    private async Task PersistDraftAsync(string text)
    {
        CancelDraftAnalysis();
        CancelAutosave();
        await UpdateDraftStateAsync(text, persistDocument: true, CancellationToken.None);
    }

    private async Task UpdateDraftStateAsync(string text, bool persistDocument, CancellationToken cancellationToken)
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
                    SessionService.State.ScriptId,
                    cancellationToken);

                if (persistDocument)
                {
                    var savedDocument = await SessionService.SaveAsync(cancellationToken);
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
            await Task.Delay(DraftAnalysisDelayMilliseconds, cancellationToken);
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
        string.IsNullOrWhiteSpace(SessionService.State.ScriptId)
            ? UntitledDraftAutosaveDelayMilliseconds
            : AutosaveDelayMilliseconds;
}
