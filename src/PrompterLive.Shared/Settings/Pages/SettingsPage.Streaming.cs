using Microsoft.AspNetCore.Components;
using PrompterLive.Core.Models.Workspace;
using PrompterLive.Core.Services.Streaming;

namespace PrompterLive.Shared.Pages;

public partial class SettingsPage
{
    private const string DirectRtmpOutputModeValue = "direct-rtmp";
    private const string LocalRecordingOutputModeValue = "local-recording";
    private const string NdiOutputModeValue = "ndi-output";
    private const string VirtualCameraOutputModeValue = "virtual-camera";

    private string SelectedStreamingOutputModeValue => _studioSettings.Streaming.OutputMode switch
    {
        StreamingOutputMode.DirectRtmp => DirectRtmpOutputModeValue,
        StreamingOutputMode.LocalRecording => LocalRecordingOutputModeValue,
        StreamingOutputMode.NdiOutput => NdiOutputModeValue,
        _ => VirtualCameraOutputModeValue
    };

    private async Task ToggleObsOutputAsync()
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                ObsVirtualCameraEnabled = !_studioSettings.Streaming.ObsVirtualCameraEnabled,
                OutputMode = StreamingOutputMode.VirtualCamera
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleNdiOutputAsync()
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                NdiOutputEnabled = !_studioSettings.Streaming.NdiOutputEnabled,
                OutputMode = StreamingOutputMode.NdiOutput
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleRecordingOutputAsync()
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                LocalRecordingEnabled = !_studioSettings.Streaming.LocalRecordingEnabled,
                OutputMode = StreamingOutputMode.LocalRecording
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleLiveKitSettingsAsync()
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                LiveKitEnabled = !_studioSettings.Streaming.LiveKitEnabled
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleVdoSettingsAsync()
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                VdoNinjaEnabled = !_studioSettings.Streaming.VdoNinjaEnabled
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleYoutubeSettingsAsync()
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                YoutubeEnabled = !_studioSettings.Streaming.YoutubeEnabled,
                OutputMode = StreamingOutputMode.DirectRtmp
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleTwitchSettingsAsync()
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                TwitchEnabled = !_studioSettings.Streaming.TwitchEnabled,
                OutputMode = StreamingOutputMode.DirectRtmp
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleCustomRtmpSettingsAsync()
    {
        var customName = string.IsNullOrWhiteSpace(_studioSettings.Streaming.CustomRtmpName)
            ? StreamingDefaults.CustomTargetName
            : _studioSettings.Streaming.CustomRtmpName;

        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                CustomRtmpEnabled = !_studioSettings.Streaming.CustomRtmpEnabled,
                CustomRtmpName = customName,
                OutputMode = StreamingOutputMode.DirectRtmp,
                RtmpUrl = _studioSettings.Streaming.CustomRtmpUrl,
                StreamKey = _studioSettings.Streaming.CustomRtmpStreamKey
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleStreamingDestinationSourceAsync((string TargetId, string SourceId) update)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = GoLiveDestinationRouting.ToggleSource(
                _studioSettings.Streaming,
                update.TargetId,
                update.SourceId,
                _sceneCameras)
        };

        await PersistStudioSettingsAsync();
    }

    private async Task OnStreamingOutputResolutionChanged(ChangeEventArgs args)
    {
        if (!Enum.TryParse<StreamingResolutionPreset>(args.Value?.ToString(), out var outputResolution))
        {
            return;
        }

        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { OutputResolution = outputResolution }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task OnStreamingOutputModeChanged(ChangeEventArgs args)
    {
        var nextMode = args.Value?.ToString() switch
        {
            DirectRtmpOutputModeValue => StreamingOutputMode.DirectRtmp,
            LocalRecordingOutputModeValue => StreamingOutputMode.LocalRecording,
            NdiOutputModeValue => StreamingOutputMode.NdiOutput,
            _ => StreamingOutputMode.VirtualCamera
        };

        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { OutputMode = nextMode }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateStreamingBitrateAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var bitrate))
        {
            return;
        }

        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { BitrateKbps = Math.Max(250, bitrate) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleSettingsTextOverlayAsync()
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                ShowTextOverlay = !_studioSettings.Streaming.ShowTextOverlay
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task ToggleSettingsIncludeCameraAsync()
    {
        var nextValue = !_studioSettings.Streaming.IncludeCameraInOutput;
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { IncludeCameraInOutput = nextValue }
        };

        foreach (var camera in _sceneCameras)
        {
            MediaSceneService.SetIncludeInOutput(camera.SourceId, nextValue);
        }

        await PersistSceneAsync();
        _studioSettings = _studioSettings with
        {
            Streaming = GoLiveDestinationRouting.Normalize(_studioSettings.Streaming, _sceneCameras)
        };
        await PersistStudioSettingsAsync();
    }

    private async Task UpdateLiveKitServerSettingAsync(ChangeEventArgs args)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { LiveKitServerUrl = GetInputValue(args) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateLiveKitRoomSettingAsync(ChangeEventArgs args)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { LiveKitRoomName = GetInputValue(args) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateLiveKitTokenSettingAsync(ChangeEventArgs args)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { LiveKitToken = GetInputValue(args) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateVdoRoomSettingAsync(ChangeEventArgs args)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { VdoNinjaRoomName = GetInputValue(args) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateVdoPublishUrlSettingAsync(ChangeEventArgs args)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { VdoNinjaPublishUrl = GetInputValue(args) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateYoutubeUrlSettingAsync(ChangeEventArgs args)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { YoutubeRtmpUrl = GetInputValue(args) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateYoutubeKeySettingAsync(ChangeEventArgs args)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { YoutubeStreamKey = GetInputValue(args) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateTwitchUrlSettingAsync(ChangeEventArgs args)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { TwitchRtmpUrl = GetInputValue(args) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateTwitchKeySettingAsync(ChangeEventArgs args)
    {
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { TwitchStreamKey = GetInputValue(args) }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateCustomRtmpNameSettingAsync(ChangeEventArgs args)
    {
        var nextName = string.IsNullOrWhiteSpace(GetInputValue(args))
            ? StreamingDefaults.CustomTargetName
            : GetInputValue(args);

        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with { CustomRtmpName = nextName }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateCustomRtmpUrlSettingAsync(ChangeEventArgs args)
    {
        var nextValue = GetInputValue(args);
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                CustomRtmpUrl = nextValue,
                RtmpUrl = nextValue
            }
        };

        await PersistStudioSettingsAsync();
    }

    private async Task UpdateCustomRtmpKeySettingAsync(ChangeEventArgs args)
    {
        var nextValue = GetInputValue(args);
        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                CustomRtmpStreamKey = nextValue,
                StreamKey = nextValue
            }
        };

        await PersistStudioSettingsAsync();
    }

    private static string GetInputValue(ChangeEventArgs args) => args.Value?.ToString() ?? string.Empty;
}
