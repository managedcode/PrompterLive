namespace PrompterOne.Shared.Contracts;

public static class UiDomIds
{
    public static class Tooltip
    {
        public static string Surface(string ownerDomId) => $"{ownerDomId}-tooltip";
    }

    public static class AppShell
    {
        public const string EditorImportScriptInput = "app-shell-editor-import-script-input";
        public const string LibraryOpenScriptInput = "app-shell-library-open-script-input";
        public const string OnboardingEyebrow = "app-shell-onboarding-eyebrow";
        public const string OnboardingTitle = "app-shell-onboarding-title";
    }

    public static class Diagnostics
    {
        public const string ConnectivityDismiss = "app-connectivity-dismiss";
        public const string ConnectivityMessage = "app-connectivity-message";
        public const string ConnectivityRetry = "app-connectivity-retry";
        public const string ConnectivityTitle = "app-connectivity-title";
        public const string ConnectivityUi = "app-connectivity-ui";
    }

    public static class Design
    {
        public const string LearnScreen = "screen-rsvp";
        public const string TeleprompterScreen = "screen-teleprompter";
    }

    public static class Settings
    {
        public const string CameraPreviewVideo = "settings-camera-preview-video";
        public const string MicrophoneLevelMonitor = "settings-microphone-level-monitor";

        public static string MicrophoneLevelMonitorForDevice(string deviceId) =>
            $"{MicrophoneLevelMonitor}-{deviceId}";
    }

    public static class Editor
    {
        public const string GraphPanel = "editor-graph-rail-panel";
        public const string GraphTab = "editor-graph-rail-tab";
        public const string MetadataRailBody = "editor-metadata-rail-body";
        public const string MetadataPanel = "editor-metadata-panel";
        public const string MetadataTab = "editor-metadata-tab";
        public const string ToolsPanel = "editor-tools-panel";
        public const string ToolsTab = "editor-tools-tab";
    }

    public static class GoLive
    {
        public const string MicrophoneLevelMonitor = "go-live-microphone-level-monitor";
        public const string ProgramStage = "go-live-program-stage";
        public const string ProgramVideo = "go-live-program-video";
        public const string PreviewCard = "go-live-preview-card";
        public const string PreviewStage = "go-live-preview-stage";
        public const string PreviewEmpty = "go-live-preview-empty";
        public const string PreviewVideo = "go-live-preview-video";

        public static string SourceVideo(string sourceId) => $"go-live-source-video-{sourceId}";
    }

    public static class Learn
    {
        public const string HeaderWpmBadge = "rsvp-wpm-badge";
        public const string ContextLeft = "rsvp-ctx-l";
        public const string ContextRight = "rsvp-ctx-r";
        public const string NextPhrase = "rsvp-next-phrase";
        public const string NotesBody = "rsvp-notes-body";
        public const string PauseFill = "rsvp-pause-fill";
        public const string ProgressLabel = "rsvp-progress-label";
        public const string ProgressFill = "rsvp-progress-fill";
        public const string Speed = "rsvp-speed";
        public const string Word = "rsvp-word";
        public const string WordShell = "rsvp-word-shell";
    }

    public static class Teleprompter
    {
        public const string BlockIndicator = "rd-block-indicator";
        public const string Camera = "rd-camera";
        public const string CameraButton = "rd-cam-btn";
        public const string CameraTint = "rd-camera-tint";
        public const string ClusterWrap = "rd-cluster-wrap";
        public const string Countdown = "rd-countdown";
        public const string FontLabel = "rd-font-label";
        public const string FontSlider = "rd-font-slider";
        public const string FontValue = "rd-font-val";
        public const string FocalGuide = "rd-guide-h";
        public const string HeaderSegment = "rd-header-segment";
        public const string ProgressFill = "rd-progress-fill";
        public const string SpeedValue = "rd-speed-val";
        public const string Stage = "rd-stage";
        public const string Time = "rd-time";
        public const string WidthGuideLeft = "rd-guide-v-l";
        public const string WidthGuideRight = "rd-guide-v-r";
        public const string WidthValue = "rd-width-val";

        public static string CardText(int index) => $"rd-card-text-{index}";

        public static string FocusLens(int cardIndex) => $"rd-focus-lens-{cardIndex}";

        public static string CardChunk(int cardIndex, int chunkIndex) =>
            $"rd-card-chunk-{cardIndex}-{chunkIndex}";

        public static string CardWord(int cardIndex, int groupIndex, int wordIndex) =>
            $"rd-card-word-{cardIndex}-{groupIndex}-{wordIndex}";

        public static string CameraOverlay(int order) => $"rd-camera-overlay-{order}";

        public static string RailTooltip(string key) => $"rd-tooltip-{key}";
    }
}
