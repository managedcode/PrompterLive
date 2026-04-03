using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Shared.Contracts;

public static class UiTestIds
{
    public static class Header
    {
        public const string Back = "header-back";
        public const string Brand = "header-brand";
        public const string Center = "header-center";
        public const string EditorLearn = "header-editor-learn";
        public const string EditorRead = "header-editor-read";
        public const string GoLive = "header-go-live";
        public const string Home = "header-home";
        public const string LibraryBreadcrumbCurrent = "header-library-breadcrumb-current";
        public const string LibraryNewScript = "header-library-new-script";
        public const string LibrarySearch = "library-search";
        public const string LiveWidget = "header-go-live-widget";
        public const string LiveWidgetPreview = "header-go-live-widget-preview";
        public const string Subtitle = "header-subtitle";
        public const string Title = "header-title";
        public const string Wpm = "header-wpm";
    }

    public static class Diagnostics
    {
        public const string Banner = "diagnostics-banner";
        public const string Bootstrap = "diagnostics-bootstrap";
        public const string BootstrapDismiss = "diagnostics-bootstrap-dismiss";
        public const string BootstrapReload = "diagnostics-bootstrap-reload";
        public const string Connectivity = "diagnostics-connectivity";
        public const string ConnectivityDismiss = "diagnostics-connectivity-dismiss";
        public const string ConnectivityRetry = "diagnostics-connectivity-retry";
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
        public const string FolderChips = "library-folder-chips";
        public const string FolderCreateStart = "library-folder-create-start";
        public const string FolderCreateTile = "library-folder-create-tile";
        public const string NewFolderCard = "library-new-folder-card";
        public const string NewFolderCancel = "library-new-folder-cancel";
        public const string NewFolderName = "library-new-folder-name";
        public const string NewFolderOverlay = "library-new-folder-overlay";
        public const string NewFolderParent = "library-new-folder-parent";
        public const string NewFolderSubmit = "library-new-folder-submit";
        public const string NewFolderTitle = "library-new-folder-title";
        public const string OpenSettings = "library-open-settings";
        public const string SectionFoldersTitle = "library-section-folders-title";
        public const string SortLabel = "library-sort-label";
        public const string SortDate = "library-sort-date";
        public const string SortDuration = "library-sort-duration";
        public const string SortName = "library-sort-name";
        public const string SortWpm = "library-sort-wpm";

        public static string BreadcrumbCurrent(string folderId) => $"library-breadcrumb-{folderId}";

        public static string Card(string scriptId) => $"library-card-{scriptId}";

        public static string CardDuplicate(string scriptId) => $"library-card-duplicate-{scriptId}";

        public static string CardLearn(string scriptId) => $"library-card-learn-{scriptId}";

        public static string CardMenu(string scriptId) => $"library-card-menu-{scriptId}";

        public static string CardMenuDropdown(string scriptId) => $"library-card-menu-dropdown-{scriptId}";

        public static string CardRead(string scriptId) => $"library-card-read-{scriptId}";

        public static string Folder(string folderId) => $"library-folder-{folderId}";

        public static string FolderChip(string folderId) => $"library-folder-chip-{folderId}";

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
        public const string Author = "editor-author";
        public const string BaseWpm = "editor-base-wpm";
        public const string Bold = "editor-bold";
        public const string ColorClear = "editor-color-clear";
        public const string ColorGreen = "editor-color-green";
        public const string ColorTrigger = "editor-color-trigger";
        public const string Created = "editor-created";
        public const string CreatedIcon = "editor-created-icon";
        public const string Duration = "editor-duration";
        public const string EmotionProfessional = "editor-emotion-professional";
        public const string EmotionTrigger = "editor-emotion-trigger";
        public const string Error = "editor-error";
        public const string FloatingAi = "editor-floating-ai";
        public const string FloatingBar = "editor-floating-bar";
        public const string FloatingEmotion = "editor-float-emotion";
        public const string FloatingEmotionMenu = "editor-floating-emotion-menu";
        public const string FloatingEmotionProfessional = "editor-float-emotion-professional";
        public const string FloatingPause = "editor-float-pause";
        public const string FloatingSlow = "editor-floating-slow";
        public const string FloatEmphasis = "editor-float-emphasis";
        public const string FormatHighlight = "editor-format-highlight";
        public const string FormatTrigger = "editor-format-trigger";
        public const string InsertBlock = "editor-insert-block";
        public const string InsertBlockMenu = "editor-insert-block-menu";
        public const string InsertSegment = "editor-insert-segment";
        public const string InsertSegmentMenu = "editor-insert-segment-menu";
        public const string InsertPronunciation = "editor-insert-pronunciation";
        public const string InsertTrigger = "editor-insert-trigger";
        public const string MenuColor = "editor-menu-color";
        public const string MenuEmotion = "editor-menu-emotion";
        public const string MenuFormat = "editor-menu-format";
        public const string MenuInsert = "editor-menu-insert";
        public const string MenuPause = "editor-menu-pause";
        public const string MenuSpeed = "editor-menu-speed";
        public const string MainPanel = "editor-main-panel";
        public const string MetadataRail = "editor-metadata-rail";
        public const string Page = "editor-page";
        public const string PauseTrigger = "editor-pause-trigger";
        public const string PauseTwoSeconds = "editor-pause-two-seconds";
        public const string Profile = "editor-profile";
        public const string Redo = "editor-redo";
        public const string SourceHighlight = "editor-source-highlight";
        public const string SourceInput = "editor-source-input";
        public const string SourceScrollHost = "editor-source-scroll-host";
        public const string SourceStage = "editor-source-stage";
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
        public const string Display = "learn-display";
        public const string LoopToggle = "learn-loop-toggle";
        public const string NextPhrase = "learn-next-phrase";
        public const string OrpLine = "learn-orp-line";
        public const string Page = "learn-page";
        public const string PlayToggle = "learn-play-toggle";
        public const string ProgressLabel = "learn-progress-label";
        public const string SpeedDown = "learn-speed-down";
        public const string SpeedUp = "learn-speed-up";
        public const string StepBackward = "learn-step-backward";
        public const string StepBackwardLarge = "learn-step-backward-large";
        public const string StepForward = "learn-step-forward";
        public const string StepForwardLarge = "learn-step-forward-large";
        public const string Word = "learn-word";
        public const string WordShell = "learn-word-shell";
    }

    public static class Teleprompter
    {
        public const string Back = "teleprompter-back";
        public const string CameraBackground = "teleprompter-camera-layer-primary";
        public const string CameraToggle = "teleprompter-camera-toggle";
        public const string ClusterWrap = "teleprompter-cluster-wrap";
        public const string Controls = "teleprompter-controls";
        public const string EdgeSection = "teleprompter-edge-section";
        public const string EdgeInfo = "teleprompter-edge-info";
        public const string FocalSlider = "teleprompter-focal-slider";
        public const string FocalGuide = "teleprompter-focal-guide";
        public const string FontDown = "teleprompter-font-down";
        public const string FontUp = "teleprompter-font-up";
        public const string FullscreenToggle = "teleprompter-fullscreen-toggle";
        public const string MirrorControls = "teleprompter-mirror-controls";
        public const string MirrorHorizontalToggle = "teleprompter-mirror-horizontal";
        public const string MirrorVerticalToggle = "teleprompter-mirror-vertical";
        public const string NextBlock = "teleprompter-next-block";
        public const string NextWord = "teleprompter-next-word";
        public const string OrientationToggle = "teleprompter-orientation-toggle";
        public const string Page = "teleprompter-page";
        public const string PlayToggle = "teleprompter-play-toggle";
        public const string PreviousBlock = "teleprompter-previous-block";
        public const string PreviousWord = "teleprompter-previous-word";
        public const string Progress = "teleprompter-progress";
        public const string ProgressLabel = "teleprompter-progress-label";
        public const string ProgressSegments = "teleprompter-progress-segments";
        public const string Sliders = "teleprompter-sliders";
        public const string Stage = "teleprompter-stage";
        public const string WidthSlider = "teleprompter-width-slider";

        public static string Card(int index) => $"teleprompter-card-{index}";

        public static string CardGroup(int cardIndex, int groupIndex) => $"teleprompter-card-group-{cardIndex}-{groupIndex}";

        public static string CardGroupPrefix(int cardIndex) => $"teleprompter-card-group-{cardIndex}-";

        public static string CardText(int index) => $"{Card(index)}-text";

        public static string CardWord(int cardIndex, int groupIndex, int wordIndex) =>
            $"teleprompter-card-word-{cardIndex}-{groupIndex}-{wordIndex}";

        public static string ProgressSegmentFill(int index) => $"teleprompter-progress-segment-fill-{index}";
    }

    public static class Settings
    {
        public const string AboutAppCard = "settings-about-app-card";
        public const string AboutCompanyCard = "settings-about-company-card";
        public const string AboutCompanyGitHub = "settings-about-company-github";
        public const string AboutCompanyWebsite = "settings-about-company-website";
        public const string AboutPanel = "settings-about-panel";
        public const string AboutProductWebsite = "settings-about-product-website";
        public const string AboutRepositoryLink = "settings-about-repository-link";
        public const string AboutReleasesLink = "settings-about-releases-link";
        public const string AboutIssuesLink = "settings-about-issues-link";
        public const string AboutVersion = "settings-about-version";
        public const string AppearancePanel = "settings-appearance-panel";
        public const string AiPanel = "settings-ai-panel";
        public const string LanguageSelect = "settings-language-select";
        public const string CameraFrameRate = "settings-camera-frame-rate";
        public const string CameraPreviewCard = "settings-camera-preview-card";
        public const string CameraPreviewEmpty = "settings-camera-preview-empty";
        public const string CameraPreviewLabel = "settings-camera-preview-label";
        public const string CameraPreviewVideo = "settings-camera-preview-video";
        public const string CameraRoutingCta = Header.GoLive;
        public const string CameraMirrorToggle = "settings-camera-mirror-toggle";
        public const string CameraResolution = "settings-camera-resolution";
        public const string CloudAutoSyncOnSave = "settings-cloud-auto-sync-on-save";
        public const string CloudDefaultProvider = "settings-cloud-default-provider";
        public const string CamerasPanel = "settings-cameras-panel";
        public const string CloudPanel = "settings-cloud-panel";
        public const string CloudSyncOnStartup = "settings-cloud-sync-on-startup";
        public const string DefaultCamera = "settings-default-camera";
        public const string EchoCancellation = "settings-echo-cancellation";
        public const string FileAutoSave = "settings-file-autosave";
        public const string FileBackupCopies = "settings-file-backup-copies";
        public const string FilesPanel = "settings-files-panel";
        public const string MicLevel = "settings-mic-level";
        public const string MicLevelValue = "settings-mic-level-value";
        public const string MicPreviewCard = "settings-mic-preview-card";
        public const string MicPreviewEmpty = "settings-mic-preview-empty";
        public const string MicPreviewLabel = "settings-mic-preview-label";
        public const string MicPreviewMeter = "settings-mic-preview-meter";
        public const string MicPreviewValue = "settings-mic-preview-value";
        public const string MicsPanel = "settings-mics-panel";
        public const string NavAbout = "settings-nav-about";
        public const string NavAi = "settings-nav-ai";
        public const string NavAppearance = "settings-nav-appearance";
        public const string NavCameras = "settings-nav-cameras";
        public const string NavCloud = "settings-nav-cloud";
        public const string NavFiles = "settings-nav-files";
        public const string NavMics = "settings-nav-mics";
        public const string NavRecording = "settings-nav-recording";
        public const string NavStreaming = "settings-nav-streaming";
        public const string NoiseSuppression = "settings-noise-suppression";
        public const string Page = "settings-page";
        public const string PrimaryMic = "settings-primary-mic";
        public const string Title = "settings-title";
        public const string RecordingAudioBitrate = "settings-recording-audio-bitrate";
        public const string RecordingAutoRecord = "settings-recording-auto-record";
        public const string RecordingPanel = "settings-recording-panel";
        public const string RecordingSplit = "settings-recording-split";
        public const string RecordingVideoBitrate = "settings-recording-video-bitrate";
        public const string ReaderCameraToggle = "settings-reader-camera-toggle";
        public const string RequestMedia = "settings-request-media";
        public const string NoCameras = "settings-no-cameras";
        public const string NoMics = "settings-no-mics";
        public const string StreamingBitrate = "settings-streaming-bitrate";
        public const string StreamingCustomRtmpKey = "settings-streaming-custom-rtmp-key";
        public const string StreamingCustomRtmpName = "settings-streaming-custom-rtmp-name";
        public const string StreamingCustomRtmpUrl = "settings-streaming-custom-rtmp-url";
        public const string StreamingDistributionAdd = "settings-streaming-distribution-add";
        public const string StreamingIncludeCamera = "settings-streaming-include-camera";
        public const string StreamingLiveKitRoom = "settings-streaming-livekit-room";
        public const string StreamingLiveKitServer = "settings-streaming-livekit-server";
        public const string StreamingLiveKitToken = "settings-streaming-livekit-token";
        public const string StreamingOutputResolution = "settings-streaming-output-resolution";
        public const string StreamingPanel = "settings-streaming-panel";
        public const string StreamingRecordingToggle = "settings-streaming-recording-toggle";
        public const string StreamingSourcePickerEmpty = "settings-streaming-source-picker-empty";
        public const string StreamingTextOverlay = "settings-streaming-text-overlay";
        public const string StreamingTransportAdd = "settings-streaming-transport-add";
        public const string StreamingTwitchKey = "settings-streaming-twitch-key";
        public const string StreamingTwitchUrl = "settings-streaming-twitch-url";
        public const string StreamingVdoBaseUrl = "settings-streaming-vdo-base-url";
        public const string StreamingVdoPublishUrl = "settings-streaming-vdo-publish-url";
        public const string StreamingVdoRoom = "settings-streaming-vdo-room";
        public const string StreamingVdoViewUrl = "settings-streaming-vdo-view-url";
        public const string StreamingYoutubeKey = "settings-streaming-youtube-key";
        public const string StreamingYoutubeUrl = "settings-streaming-youtube-url";
        public const string TestConnection = "settings-test-connection";

        public static string AiProvider(string providerId) => $"settings-ai-provider-{providerId}";

        public static string AiProviderClear(string providerId) => $"settings-ai-provider-{providerId}-clear";

        public static string AiProviderMessage(string providerId) => $"settings-ai-provider-{providerId}-message";

        public static string AiProviderSave(string providerId) => $"settings-ai-provider-{providerId}-save";

        public static string CameraDevice(string deviceId) => $"settings-camera-device-{deviceId}";

        public static string CameraDeviceAction(string deviceId) => $"settings-camera-device-action-{deviceId}";

        public static string CameraPrimaryAction(string deviceId) => $"settings-camera-primary-{deviceId}";

        public static string CloudProviderCard(string providerId) => $"settings-cloud-{providerId}-card";

        public static string CloudProviderConnect(string providerId) => $"settings-cloud-{providerId}-connect";

        public static string CloudProviderDisconnect(string providerId) => $"settings-cloud-{providerId}-disconnect";

        public static string CloudProviderExport(string providerId) => $"settings-cloud-{providerId}-export";

        public static string CloudProviderField(string providerId, string fieldId) => $"settings-cloud-{providerId}-{fieldId}";

        public static string CloudProviderImport(string providerId) => $"settings-cloud-{providerId}-import";

        public static string CloudProviderMessage(string providerId) => $"settings-cloud-{providerId}-message";

        public static string CloudProviderSubtitle(string providerId) => $"settings-cloud-{providerId}-subtitle";

        public static string MicDelay(string deviceId) => $"settings-mic-delay-{deviceId}";

        public static string MicDevice(string deviceId) => $"settings-mic-device-{deviceId}";

        public static string ThemeOption(string value) => $"settings-theme-option-{value}";

        public static string SceneCamera(string sourceId) => $"settings-scene-camera-{sourceId}";

        public static string SceneFlip(string sourceId) => $"settings-scene-flip-{sourceId}";

        public static string SceneMirror(string sourceId) => $"settings-scene-mirror-{sourceId}";

        public static string SelectOption(string triggerTestId, string optionValue) =>
            $"{triggerTestId}-option-{optionValue}";

        public static string StreamingProviderCard(string providerId) => $"settings-streaming-provider-{providerId}";

        public static string StreamingProviderSourcePicker(string providerId) => $"settings-streaming-provider-sources-{providerId}";

        public static string StreamingProviderSourceSummary(string providerId) => $"settings-streaming-provider-source-summary-{providerId}";

        public static string StreamingProviderSourceToggle(string providerId, string sourceId) => $"settings-streaming-provider-source-{providerId}-{sourceId}";

        public static string StreamingDistributionAddOption(string platformId) => $"settings-streaming-distribution-add-{platformId}";

        public static string StreamingDistributionField(string targetId, string fieldId) => (targetId, fieldId) switch
        {
            (GoLiveTargetCatalog.TargetIds.Youtube, "rtmp-url") => StreamingYoutubeUrl,
            (GoLiveTargetCatalog.TargetIds.Youtube, "stream-key") => StreamingYoutubeKey,
            (GoLiveTargetCatalog.TargetIds.Twitch, "rtmp-url") => StreamingTwitchUrl,
            (GoLiveTargetCatalog.TargetIds.Twitch, "stream-key") => StreamingTwitchKey,
            (GoLiveTargetCatalog.TargetIds.CustomRtmp, "name") => StreamingCustomRtmpName,
            (GoLiveTargetCatalog.TargetIds.CustomRtmp, "rtmp-url") => StreamingCustomRtmpUrl,
            (GoLiveTargetCatalog.TargetIds.CustomRtmp, "stream-key") => StreamingCustomRtmpKey,
            _ => $"settings-streaming-distribution-field-{targetId}-{fieldId}"
        };

        public static string StreamingDistributionRemove(string targetId) =>
            $"settings-streaming-distribution-remove-{targetId}";

        public static string StreamingDistributionToggle(string targetId) =>
            $"settings-streaming-distribution-toggle-{targetId}";

        public static string StreamingDistributionTransport(string targetId, string connectionId) =>
            $"settings-streaming-distribution-transport-{targetId}-{connectionId}";

        public static string StreamingTransportAddOption(string platformId) => $"settings-streaming-transport-add-{platformId}";

        public static string StreamingTransportField(string connectionId, string fieldId) => (connectionId, fieldId) switch
        {
            (GoLiveTargetCatalog.TargetIds.LiveKit, "server-url") => StreamingLiveKitServer,
            (GoLiveTargetCatalog.TargetIds.LiveKit, "room-name") => StreamingLiveKitRoom,
            (GoLiveTargetCatalog.TargetIds.LiveKit, "token") => StreamingLiveKitToken,
            (GoLiveTargetCatalog.TargetIds.VdoNinja, "base-url") => StreamingVdoBaseUrl,
            (GoLiveTargetCatalog.TargetIds.VdoNinja, "room-name") => StreamingVdoRoom,
            (GoLiveTargetCatalog.TargetIds.VdoNinja, "publish-url") => StreamingVdoPublishUrl,
            (GoLiveTargetCatalog.TargetIds.VdoNinja, "view-url") => StreamingVdoViewUrl,
            _ => $"settings-streaming-transport-field-{connectionId}-{fieldId}"
        };

        public static string StreamingTransportRemove(string connectionId) =>
            $"settings-streaming-transport-remove-{connectionId}";

        public static string StreamingTransportRole(string connectionId) =>
            $"settings-streaming-transport-role-{connectionId}";

        public static string StreamingTransportToggle(string connectionId) =>
            $"settings-streaming-transport-toggle-{connectionId}";
    }

    public static class GoLive
    {
        public const string ActiveSourceLabel = "go-live-active-source-label";
        public const string AddSource = "go-live-add-source";
        public const string AudioMixer = "go-live-audio-mixer";
        public const string Back = "go-live-back";
        public const string Bitrate = "go-live-bitrate";
        public const string CreateRoom = "go-live-create-room";
        public const string CustomRtmpKey = "go-live-custom-rtmp-key";
        public const string CustomRtmpName = "go-live-custom-rtmp-name";
        public const string CustomRtmpToggle = "go-live-custom-rtmp-toggle";
        public const string CustomRtmpUrl = "go-live-custom-rtmp-url";
        public const string FullProgramToggle = "go-live-full-program-toggle";
        public const string LeftPanelToggle = "go-live-left-panel-toggle";
        public const string LiveKitRoom = "go-live-livekit-room";
        public const string LiveKitServer = "go-live-livekit-server";
        public const string LiveKitToggle = "go-live-livekit-toggle";
        public const string LiveKitToken = "go-live-livekit-token";
        public const string ModeDirector = "go-live-mode-director";
        public const string ModeStudio = "go-live-mode-studio";
        public const string OpenHome = Back;
        public const string OpenLearn = "go-live-open-learn";
        public const string OpenRead = "go-live-open-read";
        public const string OpenSettings = "go-live-open-settings";
        public const string OutputResolution = "go-live-output-resolution";
        public const string Page = "go-live-page";
        public const string ProgramEmpty = "go-live-program-empty";
        public const string ProgramCard = "go-live-program-card";
        public const string ProgramVideo = "go-live-program-video";
        public const string PreviewRail = "go-live-preview-rail";
        public const string PreviewCard = "go-live-preview-card";
        public const string PreviewEmpty = "go-live-preview-empty";
        public const string PreviewLiveDot = "go-live-preview-live-dot";
        public const string PreviewSourceLabel = "go-live-preview-source-label";
        public const string PreviewVideo = "go-live-preview-video";
        public const string RecordingToggle = "go-live-recording-toggle";
        public const string RightPanelToggle = "go-live-right-panel-toggle";
        public const string RoomActive = "go-live-room-active";
        public const string RoomEmpty = "go-live-room-empty";
        public const string RoomInvite = "go-live-room-invite";
        public const string RoomTab = "go-live-room-tab";
        public const string AudioTab = "go-live-audio-tab";
        public const string StreamTab = "go-live-stream-tab";
        public const string SceneBar = "go-live-scene-bar";
        public const string SceneControls = "go-live-scene-controls";
        public const string ScreenTitle = "go-live-screen-title";
        public const string SingleLocalPreviewHint = "go-live-single-local-preview-hint";
        public const string SessionBar = "go-live-session-bar";
        public const string SessionTimer = "go-live-session-timer";
        public const string SourceRail = "go-live-source-rail";
        public const string Stage = "go-live-stage";
        public const string SelectedSourceLabel = "go-live-selected-source-label";
        public const string SourcesCard = "go-live-sources-card";
        public const string StartRecording = "go-live-start-recording";
        public const string StartStream = "go-live-start-stream";
        public const string SourceDeviceIdAttribute = "data-device-id";
        public const string SourceIdAttribute = "data-source-id";
        public const string SourcePickerEmpty = "go-live-source-picker-empty";
        public const string SwitchSelectedSource = "go-live-switch-selected-source";
        public const string StreamIncludeCamera = "go-live-stream-include-camera";
        public const string StreamTextOverlay = "go-live-stream-text-overlay";
        public const string TakeToAir = "go-live-take-to-air";
        public const string LayoutFull = "go-live-layout-full";
        public const string LayoutSplit = "go-live-layout-split";
        public const string LayoutPictureInPicture = "go-live-layout-picture-in-picture";
        public const string TwitchKey = "go-live-twitch-key";
        public const string TwitchToggle = "go-live-twitch-toggle";
        public const string TwitchUrl = "go-live-twitch-url";
        public const string VdoPublishUrl = "go-live-vdo-publish-url";
        public const string VdoRoom = "go-live-vdo-room";
        public const string VdoToggle = "go-live-vdo-toggle";
        public const string YoutubeKey = "go-live-youtube-key";
        public const string YoutubeToggle = "go-live-youtube-toggle";
        public const string YoutubeUrl = "go-live-youtube-url";

        public static string SourceCameraSelect(string sourceId) => $"go-live-source-select-{sourceId}";
        public static string DestinationToggle(string destinationId) => destinationId switch
        {
            GoLiveTargetCatalog.TargetIds.Recording => RecordingToggle,
            GoLiveTargetCatalog.TargetIds.LiveKit => LiveKitToggle,
            GoLiveTargetCatalog.TargetIds.VdoNinja => VdoToggle,
            GoLiveTargetCatalog.TargetIds.Youtube => YoutubeToggle,
            GoLiveTargetCatalog.TargetIds.Twitch => TwitchToggle,
            GoLiveTargetCatalog.TargetIds.CustomRtmp => CustomRtmpToggle,
            _ => $"go-live-destination-toggle-{destinationId}"
        };
        public static string ProviderCard(string providerId) => $"go-live-provider-{providerId}";
        public static string ProviderSourcePicker(string providerId) => $"go-live-provider-sources-{providerId}";
        public static string ProviderSourceSummary(string providerId) => $"go-live-provider-source-summary-{providerId}";
        public static string RuntimeMetric(string metricId) => $"go-live-runtime-metric-{metricId}";
        public static string StatusMetric(string metricId) => $"go-live-status-metric-{metricId}";
        public static string ProviderSourceToggle(string providerId, string sourceId) => $"go-live-provider-source-{providerId}-{sourceId}";

        public static string SourceCamera(string sourceId) => $"go-live-source-camera-{sourceId}";

        public static string SourceCameraAction(string deviceId) => $"go-live-source-camera-action-{deviceId}";

        public static string SourceCameraBadge(string sourceId) => $"go-live-source-badge-{sourceId}";

        public static string SourceVideo(string sourceId) => $"go-live-source-video-{sourceId}";

        public static string AudioChannel(string channelId) => $"go-live-audio-channel-{channelId}";

        public static string RoomParticipant(string participantId) => $"go-live-room-participant-{participantId}";

        public static string SceneChip(string sceneId) => $"go-live-scene-chip-{sceneId}";

        public static string UtilitySource(string sourceId) => $"go-live-utility-source-{sourceId}";
    }
}
