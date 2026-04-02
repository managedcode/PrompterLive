using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.GoLive.Models;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Settings.Models;
using PrompterOne.Shared.Storage;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_loadState)
        {
            return;
        }

        _loadState = false;
        _bootstrapTask ??= BootstrapPageAsync();
        await _bootstrapTask;
        await SyncPrimaryMicrophoneMonitorAsync();
    }

    private async Task BootstrapPageAsync()
    {
        await Diagnostics.RunAsync(
            GoLiveText.Load.LoadOperation,
            GoLiveText.Load.LoadMessage,
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                await EnsureSessionLoadedAsync();
                await EnsureSceneDefaultsAsync();
                await LoadRecordingPreferencesAsync();
                await LoadStudioSettingsAsync();
                await SyncRemoteSourcesAsync();
                UpdateScreenMetadata();
                StateHasChanged();
            });
    }

    private async Task EnsurePageReadyAsync()
    {
        if (_bootstrapTask is null && _loadState)
        {
            _loadState = false;
            _bootstrapTask = BootstrapPageAsync();
        }

        if (_bootstrapTask is null)
        {
            return;
        }

        await _bootstrapTask;
    }

    private async Task RunSerializedInteractionAsync(Func<Task> action)
    {
        await EnsurePageReadyAsync();
        await _interactionGate.WaitAsync();

        try
        {
            await action();
        }
        finally
        {
            _interactionGate.Release();
        }
    }

    private async Task LoadStudioSettingsAsync()
    {
        _studioSettings = await StudioSettingsStore.LoadAsync();
        var normalized = StreamingSettingsNormalizer.Normalize(_studioSettings, SceneCameras);
        if (!EqualityComparer<StudioSettings>.Default.Equals(_studioSettings, normalized))
        {
            _studioSettings = normalized;
            await PersistStudioSettingsAsync();
        }
    }

    private async Task LoadRecordingPreferencesAsync()
    {
        _recordingPreferences = await SettingsStore.LoadAsync<SettingsPagePreferences>(SettingsPagePreferences.StorageKey)
            ?? SettingsPagePreferences.Default;
    }

    private async Task EnsureSceneDefaultsAsync()
    {
        IReadOnlyList<MediaDeviceInfo> devices;
        try
        {
            devices = await MediaDeviceService.GetDevicesAsync();
        }
        catch
        {
            return;
        }

        _mediaDevices = devices;
        var cameraDevices = devices.Where(device => device.Kind == MediaDeviceKind.Camera).ToList();
        var microphoneDevices = devices.Where(device => device.Kind == MediaDeviceKind.Microphone).ToList();
        var changed = false;

        if (SceneCameras.Count == 0 && cameraDevices.Count > 0)
        {
            var defaultCamera = cameraDevices.FirstOrDefault(device => device.IsDefault) ?? cameraDevices[0];
            MediaSceneService.AddCamera(defaultCamera.DeviceId, defaultCamera.Label);
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(MediaSceneService.State.PrimaryMicrophoneId) && microphoneDevices.Count > 0)
        {
            var defaultMicrophone = microphoneDevices.FirstOrDefault(device => device.IsDefault) ?? microphoneDevices[0];
            MediaSceneService.SetPrimaryMicrophone(defaultMicrophone.DeviceId, defaultMicrophone.Label);
            MediaSceneService.UpsertAudioInput(new AudioInputState(defaultMicrophone.DeviceId, defaultMicrophone.Label));
            changed = true;
        }

        if (changed)
        {
            await PersistSceneAsync();
        }
    }

    private async Task EnsureSessionLoadedAsync()
    {
        if (!string.IsNullOrWhiteSpace(ScriptId))
        {
            var document = await ScriptRepository.GetAsync(ScriptId);
            if (document is not null &&
                !string.Equals(SessionService.State.ScriptId, document.Id, StringComparison.Ordinal))
            {
                await SessionService.OpenAsync(document);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            return;
        }
    }

    private void UpdateScreenMetadata()
    {
        _sessionTitle = SessionService.State.Title;
        _sessionSubtitle = SessionService.State.PreviewSegments.Count > 0
            ? SessionService.State.PreviewSegments[0].Title
            : GoLiveText.Chrome.StreamingSubtitle;
        SyncGoLiveSessionState();
        EnsureStudioSurfaceState();
        Shell.ShowGoLive(SessionService.State.ScriptId);
    }

    private async Task PersistSceneAsync()
    {
        await Diagnostics.RunAsync(
            GoLiveText.Load.SaveSceneOperation,
            GoLiveText.Load.SaveSceneMessage,
            () => SettingsStore.SaveAsync(BrowserAppSettingsKeys.SceneSettings, MediaSceneService.State));
        SyncGoLiveSessionState();
    }

    private async Task PersistStudioSettingsAsync()
    {
        await Diagnostics.RunAsync(
            GoLiveText.Load.SaveStudioOperation,
            GoLiveText.Load.SaveStudioMessage,
            () => StudioSettingsStore.SaveAsync(_studioSettings));
        SyncGoLiveSessionState();
    }
}
