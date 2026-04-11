namespace PrompterOne.Shared.Pages;

public partial class EditorPage : IDisposable
{
    protected override void OnInitialized()
    {
        EditorDocumentSaveCoordinator.Register(HandleSaveFileRequestedAsync);
        _aiSpotlightEditRegistration = AiSpotlight.RegisterDocumentEditTarget(ApplyAiEditPlanAsync);
    }

    public void Dispose()
    {
        EditorDocumentSaveCoordinator.Unregister(HandleSaveFileRequestedAsync);
        _aiSpotlightEditRegistration?.Dispose();
        CancelDraftAnalysis();
        CancelAutosave();
    }
}
