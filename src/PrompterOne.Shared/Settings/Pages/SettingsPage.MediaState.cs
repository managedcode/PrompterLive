using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Pages;

public partial class SettingsPage
{
    private const string EnableDevicesLabel = "Enable Camera + Mic";
    private const string LoadSettingsMessage = "Unable to load settings right now.";
    private const string LoadSettingsOperation = "Settings load";
    private const string PersistSceneMessage = "Unable to save scene changes.";
    private const string PersistSceneOperation = "Settings save scene";
    private const string PersistStudioMessage = "Unable to save studio settings.";
    private const string PersistStudioOperation = "Settings save studio";
    private const string RefreshDevicesLabel = "Refresh Devices";
    private const string RefreshMediaMessage = "Unable to refresh camera and microphone access.";
    private const string RefreshMediaOperation = "Settings media refresh";

    private bool _loadState = true;
    private IReadOnlyList<MediaDeviceInfo> _cameraDevices = [];
    private IReadOnlyList<MediaDeviceInfo> _devices = [];
    private IReadOnlyList<MediaDeviceInfo> _microphoneDevices = [];
    private string? _previewCameraId;
    private string? _primaryMicrophoneId;
    private MediaPermissionsState _permissions = new(false, false);
    private StudioSettings _studioSettings = StudioSettings.Default;

    private IReadOnlyList<SceneCameraSource> _sceneCameras => MediaSceneService.State.Cameras;

    private bool AllMicrophonesMutedOutsideGoLive =>
        _microphoneDevices.Count > 0 && _microphoneDevices.All(microphone => !IsMicrophoneEnabled(microphone));

    private bool AllSceneCamerasIncludedInOutput =>
        _sceneCameras.Count > 0 && _sceneCameras.All(camera => camera.Transform.IncludeInOutput);

    private string MediaAccessActionLabel =>
        _permissions.CameraGranted && _permissions.MicrophoneGranted ? RefreshDevicesLabel : EnableDevicesLabel;

    private MediaDeviceInfo? PreviewCamera => ResolvePreviewCamera();

    private SceneCameraSource? PreviewSceneCamera => ResolveSceneCamera(PreviewCamera?.DeviceId);

    private string SelectedCameraId => ResolveSelectedCamera()?.DeviceId ?? string.Empty;

    private string SelectedMicrophoneId => ResolveSelectedMicrophone()?.DeviceId ?? string.Empty;

    private bool ShouldShowMediaAccessAction =>
        !_permissions.CameraGranted || !_permissions.MicrophoneGranted || _devices.Count == 0;

    protected override void OnInitialized()
    {
        Shell.ShowSettings();
        InitializeCrossTabSync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_loadState)
        {
            return;
        }

        _loadState = false;
        await Diagnostics.RunAsync(
            LoadSettingsOperation,
            LoadSettingsMessage,
            async () =>
            {
                await LoadAsync();
                StateHasChanged();
            });
    }

    private async Task LoadAsync()
    {
        await Bootstrapper.EnsureReadyAsync();
        await LoadPreferencesAsync();
        _permissions = await MediaPermissionService.QueryAsync();

        try
        {
            _devices = await MediaDeviceService.GetDevicesAsync();
        }
        catch
        {
            _devices = [];
        }

        _cameraDevices = _devices.Where(device => device.Kind == MediaDeviceKind.Camera).ToList();
        _microphoneDevices = _devices.Where(device => device.Kind == MediaDeviceKind.Microphone).ToList();
        _primaryMicrophoneId = MediaSceneService.State.PrimaryMicrophoneId;

        await SeedSceneDefaultsAsync();
        var loadedSettings = await StudioSettingsStore.LoadAsync();
        _studioSettings = StreamingSettingsNormalizer.Normalize(loadedSettings, _sceneCameras);
        if (!EqualityComparer<StudioSettings>.Default.Equals(loadedSettings, _studioSettings))
        {
            await PersistStudioSettingsAsync();
        }

        await NormalizeStudioSettingsAsync();
        EnsureCameraPreviewSelection();
    }

    private void EnsureCameraPreviewSelection()
    {
        if (!string.IsNullOrWhiteSpace(_previewCameraId)
            && _cameraDevices.Any(device => string.Equals(device.DeviceId, _previewCameraId, StringComparison.Ordinal)))
        {
            return;
        }

        _previewCameraId = ResolveSelectedCameraCandidate()?.DeviceId
            ?? (_cameraDevices.Count > 0 ? _cameraDevices[0].DeviceId : null);
    }

    private async Task SeedSceneDefaultsAsync()
    {
        var changed = false;

        if (_sceneCameras.Count == 0 && _cameraDevices.Count > 0)
        {
            var defaultCamera = _cameraDevices.FirstOrDefault(device => device.IsDefault) ?? _cameraDevices[0];
            MediaSceneService.AddCamera(defaultCamera.DeviceId, defaultCamera.Label);
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(MediaSceneService.State.PrimaryMicrophoneId) && _microphoneDevices.Count > 0)
        {
            var defaultMicrophone = _microphoneDevices.FirstOrDefault(device => device.IsDefault) ?? _microphoneDevices[0];
            MediaSceneService.SetPrimaryMicrophone(defaultMicrophone.DeviceId, defaultMicrophone.Label);
            MediaSceneService.UpsertAudioInput(new AudioInputState(defaultMicrophone.DeviceId, defaultMicrophone.Label));
            _primaryMicrophoneId = defaultMicrophone.DeviceId;
            changed = true;
        }

        if (changed)
        {
            await PersistSceneAsync();
        }
    }

    private async Task RequestMediaAccessAsync()
    {
        await Diagnostics.RunAsync(
            RefreshMediaOperation,
            RefreshMediaMessage,
            async () =>
            {
                _permissions = await MediaPermissionService.RequestAsync();
                await LoadAsync();
            });
    }

    private async Task NormalizeStudioSettingsAsync()
    {
        var selectedCamera = ResolveSelectedCameraCandidate();
        var selectedMicrophone = ResolveSelectedMicrophoneCandidate();
        var selectedSceneCamera = ResolveSceneCamera(selectedCamera?.DeviceId);
        var selectedAudioInput = selectedMicrophone is null ? null : GetAudioInput(selectedMicrophone);
        var nextSettings = _studioSettings with
        {
            Camera = _studioSettings.Camera with
            {
                DefaultCameraId = selectedCamera?.DeviceId,
                MirrorCamera = selectedSceneCamera?.Transform.MirrorHorizontal ?? _studioSettings.Camera.MirrorCamera,
                AutoStartOnRead = SessionService.State.ReaderSettings.ShowCameraScene
            },
            Microphone = _studioSettings.Microphone with
            {
                DefaultMicrophoneId = selectedMicrophone?.DeviceId,
                InputLevelPercent = ClampPercent(selectedAudioInput is null
                    ? _studioSettings.Microphone.InputLevelPercent
                    : (int)Math.Round(selectedAudioInput.Gain * 100d))
            },
            Streaming = _studioSettings.Streaming with
            {
                ProgramCapture = _studioSettings.Streaming.ProgramCaptureSettings with
                {
                    IncludeCameraInOutput = _sceneCameras.Count == 0
                        ? _studioSettings.Streaming.ProgramCaptureSettings.IncludeCameraInOutput
                        : _sceneCameras.Any(camera => camera.Transform.IncludeInOutput)
                }
            }
        };

        var hasChanges = !EqualityComparer<StudioSettings>.Default.Equals(_studioSettings, nextSettings);
        _studioSettings = nextSettings;
        if (hasChanges)
        {
            await PersistStudioSettingsAsync();
        }
    }

    private async Task PersistSceneAsync()
    {
        _primaryMicrophoneId = MediaSceneService.State.PrimaryMicrophoneId;
        await Diagnostics.RunAsync(
            PersistSceneOperation,
            PersistSceneMessage,
            () => SettingsStore.SaveAsync(BrowserAppSettingsKeys.SceneSettings, MediaSceneService.State));
    }

    private Task PersistStudioSettingsAsync() =>
        Diagnostics.RunAsync(
            PersistStudioOperation,
            PersistStudioMessage,
            () => StudioSettingsStore.SaveAsync(_studioSettings));

    private MediaDeviceInfo? ResolveSelectedCamera() =>
        _cameraDevices.FirstOrDefault(device => string.Equals(device.DeviceId, ResolveSelectedCameraCandidate()?.DeviceId, StringComparison.Ordinal))
        ?? ResolveSelectedCameraCandidate();

    private MediaDeviceInfo? ResolveSelectedCameraCandidate()
    {
        if (!string.IsNullOrWhiteSpace(_studioSettings.Camera.DefaultCameraId))
        {
            var configured = _cameraDevices.FirstOrDefault(device => string.Equals(device.DeviceId, _studioSettings.Camera.DefaultCameraId, StringComparison.Ordinal));
            if (configured is not null)
            {
                return configured;
            }
        }

        var sceneCamera = _sceneCameras.Count > 0 ? _sceneCameras[0] : null;
        if (sceneCamera is not null)
        {
            return _cameraDevices.FirstOrDefault(device => string.Equals(device.DeviceId, sceneCamera.DeviceId, StringComparison.Ordinal))
                ?? new MediaDeviceInfo(sceneCamera.DeviceId, sceneCamera.Label, MediaDeviceKind.Camera);
        }

        var fallbackCamera = _cameraDevices.Count > 0 ? _cameraDevices[0] : null;
        return _cameraDevices.FirstOrDefault(device => device.IsDefault) ?? fallbackCamera;
    }

    private SceneCameraSource? ResolveSceneCamera(string? deviceId)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return _sceneCameras.FirstOrDefault(camera => string.Equals(camera.DeviceId, deviceId, StringComparison.Ordinal));
        }

        return _sceneCameras.Count > 0 ? _sceneCameras[0] : null;
    }

    private MediaDeviceInfo? ResolveSelectedMicrophone() =>
        _microphoneDevices.FirstOrDefault(device => string.Equals(device.DeviceId, ResolveSelectedMicrophoneCandidate()?.DeviceId, StringComparison.Ordinal))
        ?? ResolveSelectedMicrophoneCandidate();

    private MediaDeviceInfo? ResolveSelectedMicrophoneCandidate()
    {
        if (!string.IsNullOrWhiteSpace(_primaryMicrophoneId))
        {
            var current = _microphoneDevices.FirstOrDefault(device => string.Equals(device.DeviceId, _primaryMicrophoneId, StringComparison.Ordinal));
            if (current is not null)
            {
                return current;
            }
        }

        if (!string.IsNullOrWhiteSpace(_studioSettings.Microphone.DefaultMicrophoneId))
        {
            var configured = _microphoneDevices.FirstOrDefault(device => string.Equals(device.DeviceId, _studioSettings.Microphone.DefaultMicrophoneId, StringComparison.Ordinal));
            if (configured is not null)
            {
                return configured;
            }
        }

        var fallbackMicrophone = _microphoneDevices.Count > 0 ? _microphoneDevices[0] : null;
        return _microphoneDevices.FirstOrDefault(device => device.IsDefault) ?? fallbackMicrophone;
    }

    private MediaDeviceInfo? ResolvePreviewCamera()
    {
        if (!string.IsNullOrWhiteSpace(_previewCameraId))
        {
            var previewCamera = _cameraDevices.FirstOrDefault(device => string.Equals(device.DeviceId, _previewCameraId, StringComparison.Ordinal));
            if (previewCamera is not null)
            {
                return previewCamera;
            }
        }

        return ResolveSelectedCamera();
    }

    private static int ClampPercent(int value) => Math.Clamp(value, 0, 100);
}
