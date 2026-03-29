namespace PrompterLive.Shared.Contracts;

public static class UiTestIds
{
    public static class Header
    {
        public const string Center = "header-center";
        public const string Home = "header-home";
        public const string LibraryBreadcrumbCurrent = "header-library-breadcrumb-current";
        public const string LibraryNewScript = "header-library-new-script";
        public const string LibrarySearch = "library-search";
        public const string EditorLearn = "header-editor-learn";
        public const string EditorRead = "header-editor-read";
    }

    public static class Diagnostics
    {
        public const string Banner = "diagnostics-banner";
        public const string Dismiss = "diagnostics-dismiss";
        public const string Fatal = "diagnostics-fatal";
        public const string Home = "diagnostics-home";
        public const string Retry = "diagnostics-retry";
    }

    public static class Library
    {
        public const string Page = "library-page";
        public const string CreateScript = "library-create-script";
        public const string FolderAll = "library-folder-all";
        public const string FolderCreateStart = "library-folder-create-start";
        public const string FolderCreateTile = "library-folder-create-tile";
        public const string NewFolderCard = "library-new-folder-card";
        public const string NewFolderCancel = "library-new-folder-cancel";
        public const string NewFolderName = "library-new-folder-name";
        public const string NewFolderOverlay = "library-new-folder-overlay";
        public const string NewFolderParent = "library-new-folder-parent";
        public const string NewFolderSubmit = "library-new-folder-submit";
        public const string OpenSettings = "library-open-settings";
        public const string SortDate = "library-sort-date";
        public const string SortDuration = "library-sort-duration";
        public const string SortName = "library-sort-name";
        public const string SortWpm = "library-sort-wpm";

        public static string BreadcrumbCurrent(string folderId) => $"library-breadcrumb-{folderId}";

        public static string Card(string scriptId) => $"library-card-{scriptId}";

        public static string CardDuplicate(string scriptId) => $"library-card-duplicate-{scriptId}";

        public static string CardLearn(string scriptId) => $"library-card-learn-{scriptId}";

        public static string CardMenu(string scriptId) => $"library-card-menu-{scriptId}";

        public static string CardRead(string scriptId) => $"library-card-read-{scriptId}";

        public static string Folder(string folderId) => $"library-folder-{folderId}";

        public static string Move(string scriptId, string folderId) => $"library-move-{scriptId}-{folderId}";
    }

    public static class Editor
    {
        public const string ActiveBlockEmotion = "editor-active-block-emotion";
        public const string ActiveBlockName = "editor-active-block-name";
        public const string ActiveBlockWpm = "editor-active-block-wpm";
        public const string ActiveSegmentEmotion = "editor-active-segment-emotion";
        public const string ActiveSegmentName = "editor-active-segment-name";
        public const string ActiveSegmentTiming = "editor-active-segment-timing";
        public const string ActiveSegmentWpm = "editor-active-segment-wpm";
        public const string Ai = "editor-ai";
        public const string AiPanel = "editor-ai-panel";
        public const string AiActionExpand = "editor-ai-action-expand";
        public const string AiActionPauses = "editor-ai-action-pauses";
        public const string AiActionSimplify = "editor-ai-action-simplify";
        public const string Author = "editor-author";
        public const string BaseWpm = "editor-base-wpm";
        public const string Bold = "editor-bold";
        public const string ColorClear = "editor-color-clear";
        public const string ColorGreen = "editor-color-green";
        public const string ColorTrigger = "editor-color-trigger";
        public const string Created = "editor-created";
        public const string Duration = "editor-duration";
        public const string EmotionProfessional = "editor-emotion-professional";
        public const string EmotionTrigger = "editor-emotion-trigger";
        public const string Error = "editor-error";
        public const string FloatingAi = "editor-floating-ai";
        public const string FloatingBar = "editor-floating-bar";
        public const string FloatingPause = "editor-float-pause";
        public const string FloatingSlow = "editor-floating-slow";
        public const string FloatEmphasis = "editor-float-emphasis";
        public const string FormatHighlight = "editor-format-highlight";
        public const string FormatTrigger = "editor-format-trigger";
        public const string InsertBlock = "editor-insert-block";
        public const string InsertPronunciation = "editor-insert-pronunciation";
        public const string InsertTrigger = "editor-insert-trigger";
        public const string MenuColor = "editor-menu-color";
        public const string MenuEmotion = "editor-menu-emotion";
        public const string MenuFormat = "editor-menu-format";
        public const string MenuInsert = "editor-menu-insert";
        public const string MenuPause = "editor-menu-pause";
        public const string MenuSpeed = "editor-menu-speed";
        public const string Page = "editor-page";
        public const string PauseTrigger = "editor-pause-trigger";
        public const string PauseTwoSeconds = "editor-pause-two-seconds";
        public const string Profile = "editor-profile";
        public const string Redo = "editor-redo";
        public const string SourceHighlight = "editor-source-highlight";
        public const string SourceInput = "editor-source-input";
        public const string SpeedFast = "editor-speed-fast";
        public const string SpeedCustomWpm = "editor-speed-custom-wpm";
        public const string SpeedSlow = "editor-speed-slow";
        public const string SpeedTrigger = "editor-speed-trigger";
        public const string SpeedXfast = "editor-speed-xfast";
        public const string SpeedXslow = "editor-speed-xslow";
        public const string Undo = "editor-undo";
        public const string Version = "editor-version";

        public static string BlockNavigation(int segmentIndex, int blockIndex) => $"editor-structure-block-{segmentIndex}-{blockIndex}";

        public static string SegmentNavigation(int segmentIndex) => $"editor-structure-segment-{segmentIndex}";
    }

    public static class Learn
    {
        public const string ContextLeft = "learn-context-left";
        public const string ContextRight = "learn-context-right";
        public const string NextPhrase = "learn-next-phrase";
        public const string OrpLine = "learn-orp-line";
        public const string Page = "learn-page";
        public const string PlayToggle = "learn-play-toggle";
        public const string SpeedDown = "learn-speed-down";
        public const string SpeedUp = "learn-speed-up";
        public const string StepBackward = "learn-step-backward";
        public const string StepBackwardLarge = "learn-step-backward-large";
        public const string StepForward = "learn-step-forward";
        public const string StepForwardLarge = "learn-step-forward-large";
        public const string Word = "learn-word";
    }

    public static class Teleprompter
    {
        public const string Back = "teleprompter-back";
        public const string CameraBackground = "teleprompter-camera-layer-primary";
        public const string CameraToggle = "teleprompter-camera-toggle";
        public const string EdgeSection = "teleprompter-edge-section";
        public const string FocalSlider = "teleprompter-focal-slider";
        public const string FontDown = "teleprompter-font-down";
        public const string FontUp = "teleprompter-font-up";
        public const string NextBlock = "teleprompter-next-block";
        public const string NextWord = "teleprompter-next-word";
        public const string Page = "teleprompter-page";
        public const string PlayToggle = "teleprompter-play-toggle";
        public const string PreviousBlock = "teleprompter-previous-block";
        public const string PreviousWord = "teleprompter-previous-word";
        public const string WidthSlider = "teleprompter-width-slider";

        public static string Card(int index) => $"teleprompter-card-{index}";

        public static string CardGroup(int cardIndex, int groupIndex) => $"teleprompter-card-group-{cardIndex}-{groupIndex}";

        public static string CardGroupPrefix(int cardIndex) => $"teleprompter-card-group-{cardIndex}-";

        public static string CardText(int index) => $"{Card(index)}-text";
    }

    public static class Settings
    {
        public const string AboutPanel = "settings-about-panel";
        public const string AppearancePanel = "settings-appearance-panel";
        public const string AiPanel = "settings-ai-panel";
        public const string CameraMirrorToggle = "settings-camera-mirror-toggle";
        public const string CameraResolution = "settings-camera-resolution";
        public const string Bitrate = "settings-bitrate";
        public const string CamerasPanel = "settings-cameras-panel";
        public const string CloudPanel = "settings-cloud-panel";
        public const string DefaultCamera = "settings-default-camera";
        public const string EchoCancellation = "settings-echo-cancellation";
        public const string FileAutoSave = "settings-file-autosave";
        public const string FileBackupCopies = "settings-file-backup-copies";
        public const string FilesPanel = "settings-files-panel";
        public const string MicLevel = "settings-mic-level";
        public const string MicLevelValue = "settings-mic-level-value";
        public const string MicsPanel = "settings-mics-panel";
        public const string NavAbout = "settings-nav-about";
        public const string NavAi = "settings-nav-ai";
        public const string NavAppearance = "settings-nav-appearance";
        public const string NavCameras = "settings-nav-cameras";
        public const string NavCloud = "settings-nav-cloud";
        public const string NavFiles = "settings-nav-files";
        public const string NavMics = "settings-nav-mics";
        public const string NavStreaming = "settings-nav-streaming";
        public const string NoiseSuppression = "settings-noise-suppression";
        public const string OutputMode = "settings-output-mode";
        public const string OutputResolution = "settings-output-resolution";
        public const string Page = "settings-page";
        public const string PrimaryMic = "settings-primary-mic";
        public const string ReaderCameraToggle = "settings-reader-camera-toggle";
        public const string RequestMedia = "settings-request-media";
        public const string RtmpUrl = "settings-rtmp-url";
        public const string NoCameras = "settings-no-cameras";
        public const string NoMics = "settings-no-mics";
        public const string StreamingPanel = "settings-streaming-panel";
        public const string StreamIncludeCamera = "settings-stream-include-camera";
        public const string StreamKey = "settings-stream-key";
        public const string StreamTextOverlay = "settings-stream-text-overlay";
        public const string TestConnection = "settings-test-connection";

        public static string AiProvider(string providerId) => $"settings-ai-provider-{providerId}";

        public static string CameraDevice(string deviceId) => $"settings-camera-device-{deviceId}";

        public static string MicDelay(string deviceId) => $"settings-mic-delay-{deviceId}";

        public static string MicDevice(string deviceId) => $"settings-mic-device-{deviceId}";

        public static string SceneCamera(string sourceId) => $"settings-scene-camera-{sourceId}";

        public static string SceneFlip(string sourceId) => $"settings-scene-flip-{sourceId}";

        public static string SceneMirror(string sourceId) => $"settings-scene-mirror-{sourceId}";

        public static string StreamProvider(string providerId) => $"settings-stream-provider-{providerId}";
    }
}
