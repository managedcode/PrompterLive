using PrompterOne.Core.Models.Media;

namespace PrompterOne.Shared.Services;

public sealed record GoLiveOutputSourceTransform(
    double X,
    double Y,
    double Width,
    double Height,
    double Rotation,
    bool MirrorHorizontal,
    bool MirrorVertical,
    bool Visible,
    bool IncludeInOutput,
    int ZIndex,
    double Opacity);

public sealed record GoLiveOutputVideoSource(
    string SourceId,
    string DeviceId,
    string Label,
    GoLiveOutputSourceTransform Transform,
    bool IsPrimary)
{
    public bool IsRenderable => IsPrimary || (Transform.Visible && Transform.IncludeInOutput);
}

public sealed record GoLiveOutputAudioInput(
    string DeviceId,
    string Label,
    int DelayMs,
    double Gain,
    bool IsMuted,
    AudioRouteTarget RouteTarget,
    bool IsPrimary)
{
    public bool IsRoutedToProgram => !IsMuted && RouteTarget is AudioRouteTarget.Stream or AudioRouteTarget.Both;
}

public sealed record GoLiveProgramVideoSettings(
    int Width,
    int Height,
    int FrameRate,
    string ResolutionLabel,
    string FrameRateLabel);

public sealed record GoLiveRecordingExportSettings(
    string FileStem,
    bool PreferFilePicker,
    string ContainerLabel,
    string VideoCodecLabel,
    string AudioCodecLabel,
    int VideoBitrateKbps,
    int AudioBitrateKbps,
    int AudioSampleRate,
    int AudioChannelCount);

public sealed record GoLiveOutputRuntimeRequest(
    string PrimarySourceId,
    GoLiveProgramVideoSettings ProgramVideo,
    IReadOnlyList<GoLiveOutputVideoSource> VideoSources,
    IReadOnlyList<GoLiveOutputAudioInput> AudioInputs,
    GoLiveRecordingExportSettings Recording,
    bool ObsEnabled,
    bool RecordingEnabled,
    bool LiveKitEnabled,
    string LiveKitServerUrl,
    string LiveKitRoomName,
    string LiveKitToken,
    bool VdoNinjaEnabled,
    string VdoNinjaPublishUrl,
    string VdoNinjaRoomName)
{
    public string PrimaryCameraDeviceId => ResolvePrimaryVideoSource()?.DeviceId ?? string.Empty;

    public string PrimaryMicrophoneDeviceId =>
        AudioInputs.FirstOrDefault(input => input.IsPrimary)?.DeviceId ?? string.Empty;

    public bool CanStartRecording =>
        RecordingEnabled
        && VideoSources.Any(source => source.IsRenderable);

    public bool CanStartLiveKit =>
        LiveKitEnabled
        && VideoSources.Any(source => source.IsRenderable)
        && !string.IsNullOrWhiteSpace(LiveKitServerUrl)
        && !string.IsNullOrWhiteSpace(LiveKitRoomName)
        && !string.IsNullOrWhiteSpace(LiveKitToken);

    public bool CanStartVdoNinja =>
        VdoNinjaEnabled
        && VideoSources.Any(source => source.IsRenderable)
        && (!string.IsNullOrWhiteSpace(VdoNinjaPublishUrl)
            || !string.IsNullOrWhiteSpace(VdoNinjaRoomName));

    public bool CanStartObs =>
        ObsEnabled
        && VideoSources.Any(source => source.IsRenderable);

    private GoLiveOutputVideoSource? ResolvePrimaryVideoSource()
    {
        return VideoSources.FirstOrDefault(source => source.IsPrimary)
            ?? VideoSources.FirstOrDefault(source => source.IsRenderable);
    }
}
