namespace PrompterLive.Shared.Pages;

public partial class GoLivePage
{
    private async Task ToggleSceneOutputAsync(string sourceId)
    {
        var camera = SceneCameras.FirstOrDefault(item => string.Equals(item.SourceId, sourceId, StringComparison.Ordinal));
        if (camera is null)
        {
            return;
        }

        MediaSceneService.SetIncludeInOutput(sourceId, !camera.Transform.IncludeInOutput);
        await PersistSceneAsync();
    }
}
