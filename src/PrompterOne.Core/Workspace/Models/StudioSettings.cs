using PrompterOne.Core.Models.Streaming;

namespace PrompterOne.Core.Models.Workspace;

public enum CameraResolutionPreset
{
    FullHd1080,
    Hd720,
    UltraHd4K,
    Sd480
}

public enum CameraFrameRatePreset
{
    Fps24,
    Fps30,
    Fps60
}

public enum StreamingOutputMode
{
    VirtualCamera,
    NdiOutput,
    DirectRtmp,
    LocalRecording
}

public enum StreamingResolutionPreset
{
    FullHd1080p30,
    FullHd1080p60,
    Hd720p30,
    UltraHd2160p30
}

public sealed record CameraStudioSettings(
    string? DefaultCameraId = null,
    CameraResolutionPreset Resolution = CameraResolutionPreset.FullHd1080,
    CameraFrameRatePreset FrameRate = CameraFrameRatePreset.Fps30,
    bool MirrorCamera = false,
    bool AutoStartOnRead = true);

public sealed record MicrophoneStudioSettings(
    string? DefaultMicrophoneId = null,
    int InputLevelPercent = 65,
    bool NoiseSuppression = true,
    bool EchoCancellation = true);

public sealed record StreamStudioSettings(
    StreamingOutputMode OutputMode = StreamingOutputMode.VirtualCamera,
    StreamingResolutionPreset OutputResolution = StreamingResolutionPreset.FullHd1080p30,
    int BitrateKbps = 6000,
    bool ShowTextOverlay = true,
    bool IncludeCameraInOutput = true,
    IReadOnlyList<StreamingProfile>? ExternalDestinations = null,
    IReadOnlyList<GoLiveDestinationSourceSelection>? DestinationSourceSelections = null,
    string RtmpUrl = "",
    string StreamKey = "",
    bool ObsVirtualCameraEnabled = false,
    bool NdiOutputEnabled = false,
    bool LocalRecordingEnabled = false,
    bool LiveKitEnabled = false,
    string LiveKitServerUrl = "",
    string LiveKitRoomName = "",
    string LiveKitToken = "",
    bool VdoNinjaEnabled = false,
    string VdoNinjaRoomName = "",
    string VdoNinjaPublishUrl = "",
    bool YoutubeEnabled = false,
    string YoutubeRtmpUrl = "",
    string YoutubeStreamKey = "",
    bool TwitchEnabled = false,
    string TwitchRtmpUrl = "",
    string TwitchStreamKey = "",
    bool CustomRtmpEnabled = false,
    string CustomRtmpName = StreamingDefaults.CustomTargetName,
    string CustomRtmpUrl = "",
    string CustomRtmpStreamKey = "");

public sealed record StudioSettings(
    CameraStudioSettings Camera,
    MicrophoneStudioSettings Microphone,
    StreamStudioSettings Streaming)
{
    public static StudioSettings Default { get; } = new(
        new CameraStudioSettings(),
        new MicrophoneStudioSettings(),
        new StreamStudioSettings());
}

public static class StreamingDefaults
{
    public const string CustomTargetName = "Custom RTMP";
}
