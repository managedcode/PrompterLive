using System.Globalization;
using PrompterLive.Core.Services.Editor;
using PrompterLive.Core.Samples;

namespace PrompterLive.Shared.Pages;

public partial class EditorPage
{
    protected override Task OnParametersSetAsync()
    {
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
    }

    private async Task EnsureSessionLoadedAsync()
    {
        if (!string.IsNullOrWhiteSpace(ScriptId))
        {
            await LoadScriptFromQueryAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            await SessionService.LoadSampleAsync(SampleScriptCatalog.DemoSampleId);
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
        var computedDuration = FormatDuration(state.EstimatedDuration);

        _sourceText = document.Body;
        _screenTitle = _frontMatterService.ResolveTitle(state.Text, state.Title);
        _author = GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Author, DefaultAuthor);
        _baseWpm = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.BaseWpm, state.ScriptData?.TargetWpm ?? 140);
        _profile = GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Profile, _baseWpm >= 250 ? DefaultProfileRsvp : DefaultProfileActor);
        _version = GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Version, DefaultVersion);
        _createdDate = GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Created, _createdDate);
        _displayDuration = GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.DisplayDuration, computedDuration);
        _xslowOffset = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.XslowOffset, DefaultXslowOffset);
        _slowOffset = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.SlowOffset, DefaultSlowOffset);
        _fastOffset = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.FastOffset, DefaultFastOffset);
        _xfastOffset = TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.XfastOffset, DefaultXfastOffset);
        _segments = OutlineBuilder.Build(state.ScriptData, document.Body, 0);
        _errorMessage = state.ErrorMessage;

        UpdateSyntaxDiagnostics();
        ResetHistoryIfNeeded(resetHistory);
        UpdateActiveOutlineSelection();
        RefreshStructureAuthoringState();
        UpdateStatus(state);
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
