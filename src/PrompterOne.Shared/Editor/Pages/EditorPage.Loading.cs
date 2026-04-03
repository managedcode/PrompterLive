using System.Globalization;

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
            LoadEditorMessage,
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                await EnsureSessionLoadedAsync();
                PopulateEditorState(resetHistory: true);
                StateHasChanged();
            });

        _isEditorReady = true;
        StateHasChanged();
    }

    private async Task EnsureSessionLoadedAsync()
    {
        if (!string.IsNullOrWhiteSpace(ScriptId))
        {
            await LoadScriptFromQueryAsync();
        }
    }

    private async Task LoadScriptFromQueryAsync()
    {
        var document = await ScriptRepository.GetAsync(ScriptId!);
        if (document is not null &&
            !string.Equals(SessionService.State.ScriptId, document.Id, StringComparison.Ordinal))
        {
            await SessionService.OpenAsync(document);
        }

        if (document is not null)
        {
            _createdDate = document.UpdatedAt.LocalDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }

    private void PopulateEditorState(bool resetHistory = false)
    {
        var state = SessionService.State;
        var document = _frontMatterService.Parse(state.Text);
        var metadata = document.Metadata;

        if (resetHistory)
        {
            _draftRevision = checked(_draftRevision + 1);
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
    }

    private void UpdateSyntaxDiagnostics()
    {
        if (string.IsNullOrWhiteSpace(_errorMessage))
        {
            Diagnostics.ClearRecoverable(EditorSyntaxOperation);
            return;
        }

        Diagnostics.ReportRecoverable(EditorSyntaxOperation, EditorSyntaxMessage, _errorMessage);
    }

    private void ResetHistoryIfNeeded(bool resetHistory)
    {
        if (resetHistory || !_history.IsInitialized)
        {
            _history.Reset(_sourceText, _selection.Range);
        }
    }
}
