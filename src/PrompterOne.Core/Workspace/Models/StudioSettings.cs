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
    ProgramCaptureProfile? ProgramCapture = null,
    RecordingProfile? Recording = null,
    IReadOnlyList<TransportConnectionProfile>? TransportConnections = null,
    IReadOnlyList<DistributionTargetProfile>? DistributionTargets = null,
    IReadOnlyList<GoLiveDestinationSourceSelection>? SourceSelections = null)
{
    public ProgramCaptureProfile ProgramCaptureSettings => ProgramCapture ?? new ProgramCaptureProfile();

    public RecordingProfile RecordingSettings => Recording ?? new RecordingProfile();
}

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
