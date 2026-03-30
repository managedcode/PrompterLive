using PrompterLive.Shared.Contracts;

namespace PrompterLive.App.Tests;

internal static class AppTestData
{
    public static class Scripts
    {
        public const string DemoId = "rsvp-tech-demo";
        public const string QuantumId = "quantum-computing";
        public const string SecurityIncidentId = "security-incident";
        public const string DemoTitle = "Product Launch";
        public const string TedLeadershipTitle = "TED: Leadership";
        public const string BroadcastMic = "Broadcast mic";
    }

    public static class Folders
    {
        public const string PresentationsId = "presentations";
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
        public const string StartLevelMonitorInvocation = "BrowserMediaInterop.startMicrophoneLevelMonitor";
        public const string StopLevelMonitorInvocation = "BrowserMediaInterop.stopMicrophoneLevelMonitor";
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
        public const string LiveKitRoom = "launch-room";
        public const string LiveKitServer = "wss://livekit.example.com";
        public const string LiveKitToken = "lk-test-token";
        public const string TwitchUrl = "rtmp://live.twitch.tv/app";
        public const string TwitchKey = "live_twitch_key";
        public const string YoutubeUrl = "rtmps://a.rtmp.youtube.com/live2";
        public const string YoutubeKey = "youtube_stream_key";
    }
}
