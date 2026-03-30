using PrompterLive.Core.Services.Workspace;
using PrompterLive.Shared.Services.Editor;

namespace PrompterLive.Shared.Pages;

public partial class EditorPage
{
    private void RefreshDraftViewFromSource()
    {
        var persistedText = BuildPersistedDocument(_sourceText);

        try
        {
            var scriptData = TpsParser.ParseTps(persistedText);
            _segments = OutlineBuilder.Build(scriptData, _sourceText, 0);
            _errorMessage = null;
            UpdateDraftMetrics(scriptData);
            StageDraftText(persistedText, null);
        }
        catch (Exception exception)
        {
            _segments = [];
            _errorMessage = exception.Message;
            UpdateDraftMetrics((PrompterLive.Core.Models.Documents.ScriptData?)null);
            StageDraftText(persistedText, exception.Message);
        }

        UpdateSyntaxDiagnostics();
        RefreshSelectionState();
    }

    private void StageDraftText(string persistedText, string? errorMessage)
    {
        if (SessionService is not ScriptSessionService sessionService)
        {
            return;
        }

        sessionService.StageDraftText(
            _screenTitle,
            persistedText,
            SessionService.State.DocumentName,
            SessionService.State.ScriptId,
            errorMessage);
    }

    private void UpdateDraftMetrics(PrompterLive.Core.Models.Workspace.ScriptWorkspaceState state)
    {
        _draftMetrics = new EditorDraftMetrics(state.WordCount, state.EstimatedDuration);
    }

    private void UpdateDraftMetrics(PrompterLive.Core.Models.Documents.ScriptData? scriptData)
    {
        _draftMetrics = EditorDraftMetricsCalculator.Calculate(scriptData);
    }
}
