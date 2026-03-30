using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Models.Workspace;
using PrompterLive.Core.Samples;

namespace PrompterLive.Shared.Pages;

public partial class GoLivePage
{
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_loadState)
        {
            return;
        }

        _loadState = false;
        await Diagnostics.RunAsync(
            GoLiveLoadOperation,
            GoLiveLoadMessage,
            async () =>
            {
                await Bootstrapper.EnsureReadyAsync();
                await EnsureSessionLoadedAsync();
                await EnsureSceneDefaultsAsync();
                await LoadStudioSettingsAsync();
                UpdateScreenMetadata();
                StateHasChanged();
            });
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

        var microphoneDevices = devices.Where(device => device.Kind == MediaDeviceKind.Microphone).ToList();
        var changed = false;

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
            await SessionService.LoadSampleAsync(SampleScriptCatalog.DemoSampleId);
        }
    }

    private void UpdateScreenMetadata()
    {
        _screenTitle = SessionService.State.Title;
        _screenSubtitle = SessionService.State.PreviewSegments.Count > 0
            ? SessionService.State.PreviewSegments[0].Title
            : StreamingSubtitle;
        SyncGoLiveSessionState();
        Shell.ShowGoLive(_screenTitle, _screenSubtitle, SessionService.State.ScriptId);
    }

    private async Task PersistSceneAsync()
    {
        await Diagnostics.RunAsync(
            GoLiveSceneOperation,
            GoLiveSceneMessage,
            () => SettingsStore.SaveAsync(SceneSettingsKey, MediaSceneService.State));
        SyncGoLiveSessionState();
    }

    private async Task PersistStudioSettingsAsync()
    {
        await Diagnostics.RunAsync(
            GoLiveStudioOperation,
            GoLiveStudioMessage,
            () => StudioSettingsStore.SaveAsync(_studioSettings));
        SyncGoLiveSessionState();
    }
}
