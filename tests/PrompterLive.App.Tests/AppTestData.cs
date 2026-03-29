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
        public static string TeleprompterSecurityIncident => AppRoutes.TeleprompterWithId(Scripts.SecurityIncidentId);
        public const string Settings = AppRoutes.Settings;
    }

    public static class Editor
    {
        public const string TestSpeaker = "Test Speaker";
        public const string CreatedDate = "2026-03-26";
        public const string Version = "2.0";
        public const string BodyHeading = "## [Intro|140WPM|warm]";
    }

    public static class Streaming
    {
        public const int BitrateKbps = 7200;
        public const string RtmpUrl = "rtmp://live.example.com/stream";
        public const string StreamKey = "sk-live-key";
    }
}
