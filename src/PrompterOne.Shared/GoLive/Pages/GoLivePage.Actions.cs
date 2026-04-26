using PrompterOne.Core.Models.Media;
using PrompterOne.Shared.Services;

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
            var previousTakeTarget = BuildRecordingTakeTarget(blocks, previousIndex);
            _recordingBlockIndex = Math.Clamp(_recordingBlockIndex + direction, 0, blocks.Count - 1);
            if (_recordingBlockIndex == previousIndex || !GoLiveSession.State.IsRecordingActive)
            {
                return;
            }

            var exportedTake = await GoLiveOutputRuntime.RotateRecordingTakeAsync(BuildRuntimeRequest(SelectedCamera));
            await SaveRecordingBlockTakeAsync(previousTakeTarget, exportedTake);
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

    private GoLiveRecordingTakeTarget? BuildCurrentRecordingTakeTarget()
    {
        var blocks = RecordingBlocks;
        return blocks.Count == 0
            ? null
            : BuildRecordingTakeTarget(blocks, NormalizeRecordingBlockIndex(blocks.Count));
    }

    private GoLiveRecordingTakeTarget? BuildRecordingTakeTarget(
        IReadOnlyList<PrompterOne.Core.Services.Preview.BlockPreviewModel> blocks,
        int blockIndex)
    {
        if (blockIndex < 0 || blockIndex >= blocks.Count || string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            return null;
        }

        var block = blocks[blockIndex];
        return new GoLiveRecordingTakeTarget(
            BuildRecordingBlockKey(block, blockIndex),
            string.IsNullOrWhiteSpace(block.Title) ? $"Block {blockIndex + 1}" : block.Title.Trim());
    }

    private async Task SaveRecordingBlockTakeAsync(
        GoLiveRecordingTakeTarget? target,
        GoLiveRecordingTakeExportSnapshot? exportedTake)
    {
        if (target is null
            || exportedTake is null
            || exportedTake.SizeBytes <= 0
            || string.IsNullOrWhiteSpace(exportedTake.FileName))
        {
            return;
        }

        var nextTakeNumber = _recordingBlockTakes.Count(take => string.Equals(take.BlockKey, target.BlockKey, StringComparison.Ordinal)) + 1;
        var record = new GoLiveBlockTakeRecord(
            Id: Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture),
            BlockKey: target.BlockKey,
            BlockTitle: target.BlockTitle,
            TakeNumber: nextTakeNumber,
            FileName: exportedTake.FileName,
            MimeType: exportedTake.MimeType,
            SaveMode: exportedTake.SaveMode,
            SizeBytes: exportedTake.SizeBytes,
            RecordedAt: DateTimeOffset.UtcNow);

        _recordingBlockTakes = _recordingBlockTakes
            .Concat([record])
            .OrderBy(take => take.BlockTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(take => take.TakeNumber)
            .ToList();

        await BlockTakeStore.SaveAsync(SessionService.State.ScriptId, _recordingBlockTakes);
        StateHasChanged();
    }

    private sealed record GoLiveRecordingTakeTarget(string BlockKey, string BlockTitle);
}
