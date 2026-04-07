using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services;

namespace PrompterOne.Web.Tests;

internal static class AppTestData
{
    public static class Scripts
    {
        public const string DemoId = "test-product-launch-script";
        public const string LeadershipId = "test-ted-leadership-script";
        public const string ArchitectureId = "test-green-architecture-script";
        public const string LearnWpmBoundaryId = "test-learn-wpm-boundary-script";
        public const string QuantumId = "test-quantum-computing-script";
        public const string ReaderTimingId = "test-reader-timing-script";
        public const string SecurityIncidentId = "test-security-incident-script";
        public const string SpeedOffsetsId = "test-tps-speed-offsets-script";
        public const string DemoTitle = "Product Launch";
        public const string TedLeadershipTitle = "TED: Leadership";
        public const string GreenArchitectureTitle = "Green Architecture";
        public const string LearnWpmBoundaryTitle = "Learn WPM Boundary Probe";
        public const string QuantumTitle = "Quantum Computing";
        public const string ReaderTimingTitle = "Reader Timing Probe";
        public const string SecurityIncidentTitle = "Security Incident";
        public const string SpeedOffsetsTitle = "TPS Speed Offsets";
        public const string BroadcastMic = "Broadcast mic";
    }

    public static class Folders
    {
        public const string PresentationsId = "test-presentations";
        public const string PresentationsName = "Presentations";
        public const string ProductId = "test-product";
        public const string ProductName = "Product";
        public const string TedTalksId = "test-ted-talks";
        public const string TedTalksName = "TED Talks";
        public const string NewsReportsId = "test-news-reports";
        public const string NewsReportsName = "News Reports";
        public const string InvestorsId = "test-investors";
        public const string InvestorsName = "Investors";
        public const string InternalId = "test-internal";
        public const string InternalName = "Internal";
        public const string RoadshowsId = "roadshows";
        public const string Roadshows = "Roadshows";
    }

    public static class Routes
    {
        public static string EditorDemo => AppRoutes.EditorWithId(Scripts.DemoId);
        public static string EditorQuantum => AppRoutes.EditorWithId(Scripts.QuantumId);
        public static string GoLiveDemo => AppRoutes.GoLiveWithId(Scripts.DemoId);
        public static string GoLiveLeadership => AppRoutes.GoLiveWithId(Scripts.LeadershipId);
        public static string LearnWpmBoundary => AppRoutes.LearnWithId(Scripts.LearnWpmBoundaryId);
        public static string LearnReaderTiming => AppRoutes.LearnWithId(Scripts.ReaderTimingId);
        public static string LearnQuantum => AppRoutes.LearnWithId(Scripts.QuantumId);
        public static string TeleprompterArchitecture => AppRoutes.TeleprompterWithId(Scripts.ArchitectureId);
        public static string TeleprompterDemo => AppRoutes.TeleprompterWithId(Scripts.DemoId);
        public static string TeleprompterQuantum => AppRoutes.TeleprompterWithId(Scripts.QuantumId);
        public static string TeleprompterReaderTiming => AppRoutes.TeleprompterWithId(Scripts.ReaderTimingId);
        public static string TeleprompterSecurityIncident => AppRoutes.TeleprompterWithId(Scripts.SecurityIncidentId);
        public static string TeleprompterSpeedOffsets => AppRoutes.TeleprompterWithId(Scripts.SpeedOffsetsId);
        public const string Settings = AppRoutes.Settings;
    }

    public static class Camera
    {
        public const string AttachCameraInvocation = BrowserMediaInteropMethodNames.AttachCamera;
        public const string FirstDeviceId = "cam-1";
        public const string FirstSourceId = "scene-cam-a";
        public const string FrontCamera = "Front camera";
        public const string FrameRateFps24 = "Fps24";
        public const string SecondDeviceId = "cam-2";
        public const string SecondSourceId = "scene-cam-b";
        public const string SideCamera = "Side camera";
        public const string MicrophoneOnlyId = "mic-only-1";
        public const string MicrophoneOnlyLabel = "Microphone only";
    }

    public static class Microphone
    {
        public const string PrimaryDeviceId = "mic-1";
        public const string StartLevelMonitorInvocation = BrowserMediaInteropMethodNames.StartMicrophoneLevelMonitor;
        public const string StopLevelMonitorInvocation = BrowserMediaInteropMethodNames.StopMicrophoneLevelMonitor;
    }

    public static class Theme
    {
        public const string ApplySettingsInvocation = "prompterOneTheme.applySettingsTheme";
        public const string LightColorScheme = "light";
    }

    public static class About
    {
        public const string BuildNumber = "777";
        public const string Version = "0.1.777";
        public const string VersionSubtitle = "Version 0.1.777 · Build 777";
    }

    public static class Editor
    {
        public const string TestSpeaker = "Test Speaker";
        public const string CreatedDate = "2026-03-26";
        public const string DisplayDuration = "12:34";
        public const string Version = "2.0";
        public const string BodyHeading = "## [Intro|140WPM|warm]";
    }

    public static class Streaming
    {
        public const int BitrateKbps = 7200;
        public const string RtmpUrl = "rtmp://live.example.com/stream";
        public const string StreamKey = "sk-live-key";
    }

    public static class GoLive
    {
        public const string LegacyNetworkUploadMetric = "8.2 Mbps";
        public const string LiveKitRoom = "launch-room";
        public const string LiveKitServer = "wss://livekit.example.com";
        public const string LiveKitToken = "lk-test-token";
        public const string RemoteLiveKitConnectionId = "livekit-guests";
        public const string RemoteLiveKitSourceId = "livekit-guests:guest-one";
        public const string RemoteLiveKitSourceLabel = "Guest One";
        public const string MicChannelId = "mic";
        public const string PrimaryParticipantId = "host";
        public const string PrimaryParticipantName = "Host";
        public const string PrimaryParticipantRole = "Local program";
        public const string PrompterUtilitySourceId = "prompter-display";
        public const string SessionTimerPrefix = "00:02:";
        public const string TwitchUrl = "rtmp://live.twitch.tv/app";
        public const string TwitchKey = "live_twitch_key";
        public const string RemoteVdoConnectionId = "vdo-guests";
        public const string RemoteVdoSourceId = "vdo-guests:guest-two";
        public const string RemoteVdoSourceLabel = "Guest Two";
        public const string VdoNinjaRoom = "launch-room";
        public const string VdoNinjaPublishUrl = "https://vdo.ninja/?room=launch-room&push=prompterone-program";
        public const string YoutubeUrl = "rtmps://a.rtmp.youtube.com/live2";
        public const string YoutubeKey = "youtube_stream_key";

        public static TransportConnectionProfile CreateLiveKitConnection(
            bool isEnabled = true,
            string connectionId = GoLiveTargetCatalog.TargetIds.LiveKit) =>
            new(
                Id: connectionId,
                Name: "LiveKit",
                PlatformKind: StreamingPlatformKind.LiveKit,
                IsEnabled: isEnabled,
                ServerUrl: LiveKitServer,
                RoomName: LiveKitRoom,
                Token: LiveKitToken);

        public static TransportConnectionProfile CreateLiveKitSourceConnection(
            bool isEnabled = true,
            string connectionId = RemoteLiveKitConnectionId) =>
            CreateLiveKitConnection(isEnabled, connectionId) with
            {
                Roles = StreamingTransportRole.Source
            };

        public static DistributionTargetProfile CreateYoutubeTarget(
            bool isEnabled = true,
            string targetId = GoLiveTargetCatalog.TargetIds.Youtube,
            IReadOnlyList<string>? boundTransportConnectionIds = null) =>
            new(
                Id: targetId,
                Name: "YouTube Live",
                PlatformKind: StreamingPlatformKind.Youtube,
                IsEnabled: isEnabled,
                RtmpUrl: YoutubeUrl,
                StreamKey: YoutubeKey,
                BoundTransportConnectionIds: boundTransportConnectionIds ?? Array.Empty<string>());

        public static TransportConnectionProfile CreateVdoNinjaConnection(
            bool isEnabled = true,
            string connectionId = GoLiveTargetCatalog.TargetIds.VdoNinja) =>
            new(
                Id: connectionId,
                Name: "VDO.Ninja",
                PlatformKind: StreamingPlatformKind.VdoNinja,
                IsEnabled: isEnabled,
                BaseUrl: VdoNinjaDefaults.HostedBaseUrl,
                RoomName: VdoNinjaRoom,
                PublishUrl: VdoNinjaPublishUrl);

        public static TransportConnectionProfile CreateVdoNinjaSourceConnection(
            bool isEnabled = true,
            string connectionId = RemoteVdoConnectionId) =>
            CreateVdoNinjaConnection(isEnabled, connectionId) with
            {
                Roles = StreamingTransportRole.Source,
                PublishUrl = string.Empty,
                ViewUrl = $"https://vdo.ninja/?view={connectionId}"
            };
    }
}
