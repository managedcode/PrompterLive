using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;

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

public sealed record GoLiveOutputTransportConnection(
    string ConnectionId,
    string Name,
    StreamingPlatformKind PlatformKind,
    StreamingTransportRole Roles,
    bool IsEnabled,
    string ServerUrl,
    string BaseUrl,
    string RoomName,
    string Token,
    string PublishUrl,
    string ViewUrl)
{
    public bool CanIngestRemoteSources => IsEnabled && Roles.HasFlag(StreamingTransportRole.Source);

    public bool CanPublishProgram =>
        IsEnabled
        && Roles.HasFlag(StreamingTransportRole.Publish)
        && PlatformKind switch
        {
            StreamingPlatformKind.LiveKit => !string.IsNullOrWhiteSpace(ServerUrl)
                && !string.IsNullOrWhiteSpace(RoomName)
                && !string.IsNullOrWhiteSpace(Token),
            StreamingPlatformKind.VdoNinja => !string.IsNullOrWhiteSpace(PublishUrl)
                || !string.IsNullOrWhiteSpace(RoomName),
            _ => false
        };
}

public sealed record GoLiveOutputRuntimeRequest(
    string PrimarySourceId,
    GoLiveProgramVideoSettings ProgramVideo,
    IReadOnlyList<GoLiveOutputVideoSource> VideoSources,
    IReadOnlyList<GoLiveOutputAudioInput> AudioInputs,
    GoLiveRecordingExportSettings Recording,
    bool RecordingEnabled,
    IReadOnlyList<GoLiveOutputTransportConnection> TransportConnections)
{
    public string PrimaryCameraDeviceId => ResolvePrimaryVideoSource()?.DeviceId ?? string.Empty;

    public string PrimaryMicrophoneDeviceId =>
        AudioInputs.FirstOrDefault(input => input.IsPrimary)?.DeviceId ?? string.Empty;

    public bool CanStartRecording =>
        RecordingEnabled
        && VideoSources.Any(source => source.IsRenderable);

    public IReadOnlyList<GoLiveOutputTransportConnection> GetPublishableConnections(StreamingPlatformKind kind)
    {
        return TransportConnections
            .Where(connection => connection.PlatformKind == kind && connection.CanPublishProgram)
            .ToArray();
    }

    private GoLiveOutputVideoSource? ResolvePrimaryVideoSource()
    {
        return VideoSources.FirstOrDefault(source => source.IsPrimary)
            ?? VideoSources.FirstOrDefault(source => source.IsRenderable);
    }
}
