using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Settings.Models;

namespace PrompterOne.Shared.Services;

public static class GoLiveOutputRequestFactory
{
    private const int MonoChannelCount = 1;
    private const int StereoChannelCount = 2;
    private const int SampleRateKhzMultiplier = 1000;
    private const string FrameRate30Label = "30 FPS";
    private const string FrameRate60Label = "60 FPS";
    private const string ResolutionFullHdLabel = "1920 × 1080";
    private const string ResolutionHdLabel = "1280 × 720";
    private const string ResolutionUltraHdLabel = "3840 × 2160";

    public static GoLiveOutputRuntimeRequest Build(
        SceneCameraSource? primaryCamera,
        MediaSceneState scene,
        StreamStudioSettings streaming,
        SettingsPagePreferences recordingPreferences,
        string recordingFileStem)
    {
        var liveKitDestination = ResolveLiveKitDestination(streaming);
        var programVideo = ResolveProgramVideo(streaming.OutputResolution);
        var videoSources = BuildVideoSources(primaryCamera, scene.Cameras);
        var audioInputs = BuildAudioInputs(scene);
        var recording = BuildRecordingExport(recordingPreferences, recordingFileStem);

        return new(
            PrimarySourceId: primaryCamera?.SourceId ?? string.Empty,
            ProgramVideo: programVideo,
            VideoSources: videoSources,
            AudioInputs: audioInputs,
            Recording: recording,
            ObsEnabled: streaming.ObsVirtualCameraEnabled,
            RecordingEnabled: streaming.LocalRecordingEnabled,
            LiveKitEnabled: liveKitDestination?.IsEnabled == true,
            LiveKitServerUrl: liveKitDestination?.ServerUrl ?? string.Empty,
            LiveKitRoomName: liveKitDestination?.RoomName ?? string.Empty,
            LiveKitToken: liveKitDestination?.Token ?? string.Empty);
    }

    private static StreamingProfile? ResolveLiveKitDestination(StreamStudioSettings streaming)
    {
        var destinations = streaming.ExternalDestinations ?? Array.Empty<StreamingProfile>();
        var configuredDestination = destinations.FirstOrDefault(destination =>
                destination.ProviderKind == StreamingProviderKind.LiveKit
                && destination.IsEnabled)
            ?? destinations.FirstOrDefault(destination =>
                destination.ProviderKind == StreamingProviderKind.LiveKit);

        if (configuredDestination is not null)
        {
            return configuredDestination;
        }

        if (!streaming.LiveKitEnabled
            && string.IsNullOrWhiteSpace(streaming.LiveKitServerUrl)
            && string.IsNullOrWhiteSpace(streaming.LiveKitRoomName)
            && string.IsNullOrWhiteSpace(streaming.LiveKitToken))
        {
            return null;
        }

        return StreamingPlatformCatalog.CreateProfile(
            StreamingPlatformKind.LiveKit,
            GoLiveTargetCatalog.TargetIds.LiveKit) with
        {
            IsEnabled = streaming.LiveKitEnabled,
            ServerUrl = streaming.LiveKitServerUrl,
            RoomName = streaming.LiveKitRoomName,
            Token = streaming.LiveKitToken
        };
    }

    private static IReadOnlyList<GoLiveOutputAudioInput> BuildAudioInputs(MediaSceneState scene)
    {
        var audioInputs = scene.AudioBus.Inputs
            .Select(input => new GoLiveOutputAudioInput(
                DeviceId: input.DeviceId,
                Label: input.Label,
                DelayMs: input.DelayMs,
                Gain: input.Gain,
                IsMuted: input.IsMuted,
                RouteTarget: input.RouteTarget,
                IsPrimary: string.Equals(input.DeviceId, scene.PrimaryMicrophoneId, StringComparison.Ordinal)))
            .ToList();

        if (!string.IsNullOrWhiteSpace(scene.PrimaryMicrophoneId)
            && audioInputs.All(input => !string.Equals(input.DeviceId, scene.PrimaryMicrophoneId, StringComparison.Ordinal)))
        {
            audioInputs.Insert(
                0,
                new GoLiveOutputAudioInput(
                    DeviceId: scene.PrimaryMicrophoneId,
                    Label: scene.PrimaryMicrophoneLabel ?? string.Empty,
                    DelayMs: 0,
                    Gain: 1.0,
                    IsMuted: false,
                    RouteTarget: AudioRouteTarget.Both,
                    IsPrimary: true));
        }

        return audioInputs;
    }

    private static GoLiveRecordingExportSettings BuildRecordingExport(
        SettingsPagePreferences recordingPreferences,
        string recordingFileStem)
    {
        return new(
            FileStem: recordingFileStem,
            PreferFilePicker: true,
            ContainerLabel: recordingPreferences.RecordingContainer,
            VideoCodecLabel: recordingPreferences.RecordingVideoCodec,
            AudioCodecLabel: recordingPreferences.RecordingAudioCodec,
            VideoBitrateKbps: recordingPreferences.RecordingVideoBitrateKbps,
            AudioBitrateKbps: recordingPreferences.RecordingAudioBitrateKbps,
            AudioSampleRate: ResolveAudioSampleRate(recordingPreferences.RecordingAudioSampleRate),
            AudioChannelCount: ResolveAudioChannelCount(recordingPreferences.RecordingAudioChannels));
    }

    private static GoLiveProgramVideoSettings ResolveProgramVideo(StreamingResolutionPreset resolution)
    {
        return resolution switch
        {
            StreamingResolutionPreset.FullHd1080p60 => new(1920, 1080, 60, ResolutionFullHdLabel, FrameRate60Label),
            StreamingResolutionPreset.Hd720p30 => new(1280, 720, 30, ResolutionHdLabel, FrameRate30Label),
            StreamingResolutionPreset.UltraHd2160p30 => new(3840, 2160, 30, ResolutionUltraHdLabel, FrameRate30Label),
            _ => new(1920, 1080, 30, ResolutionFullHdLabel, FrameRate30Label)
        };
    }

    private static IReadOnlyList<GoLiveOutputVideoSource> BuildVideoSources(
        SceneCameraSource? primaryCamera,
        IReadOnlyList<SceneCameraSource> cameras)
    {
        return cameras.Select(camera => new GoLiveOutputVideoSource(
                SourceId: camera.SourceId,
                DeviceId: camera.DeviceId,
                Label: camera.Label,
                Transform: new GoLiveOutputSourceTransform(
                    camera.Transform.X,
                    camera.Transform.Y,
                    camera.Transform.Width,
                    camera.Transform.Height,
                    camera.Transform.Rotation,
                    camera.Transform.MirrorHorizontal,
                    camera.Transform.MirrorVertical,
                    camera.Transform.Visible,
                    camera.Transform.IncludeInOutput,
                    camera.Transform.ZIndex,
                    camera.Transform.Opacity),
                IsPrimary: string.Equals(camera.SourceId, primaryCamera?.SourceId, StringComparison.Ordinal)))
            .ToList();
    }

    private static int ResolveAudioChannelCount(string channelLabel)
    {
        return string.Equals(channelLabel, RecordingPreferenceCatalog.AudioChannels.Mono, StringComparison.Ordinal)
            ? MonoChannelCount
            : StereoChannelCount;
    }

    private static int ResolveAudioSampleRate(string sampleRateLabel)
    {
        return sampleRateLabel switch
        {
            RecordingPreferenceCatalog.SampleRates.Khz44_1 => 44100,
            RecordingPreferenceCatalog.SampleRates.Khz96 => 96000,
            _ => 48 * SampleRateKhzMultiplier
        };
    }
}
