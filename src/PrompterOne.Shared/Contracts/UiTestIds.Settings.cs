using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Shared.Contracts;

public static partial class UiTestIds
{
    public static class Settings
    {
        public const string AboutAppCard = "settings-about-app-card";
        public const string AboutClarityDisclosure = "settings-about-clarity-disclosure";
        public const string AboutCompanyCard = "settings-about-company-card";
        public const string AboutCompanyGitHub = "settings-about-company-github";
        public const string AboutCompanyWebsite = "settings-about-company-website";
        public const string AboutPanel = "settings-about-panel";
        public const string AboutOnboardingCard = "settings-about-onboarding-card";
        public const string AboutOnboardingRestart = "settings-about-onboarding-restart";
        public const string AboutProductGitHub = "settings-about-product-github";
        public const string AboutTpsGitHub = "settings-about-tps-github";
        public const string AboutProductWebsite = "settings-about-product-website";
        public const string AboutRepositoryLink = "settings-about-repository-link";
        public const string AboutReleasesLink = "settings-about-releases-link";
        public const string AboutIssuesLink = "settings-about-issues-link";
        public const string AboutVersion = "settings-about-version";
        public const string AppearancePanel = "settings-appearance-panel";
        public const string AiPanel = "settings-ai-panel";
        public const string AboutAppIconSurface = "settings-about-app-icon-surface";
        public const string FeedbackCard = "settings-feedback-card";
        public const string FeedbackOpen = "settings-feedback-open";
        public const string FeedbackPanel = "settings-feedback-panel";
        public const string LanguagePanel = "settings-language-panel";
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
        public const string NavLanguage = "settings-nav-language";
        public const string NavCameras = "settings-nav-cameras";
        public const string NavCloud = "settings-nav-cloud";
        public const string NavFeedback = "settings-nav-feedback";
        public const string NavFiles = "settings-nav-files";
        public const string NavMics = "settings-nav-mics";
        public const string NavRecording = "settings-nav-recording";
        public const string NavShortcuts = "settings-nav-shortcuts";
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
        public const string ShortcutsPanel = "settings-shortcuts-panel";
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
        public static string AccentSwatch(string swatchId) => $"settings-accent-swatch-{swatchId}";
        public static string AiProviderClear(string providerId) => $"settings-ai-provider-{providerId}-clear";
        public static string AiProviderField(string providerId, string fieldId) => $"settings-ai-provider-{providerId}-{fieldId}";
        public static string AiProviderModel(string providerId, int index) => $"settings-ai-provider-{providerId}-model-{index}";
        public static string AiProviderModelAdd(string providerId) => $"settings-ai-provider-{providerId}-model-add";
        public static string AiProviderModelField(string providerId, int index, string fieldId) => $"settings-ai-provider-{providerId}-model-{index}-{fieldId}";
        public static string AiProviderModelRemove(string providerId, int index) => $"settings-ai-provider-{providerId}-model-{index}-remove";
        public static string AiProviderMessage(string providerId) => $"settings-ai-provider-{providerId}-message";
        public static string AiProviderSave(string providerId) => $"settings-ai-provider-{providerId}-save";
        public static string AiProviderSubtitle(string providerId) => $"settings-ai-provider-{providerId}-subtitle";
        public static string TextColorSwatch(string swatchId) => $"settings-text-color-swatch-{swatchId}";
        public static string CameraDevice(string deviceId) => $"settings-camera-device-{deviceId}";
        public static string CameraDeviceAction(string deviceId) => $"settings-camera-device-action-{deviceId}";
        public static string CameraDeviceLabel(string deviceId) => $"settings-camera-device-label-{deviceId}";
        public static string ShortcutsAction(string groupId, string actionId) => $"settings-shortcuts-action-{groupId}-{actionId}";
        public static string ShortcutsGroup(string groupId) => $"settings-shortcuts-group-{groupId}";
        public static string CameraPrimaryAction(string deviceId) => $"settings-camera-primary-{deviceId}";
        public static string CloudProviderCard(string providerId) => $"settings-cloud-{providerId}-card";
        public static string CloudProviderActions(string providerId) => $"settings-cloud-{providerId}-actions";
        public static string CloudProviderConnect(string providerId) => $"settings-cloud-{providerId}-connect";
        public static string CloudProviderDisconnect(string providerId) => $"settings-cloud-{providerId}-disconnect";
        public static string CloudProviderExport(string providerId) => $"settings-cloud-{providerId}-export";
        public static string CloudProviderField(string providerId, string fieldId) => $"settings-cloud-{providerId}-{fieldId}";
        public static string CloudProviderImport(string providerId) => $"settings-cloud-{providerId}-import";
        public static string CloudProviderMessage(string providerId) => $"settings-cloud-{providerId}-message";
        public static string CloudProviderSubtitle(string providerId) => $"settings-cloud-{providerId}-subtitle";
        public static string MicDelay(string deviceId) => $"settings-mic-delay-{deviceId}";
        public static string MicDevice(string deviceId) => $"settings-mic-device-{deviceId}";
        public static string MicDeviceLabel(string deviceId) => $"settings-mic-device-label-{deviceId}";
        public static string ThemeOption(string value) => $"settings-theme-option-{value}";
        public static string SceneCamera(string sourceId) => $"settings-scene-camera-{sourceId}";
        public static string SceneFlip(string sourceId) => $"settings-scene-flip-{sourceId}";
        public static string SceneMirror(string sourceId) => $"settings-scene-mirror-{sourceId}";
        public static string SelectPanel(string triggerTestId) => $"{triggerTestId}-panel";
        public static string SelectOption(string triggerTestId, string optionValue) => $"{triggerTestId}-option-{optionValue}";
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

        public static string StreamingDistributionRemove(string targetId) => $"settings-streaming-distribution-remove-{targetId}";
        public static string StreamingDistributionToggle(string targetId) => $"settings-streaming-distribution-toggle-{targetId}";
        public static string StreamingDistributionTransport(string targetId, string connectionId) => $"settings-streaming-distribution-transport-{targetId}-{connectionId}";
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

        public static string StreamingTransportRemove(string connectionId) => $"settings-streaming-transport-remove-{connectionId}";
        public static string StreamingTransportRole(string connectionId) => $"settings-streaming-transport-role-{connectionId}";
        public static string StreamingTransportToggle(string connectionId) => $"settings-streaming-transport-toggle-{connectionId}";
    }
}
