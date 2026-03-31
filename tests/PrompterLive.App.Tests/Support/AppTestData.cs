using PrompterLive.Shared.Contracts;

namespace PrompterLive.App.Tests;

internal static class AppTestData
{
    public static class Scripts
    {
        public const string DemoId = "test-product-launch-script";
        public const string LeadershipId = "test-ted-leadership-script";
        public const string ArchitectureId = "test-green-architecture-script";
        public const string QuantumId = "test-quantum-computing-script";
        public const string SecurityIncidentId = "test-security-incident-script";
        public const string DemoTitle = "Product Launch";
        public const string TedLeadershipTitle = "TED: Leadership";
        public const string GreenArchitectureTitle = "Green Architecture";
        public const string QuantumTitle = "Quantum Computing";
        public const string SecurityIncidentTitle = "Security Incident";
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
        public static string LearnQuantum => AppRoutes.LearnWithId(Scripts.QuantumId);
        public static string TeleprompterQuantum => AppRoutes.TeleprompterWithId(Scripts.QuantumId);
        public static string TeleprompterSecurityIncident => AppRoutes.TeleprompterWithId(Scripts.SecurityIncidentId);
        public const string Settings = AppRoutes.Settings;
    }

    public static class Camera
    {
        public const string AttachCameraInvocation = "BrowserMediaInterop.attachCamera";
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
        public const string StartLevelMonitorInvocation = "BrowserMediaInterop.startMicrophoneLevelMonitor";
        public const string StopLevelMonitorInvocation = "BrowserMediaInterop.stopMicrophoneLevelMonitor";
    }

    public static class Theme
    {
        public const string ApplySettingsInvocation = "prompterLiveTheme.applySettingsTheme";
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
        public const string MicChannelId = "mic";
        public const string PrimaryParticipantId = "host";
        public const string PrimaryParticipantName = "Host";
        public const string PrimaryParticipantRole = "Local program";
        public const string PrompterUtilitySourceId = "prompter-display";
        public const string SessionTimerPrefix = "00:02:";
        public const string TwitchUrl = "rtmp://live.twitch.tv/app";
        public const string TwitchKey = "live_twitch_key";
        public const string YoutubeUrl = "rtmps://a.rtmp.youtube.com/live2";
        public const string YoutubeKey = "youtube_stream_key";
    }
}
