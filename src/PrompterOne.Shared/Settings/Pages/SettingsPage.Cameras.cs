using Microsoft.AspNetCore.Components;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Settings.Components;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Pages;

public partial class SettingsPage
{
    private const string ActiveCardCssClass = "active";
    private const string BuiltInConnectionLabel = "Built-in";
    private const string CameraFeedBaseCssClass = "set-cam-feed";
    private const string DeviceCardCssClass = "set-device-card";
    private const string OffCameraFeedCssClass = "cam-off";
    private const string SelectedCameraCssClass = "set-cam-selected";
    private const string UsbConnectionLabel = "USB";
    private const string VirtualConnectionLabel = "Virtual";

    private static readonly IReadOnlyList<SettingsSelectOption> CameraResolutionOptions =
    [
        new(nameof(CameraResolutionPreset.FullHd1080), "1920 x 1080 (Full HD)"),
        new(nameof(CameraResolutionPreset.Hd720), "1280 x 720 (HD)"),
        new(nameof(CameraResolutionPreset.UltraHd4K), "3840 x 2160 (4K)"),
        new(nameof(CameraResolutionPreset.Sd480), "640 x 480 (SD)")
    ];

    private async Task ToggleCameraSelectionAsync(MediaDeviceInfo camera)
    {
        var existing = _sceneCameras.FirstOrDefault(source => string.Equals(source.DeviceId, camera.DeviceId, StringComparison.Ordinal));
        if (existing is null)
        {
            MediaSceneService.AddCamera(camera.DeviceId, camera.Label);
        }
        else
        {
            MediaSceneService.RemoveCamera(existing.SourceId);
        }

        SelectCameraPreview(camera);
        await PersistSceneAsync();
        await NormalizeStudioSettingsAsync();
        EnsureCameraPreviewSelection();
    }

    private void SelectCameraPreview(MediaDeviceInfo camera) => _previewCameraId = camera.DeviceId;

    private async Task SetPrimaryCameraAsync(MediaDeviceInfo camera)
    {
        SelectCameraPreview(camera);

        if (!_sceneCameras.Any(source => string.Equals(source.DeviceId, camera.DeviceId, StringComparison.Ordinal)))
        {
            MediaSceneService.AddCamera(camera.DeviceId, camera.Label);
            await PersistSceneAsync();
        }

        _studioSettings = _studioSettings with
        {
            Camera = _studioSettings.Camera with
            {
                DefaultCameraId = camera.DeviceId,
                MirrorCamera = ResolveSceneCamera(camera.DeviceId)?.Transform.MirrorHorizontal ?? _studioSettings.Camera.MirrorCamera
            }
        };

        await PersistStudioSettingsAsync();
        await NormalizeStudioSettingsAsync();
    }

    private async Task ToggleAllSceneOutputsAsync()
    {
        if (_sceneCameras.Count == 0)
        {
            return;
        }

        var includeInOutput = !AllSceneCamerasIncludedInOutput;
        foreach (var camera in _sceneCameras)
        {
            MediaSceneService.SetIncludeInOutput(camera.SourceId, includeInOutput);
        }

        await PersistSceneAsync();
        await NormalizeStudioSettingsAsync();
    }

    private async Task ToggleSceneMirrorAsync(string sourceId)
    {
        var source = _sceneCameras.FirstOrDefault(item => string.Equals(item.SourceId, sourceId, StringComparison.Ordinal));
        if (source is null)
        {
            return;
        }

        MediaSceneService.UpdateTransform(sourceId, source.Transform with { MirrorHorizontal = !source.Transform.MirrorHorizontal });
        await PersistSceneAsync();
        await NormalizeStudioSettingsAsync();
    }

    private async Task OnCameraResolutionChanged(ChangeEventArgs args)
    {
        if (!Enum.TryParse<CameraResolutionPreset>(args.Value?.ToString(), out var resolution))
        {
            return;
        }

        _studioSettings = _studioSettings with
        {
            Camera = _studioSettings.Camera with { Resolution = resolution }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleReaderCameraSceneAsync()
    {
        var current = SessionService.State.ReaderSettings;
        var next = current with { ShowCameraScene = !current.ShowCameraScene };
        await SessionService.UpdateReaderSettingsAsync(next);
        await SettingsStore.SaveAsync(BrowserAppSettingsKeys.ReaderSettings, next);

        _studioSettings = _studioSettings with
        {
            Camera = _studioSettings.Camera with { AutoStartOnRead = next.ShowCameraScene }
        };

        await PersistStudioSettingsAsync();
    }

    private static string BuildCameraCardCssClass(bool isEnabled, bool isPreview)
    {
        var cssClass = isEnabled ? $"{DeviceCardCssClass} {ActiveCardCssClass}" : DeviceCardCssClass;
        return isPreview ? $"{cssClass} {SelectedCameraCssClass}" : cssClass;
    }

    private static string BuildCameraFeedCssClass(bool isEnabled) =>
        isEnabled ? CameraFeedBaseCssClass : $"{CameraFeedBaseCssClass} {OffCameraFeedCssClass}";

    private string BuildCameraMeta(MediaDeviceInfo camera, bool isPrimary)
    {
        var parts = new List<string>(capacity: 4)
        {
            ResolveCameraConnectionLabel(camera),
            BuildCameraResolutionSummary(),
            BuildFrameRateSummary()
        };

        if (isPrimary)
        {
            parts.Add("Primary");
        }

        return string.Join(" · ", parts);
    }

    private string BuildCameraPreviewDescription() =>
        PreviewCamera is null ? string.Empty : BuildCameraMeta(PreviewCamera, IsPrimaryCamera(PreviewCamera));

    private string BuildFrameRateSummary() =>
        _studioSettings.Camera.FrameRate switch
        {
            CameraFrameRatePreset.Fps24 => "24fps",
            CameraFrameRatePreset.Fps60 => "60fps",
            _ => "30fps"
        };

    private string BuildCameraResolutionSummary() =>
        _studioSettings.Camera.Resolution switch
        {
            CameraResolutionPreset.Hd720 => "1280×720",
            CameraResolutionPreset.UltraHd4K => "3840×2160",
            CameraResolutionPreset.Sd480 => "640×480",
            _ => "1920×1080"
        };
    private bool IsPrimaryCamera(MediaDeviceInfo camera) =>
        string.Equals(SelectedCameraId, camera.DeviceId, StringComparison.Ordinal);

    private static string ResolveCameraConnectionLabel(MediaDeviceInfo camera)
    {
        if (camera.Label.Contains(VirtualConnectionLabel, StringComparison.OrdinalIgnoreCase)
            || camera.Label.Contains("OBS", StringComparison.OrdinalIgnoreCase))
        {
            return VirtualConnectionLabel;
        }

        if (camera.Label.Contains("FaceTime", StringComparison.OrdinalIgnoreCase)
            || camera.Label.Contains(BuiltInConnectionLabel, StringComparison.OrdinalIgnoreCase)
            || camera.Label.Contains("MacBook", StringComparison.OrdinalIgnoreCase))
        {
            return BuiltInConnectionLabel;
        }

        return UsbConnectionLabel;
    }
}
