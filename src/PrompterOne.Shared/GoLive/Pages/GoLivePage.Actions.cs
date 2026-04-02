using PrompterOne.Core.Models.Media;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private async Task AddAvailableCameraAsync()
    {
        await EnsurePageReadyAsync();

        var nextCamera = _mediaDevices.FirstOrDefault(device =>
            device.Kind == MediaDeviceKind.Camera
            && SceneCameras.All(camera => !string.Equals(camera.DeviceId, device.DeviceId, StringComparison.Ordinal)));

        if (nextCamera is null)
        {
            return;
        }

        var source = MediaSceneService.AddCamera(nextCamera.DeviceId, nextCamera.Label);
        GoLiveSession.SelectSource(AvailableSceneSources, source.SourceId);
        await PersistSceneAsync();
    }

    private async Task ToggleSceneOutputAsync(string sourceId)
    {
        await EnsurePageReadyAsync();

        if (IsRemoteSource(sourceId))
        {
            var remoteSource = _remoteSceneSources.FirstOrDefault(item => string.Equals(item.SourceId, sourceId, StringComparison.Ordinal));
            if (remoteSource is null)
            {
                return;
            }

            SetRemoteSourceIncludeInOutput(sourceId, !remoteSource.Transform.IncludeInOutput);
            return;
        }

        var camera = SceneCameras.FirstOrDefault(item => string.Equals(item.SourceId, sourceId, StringComparison.Ordinal));
        if (camera is null)
        {
            return;
        }

        MediaSceneService.SetIncludeInOutput(sourceId, !camera.Transform.IncludeInOutput);
        await PersistSceneAsync();
    }
}
