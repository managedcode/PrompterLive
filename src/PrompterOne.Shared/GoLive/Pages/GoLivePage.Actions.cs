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

    private Task ToggleRecordingBlockContextAsync()
    {
        _showRecordingBlockContext = !_showRecordingBlockContext;
        NormalizeRecordingBlockIndex();
        return Task.CompletedTask;
    }

    private async Task MoveRecordingBlockContextAsync(int direction)
    {
        await RunSerializedInteractionAsync(async () =>
        {
            var blocks = RecordingBlocks;
            if (blocks.Count == 0)
            {
                _recordingBlockIndex = 0;
                return;
            }

            var previousIndex = _recordingBlockIndex;
            _recordingBlockIndex = Math.Clamp(_recordingBlockIndex + direction, 0, blocks.Count - 1);
            if (_recordingBlockIndex == previousIndex || !GoLiveSession.State.IsRecordingActive)
            {
                return;
            }

            await GoLiveOutputRuntime.RotateRecordingTakeAsync(BuildRuntimeRequest(SelectedCamera));
        });
    }

    private int NormalizeRecordingBlockIndex() => NormalizeRecordingBlockIndex(RecordingBlocks.Count);

    private int NormalizeRecordingBlockIndex(int blockCount)
    {
        if (blockCount == 0)
        {
            _recordingBlockIndex = 0;
            return _recordingBlockIndex;
        }

        _recordingBlockIndex = Math.Clamp(_recordingBlockIndex, 0, blockCount - 1);
        return _recordingBlockIndex;
    }
}
