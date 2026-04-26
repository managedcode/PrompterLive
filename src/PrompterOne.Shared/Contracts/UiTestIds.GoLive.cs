using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Shared.Contracts;

public static partial class UiTestIds
{
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
        public const string LocalRecordingAudioButton = "go-live-local-recording-audio-button";
        public const string LocalRecordingControls = "go-live-local-recording-controls";
        public const string LocalRecordingStatus = "go-live-local-recording-status";
        public const string LocalRecordingVideoButton = "go-live-local-recording-video-button";
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
        public const string RecordingBlockActive = "go-live-recording-block-active";
        public const string RecordingBlockContext = "go-live-recording-block-context";
        public const string RecordingBlockContextToggle = "go-live-recording-block-context-toggle";
        public const string RecordingBlockNext = "go-live-recording-block-next";
        public const string RecordingBlockNextControl = "go-live-recording-block-next-control";
        public const string RecordingBlockPrevious = "go-live-recording-block-previous";
        public const string RecordingBlockPreviousControl = "go-live-recording-block-previous-control";
        public const string RecordingBlockTake = "go-live-recording-block-take";
        public const string RecordingBlockTakeCompare = "go-live-recording-block-take-compare";
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
