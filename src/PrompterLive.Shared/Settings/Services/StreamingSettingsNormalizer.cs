using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Models.Workspace;
using PrompterLive.Core.Services.Streaming;

namespace PrompterLive.Shared.Services;

internal static class StreamingSettingsNormalizer
{
    public static StudioSettings Normalize(StudioSettings settings, IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var streaming = settings.Streaming;
        var hasModernTargets = streaming.ObsVirtualCameraEnabled
            || streaming.NdiOutputEnabled
            || streaming.LocalRecordingEnabled
            || streaming.LiveKitEnabled
            || streaming.VdoNinjaEnabled
            || streaming.YoutubeEnabled
            || streaming.TwitchEnabled
            || streaming.CustomRtmpEnabled;

        StreamStudioSettings normalizedStreaming;
        if (hasModernTargets)
        {
            normalizedStreaming = streaming with
            {
                CustomRtmpName = string.IsNullOrWhiteSpace(streaming.CustomRtmpName)
                    ? StreamingDefaults.CustomTargetName
                    : streaming.CustomRtmpName
            };
        }
        else
        {
            var customRtmpUrl = string.IsNullOrWhiteSpace(streaming.CustomRtmpUrl)
                ? streaming.RtmpUrl
                : streaming.CustomRtmpUrl;
            var customRtmpKey = string.IsNullOrWhiteSpace(streaming.CustomRtmpStreamKey)
                ? streaming.StreamKey
                : streaming.CustomRtmpStreamKey;

            normalizedStreaming = streaming.OutputMode switch
            {
                StreamingOutputMode.VirtualCamera => streaming with { ObsVirtualCameraEnabled = true },
                StreamingOutputMode.NdiOutput => streaming with { NdiOutputEnabled = true },
                StreamingOutputMode.LocalRecording => streaming with { LocalRecordingEnabled = true },
                StreamingOutputMode.DirectRtmp => streaming with
                {
                    CustomRtmpEnabled = !string.IsNullOrWhiteSpace(customRtmpUrl),
                    CustomRtmpName = string.IsNullOrWhiteSpace(streaming.CustomRtmpName)
                        ? StreamingDefaults.CustomTargetName
                        : streaming.CustomRtmpName,
                    CustomRtmpUrl = customRtmpUrl,
                    CustomRtmpStreamKey = customRtmpKey
                },
                _ => streaming
            };
        }

        normalizedStreaming = GoLiveDestinationRouting.Normalize(normalizedStreaming, sceneCameras);
        return settings with { Streaming = normalizedStreaming };
    }
}
