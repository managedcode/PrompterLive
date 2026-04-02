namespace PrompterOne.Shared.Services;

internal sealed record GoLiveOutputAudioSnapshot(
    int ProgramLevelPercent,
    int RecordingLevelPercent);

internal sealed record GoLiveOutputProgramSnapshot(
    int AudioInputCount,
    int FrameRate,
    int Height,
    string PrimarySourceId,
    int VideoSourceCount,
    int Width);

internal sealed record GoLiveOutputRecordingSnapshot(
    bool Active,
    int AudioBitrateKbps,
    string FileName,
    string MimeType,
    string RequestedAudioCodec,
    string RequestedContainer,
    string RequestedVideoCodec,
    string SaveMode,
    long SizeBytes,
    int VideoBitrateKbps);

internal sealed record GoLiveOutputRuntimeSnapshot(
    string AudioDeviceId,
    GoLiveOutputAudioSnapshot? Audio,
    bool HasMediaStream,
    GoLiveOutputProviderSnapshot? LiveKit,
    GoLiveOutputProgramSnapshot? Program,
    GoLiveOutputRecordingSnapshot? Recording,
    GoLiveOutputVdoNinjaSnapshot? VdoNinja,
    string VideoDeviceId);

public sealed record GoLiveOutputAudioState(
    int ProgramLevelPercent,
    int RecordingLevelPercent)
{
    public static GoLiveOutputAudioState Default { get; } = new(
        ProgramLevelPercent: 0,
        RecordingLevelPercent: 0);
}

public sealed record GoLiveOutputProgramState(
    int AudioInputCount,
    int FrameRate,
    int Height,
    string PrimarySourceId,
    int VideoSourceCount,
    int Width)
{
    public static GoLiveOutputProgramState Default { get; } = new(
        AudioInputCount: 0,
        FrameRate: 0,
        Height: 0,
        PrimarySourceId: string.Empty,
        VideoSourceCount: 0,
        Width: 0);
}

public sealed record GoLiveOutputRecordingState(
    bool Active,
    int AudioBitrateKbps,
    string FileName,
    string MimeType,
    string RequestedAudioCodec,
    string RequestedContainer,
    string RequestedVideoCodec,
    string SaveMode,
    long SizeBytes,
    int VideoBitrateKbps)
{
    public static GoLiveOutputRecordingState Default { get; } = new(
        Active: false,
        AudioBitrateKbps: 0,
        FileName: string.Empty,
        MimeType: string.Empty,
        RequestedAudioCodec: string.Empty,
        RequestedContainer: string.Empty,
        RequestedVideoCodec: string.Empty,
        SaveMode: string.Empty,
        SizeBytes: 0,
        VideoBitrateKbps: 0);
}

public sealed record GoLiveOutputRuntimeState(
    GoLiveOutputAudioState Audio,
    bool HasMediaStream,
    GoLiveOutputProviderState LiveKit,
    bool LiveKitActive,
    GoLiveOutputProgramState Program,
    GoLiveOutputRecordingState Recording,
    bool RecordingActive,
    GoLiveOutputVdoNinjaState VdoNinja,
    bool VdoNinjaActive,
    string CameraDeviceId,
    string MicrophoneDeviceId)
{
    public static GoLiveOutputRuntimeState Default { get; } = new(
        Audio: GoLiveOutputAudioState.Default,
        HasMediaStream: false,
        LiveKit: GoLiveOutputProviderState.Default,
        LiveKitActive: false,
        Program: GoLiveOutputProgramState.Default,
        Recording: GoLiveOutputRecordingState.Default,
        RecordingActive: false,
        VdoNinja: GoLiveOutputVdoNinjaState.Default,
        VdoNinjaActive: false,
        CameraDeviceId: string.Empty,
        MicrophoneDeviceId: string.Empty);

    public bool HasActiveOutputs => HasLiveOutputs || RecordingActive;

    public bool HasLiveOutputs => LiveKitActive || VdoNinjaActive;

    internal static GoLiveOutputRuntimeState FromSnapshot(GoLiveOutputRuntimeSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return Default;
        }

        return new(
            Audio: snapshot.Audio is null
                ? GoLiveOutputAudioState.Default
                : new(
                    snapshot.Audio.ProgramLevelPercent,
                    snapshot.Audio.RecordingLevelPercent),
            HasMediaStream: snapshot.HasMediaStream,
            LiveKit: snapshot.LiveKit is null
                ? GoLiveOutputProviderState.Default
                : new(
                    snapshot.LiveKit.Active,
                    snapshot.LiveKit.Connected,
                    snapshot.LiveKit.RoomName ?? string.Empty,
                    snapshot.LiveKit.ServerUrl ?? string.Empty),
            LiveKitActive: snapshot.LiveKit?.Active == true,
            Program: snapshot.Program is null
                ? GoLiveOutputProgramState.Default
                : new(
                    snapshot.Program.AudioInputCount,
                    snapshot.Program.FrameRate,
                    snapshot.Program.Height,
                    snapshot.Program.PrimarySourceId ?? string.Empty,
                    snapshot.Program.VideoSourceCount,
                    snapshot.Program.Width),
            Recording: snapshot.Recording is null
                ? GoLiveOutputRecordingState.Default
                : new(
                    snapshot.Recording.Active,
                    snapshot.Recording.AudioBitrateKbps,
                    snapshot.Recording.FileName ?? string.Empty,
                    snapshot.Recording.MimeType ?? string.Empty,
                    snapshot.Recording.RequestedAudioCodec ?? string.Empty,
                    snapshot.Recording.RequestedContainer ?? string.Empty,
                    snapshot.Recording.RequestedVideoCodec ?? string.Empty,
                    snapshot.Recording.SaveMode ?? string.Empty,
                    snapshot.Recording.SizeBytes,
                    snapshot.Recording.VideoBitrateKbps),
            RecordingActive: snapshot.Recording?.Active == true,
            VdoNinja: snapshot.VdoNinja is null
                ? GoLiveOutputVdoNinjaState.Default
                : new(
                    snapshot.VdoNinja.Active,
                    snapshot.VdoNinja.Connected,
                    snapshot.VdoNinja.LastPeerLatencyMs,
                    snapshot.VdoNinja.PeerCount,
                    snapshot.VdoNinja.PublishUrl ?? string.Empty,
                    snapshot.VdoNinja.RoomName ?? string.Empty,
                    snapshot.VdoNinja.StreamId ?? string.Empty),
            VdoNinjaActive: snapshot.VdoNinja?.Active == true,
            CameraDeviceId: snapshot.VideoDeviceId ?? string.Empty,
            MicrophoneDeviceId: snapshot.AudioDeviceId ?? string.Empty);
    }
}
