using System.Text.RegularExpressions;
using PrompterLive.Shared.Contracts;

namespace PrompterLive.App.UITests;

internal static class BrowserTestConstants
{
    public static class Scripts
    {
        public const string DemoId = "rsvp-tech-demo";
        public const string LeadershipId = "ted-leadership";
        public const string QuantumId = "quantum-computing";
        public const string SecurityIncidentId = "security-incident";
        public const string ProductLaunchTitle = "Product Launch";
        public const string LeadershipTitle = "TED: Leadership";
        public const string QuantumTitle = "Quantum Computing";
        public const string SecurityIncidentTitle = "Security Incident";
    }

    public static class Folders
    {
        public const string PresentationsId = "presentations";
        public const string TedTalksId = "ted";
        public const string RoadshowsId = "roadshows";
        public const string RoadshowsName = "Roadshows";
        public const string TedTalksName = "TED Talks";
    }

    public static class Library
    {
        public const string SearchQuery = "Quantum";
        public const string ModeLabel = "Actor";
    }

    public static class Editor
    {
        public const string BodyHeading = "## [Intro|140WPM|warm]";
        public const string LegacyActiveBlockLabel = "ACTIVE BLOCK";
        public const string LegacyActiveSegmentLabel = "ACTIVE SEGMENT";
        public const string Welcome = "welcome";
        public const string TransformativeMoment = "transformative moment";
        public const string HighlightFragment = "[highlight]welcome[/highlight]";
        public const string EmphasisFragment = "[emphasis]welcome[/emphasis]";
        public const string GreenFragment = "[green]welcome[/green]";
        public const string ProfessionalFragment = "[professional]transformative moment[/professional]";
        public const string SlowFragment = "[slow]transformative moment[/slow]";
        public const string PauseFragment = "[pause:2s]";
        public const string CustomWpmToken = "[180WPM]";
        public const string PronunciationToken = "[pronunciation:guide]";
        public const string SegmentRewrite = "## [Launch Angle|305WPM|focused|1:00-2:00]";
        public const string BlockRewrite = "### [Signal Block|305WPM|professional]";
        public const string TypedTitle = "Typed Intro";
        public const string TypedBlock = "Typed Block";
        public const string TypedHighlight = "[highlight]Every word[/highlight]";
        public const string TypedScript = """
            ## [Typed Intro|175WPM|focused|0:05-0:20]
            ### [Typed Block|165WPM|professional]
            This is a typed TPS script. / [highlight]Every word[/highlight] stays in sync. //
            """;
    }

    public static class Streaming
    {
        public const string OutputModeDirectRtmp = "DirectRtmp";
        public const string ResolutionHd720 = "Hd720";
        public const string RtmpUrl = "rtmp://live.example.com/stream";
        public const string StreamKey = "sk-live-key";
        public const string BitrateKbps = "7200";
    }

    public static class Diagnostics
    {
        public const string ForcedFailureDetail = "Forced diagnostics failure from browser test.";
        public const string CreateFolderFailure = "Unable to create this folder.";
        public const string FolderStorageKey = "prompterlive.folders.v1";
    }

    public static class Timing
    {
        public const int FastVisibleTimeoutMs = 5_000;
        public const int DefaultVisibleTimeoutMs = 10_000;
        public const int ExtendedVisibleTimeoutMs = 15_000;
        public const int FloatingToolbarSettleDelayMs = 500;
        public const int LearnPlaybackDelayMs = 900;
        public const int ReaderPlaybackDelayMs = 2_500;
        public const int ReaderCameraInitDelayMs = 750;
        public const int PersistDelayMs = 800;
    }

    public static class Regexes
    {
        public static Regex ActiveClass { get; } = new(@"\bactive\b", RegexOptions.Compiled);
        public static Regex ToggleOnClass { get; } = new(@"\bon\b", RegexOptions.Compiled);
        public static Regex NonZeroWidth { get; } = new(@"width:\s*0%", RegexOptions.Compiled);
        public static Regex ReaderTimeNotZero { get; } = new(@"^0:00 /", RegexOptions.Compiled);
        public static Regex CameraAutoStart { get; } = new(@"true|false", RegexOptions.Compiled);
        public static Regex EndsWithPause { get; } = new(@"\[pause:2s\]\s*$", RegexOptions.Compiled);
    }

    public static class Keyboard
    {
        public const string SelectAll = "Meta+A";
        public const string Backspace = "Backspace";
        public const string Undo = "Meta+Z";
        public const string Redo = "Meta+Shift+Z";
    }

    public static class Routes
    {
        public static string Library => AppRoutes.Library;
        public static string Settings => AppRoutes.Settings;
        public static string EditorDemo => AppRoutes.EditorWithId(Scripts.DemoId);
        public static string EditorQuantum => AppRoutes.EditorWithId(Scripts.QuantumId);
        public static string LearnDemo => AppRoutes.LearnWithId(Scripts.DemoId);
        public static string LearnQuantum => AppRoutes.LearnWithId(Scripts.QuantumId);
        public static string TeleprompterDemo => AppRoutes.TeleprompterWithId(Scripts.DemoId);
        public static string TeleprompterSecurityIncident => AppRoutes.TeleprompterWithId(Scripts.SecurityIncidentId);
        public static string TeleprompterQuantum => AppRoutes.TeleprompterWithId(Scripts.QuantumId);

        public static string Pattern(string route) => string.Concat("**", route);
    }

    public static class Elements
    {
        public static string DemoCard => UiTestIds.Library.Card(Scripts.DemoId);
        public static string SecurityIncidentCard => UiTestIds.Library.Card(Scripts.SecurityIncidentId);
        public static string LeadershipCard => UiTestIds.Library.Card(Scripts.LeadershipId);
        public static string QuantumCard => UiTestIds.Library.Card(Scripts.QuantumId);
        public static string RoadshowsFolder => UiTestIds.Library.Folder(Folders.RoadshowsId);
        public static string TedTalksFolder => UiTestIds.Library.Folder(Folders.TedTalksId);
    }
}
