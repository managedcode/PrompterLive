using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.GoLive.Models;

public static class GoLiveText
{
    public static class Chrome
    {
        public const string BackLabel = "GoLiveBackLabel";
        public const string DirectorModeLabel = "GoLiveDirectorModeLabel";
        public const string LivePreviewTitle = "GoLiveLivePreviewTitle";
        public const string ScreenTitle = nameof(UiTextKey.HeaderGoLive);
        public const string StreamingSubtitle = "GoLiveStreamingSubtitle";
        public const string StudioModeLabel = "GoLiveStudioModeLabel";
    }

    public static class Load
    {
        public const string LoadMessage = "GoLiveLoadMessage";
        public const string LoadOperation = "Go Live load";
        public const string SaveSceneMessage = "GoLiveSaveSceneMessage";
        public const string SaveSceneOperation = "Go Live save scene";
        public const string SaveStudioMessage = "GoLiveSaveStudioMessage";
        public const string SaveStudioOperation = "Go Live save studio";
    }

    public static class Audio
    {
        public const string DefaultMicrophoneRouteLabel = "GoLiveAudioDefaultMicrophoneRouteLabel";
        public const string MonitorOnlyLabel = "GoLiveAudioMonitorOnlyLabel";
        public const string NoMicrophoneLabel = "GoLiveAudioNoMicrophoneLabel";
        public const string StreamOnlyLabel = "GoLiveAudioStreamOnlyLabel";
    }

    public static class Session
    {
        public const string CameraFallbackLabel = "GoLiveSessionCameraFallbackLabel";
        public const string DefaultProgramTimerLabel = "00:00:00";
        public const string IdleStateValue = "idle";
        public const string ProgramBadgeIdleLabel = "GoLiveSessionProgramBadgeIdleLabel";
        public const string ProgramBadgeLiveLabel = "GoLiveSessionProgramBadgeLiveLabel";
        public const string ProgramBadgeRecordingLabel = "GoLiveSessionProgramBadgeRecordingLabel";
        public const string ProgramBadgeStreamingRecordingLabel = "GoLiveSessionProgramBadgeStreamingRecordingLabel";
        public const string RecordingIndicatorLabel = "GoLiveSessionRecordingIndicatorLabel";
        public const string RecordingStateValue = "recording";
        public const string SessionIdleLabel = "GoLiveSessionIdleLabel";
        public const string SessionRecordingLabel = "GoLiveSessionRecordingLabel";
        public const string SessionStreamingLabel = "GoLiveSessionStreamingLabel";
        public const string SessionStreamingRecordingLabel = "GoLiveSessionStreamingRecordingLabel";
        public const string StartStreamMessage = "GoLiveSessionStartStreamMessage";
        public const string StartStreamOperation = "Go Live start stream";
        public const string StageFrameRate30Label = "30 FPS";
        public const string StageFrameRate60Label = "60 FPS";
        public const string StopStreamMessage = "GoLiveSessionStopStreamMessage";
        public const string StopStreamOperation = "Go Live stop stream";
        public const string StreamingStateValue = "streaming";
        public const string StreamButtonLabel = "GoLiveSessionStreamButtonLabel";
        public const string StreamPrerequisiteDetail = "GoLiveSessionStreamPrerequisiteDetail";
        public const string StreamPrerequisiteMessage = "GoLiveSessionStreamPrerequisiteMessage";
        public const string StreamPrerequisiteOperation = "Go Live stream prerequisites";
        public const string StreamStopLabel = "GoLiveSessionStreamStopLabel";
        public const string SwitchProgramMessage = "GoLiveSessionSwitchProgramMessage";
        public const string SwitchProgramOperation = "Go Live switch program source";
        public const string SwitchButtonDisabledLabel = "GoLiveSessionSwitchButtonDisabledLabel";
        public const string SwitchButtonLabel = "GoLiveSessionSwitchButtonLabel";
        public const string StartRecordingMessage = "GoLiveSessionStartRecordingMessage";
        public const string StartRecordingOperation = "Go Live start recording";
        public const string StopRecordingMessage = "GoLiveSessionStopRecordingMessage";
        public const string StopRecordingOperation = "Go Live stop recording";
    }

    public static class Surface
    {
        public const string ActiveDestinationsMetricLabel = "GoLiveSurfaceActiveDestinationsMetricLabel";
        public const string AudioIdleDetailLabel = "GoLiveSurfaceAudioIdleDetailLabel";
        public const string AudioMicChannelId = "mic";
        public const string AudioProgramChannelId = "program";
        public const string AudioProgramChannelLabel = "GoLiveSurfaceAudioProgramChannelLabel";
        public const string AudioRecordingChannelId = "recording";
        public const string AudioRecordingChannelLabel = "GoLiveSurfaceAudioRecordingChannelLabel";
        public const string CustomScenePrefix = "custom-scene-";
        public const string CustomSceneTitlePrefix = "GoLiveSurfaceCustomSceneTitlePrefix";
        public const string DetailLocalProgramLabel = "GoLiveSurfaceDetailLocalProgramLabel";
        public const string DirectorSourcesTitle = "GoLiveSurfaceDirectorSourcesTitle";
        public const string HostParticipantId = "host";
        public const string HostParticipantInitial = "GoLiveSurfaceHostParticipantInitial";
        public const string HostParticipantName = "GoLiveSurfaceHostParticipantName";
        public const string InterviewSceneFallback = "GoLiveSurfaceInterviewSceneFallback";
        public const string LocalRoomPrefix = "local-";
        public const string MicrophoneMetricLabel = "GoLiveSurfaceMicrophoneMetricLabel";
        public const string PictureInPictureSceneId = "scene-picture-in-picture";
        public const string PictureInPictureSceneLabel = "GoLiveSurfacePictureInPictureSceneLabel";
        public const string PrimarySceneId = "scene-primary";
        public const string ProgramMetricLabel = "GoLiveSurfaceProgramMetricLabel";
        public const string ProgramStandbyDetailLabel = "GoLiveSurfaceProgramStandbyDetailLabel";
        public const string RecordingActiveMetricValue = "GoLiveSurfaceRecordingActiveMetricValue";
        public const string RecordingContainerMp4Value = "MP4";
        public const string RecordingMetricLabel = "GoLiveSurfaceRecordingMetricLabel";
        public const string RecordingReadyDetailLabel = "GoLiveSurfaceRecordingReadyDetailLabel";
        public const string RecordingReadyMetricValue = "GoLiveSurfaceRecordingReadyMetricValue";
        public const string RecordingSaveModeBrowserDownloadValue = "GoLiveSurfaceRecordingSaveModeBrowserDownloadValue";
        public const string RecordingSaveModeLocalFileValue = "GoLiveSurfaceRecordingSaveModeLocalFileValue";
        public const string RecordingContainerWebmValue = "WEBM";
        public const string ResolutionDimensionsFullHd = "1920 × 1080";
        public const string ResolutionDimensionsHd = "1280 × 720";
        public const string ResolutionDimensionsUltraHd = "3840 × 2160";
        public const string RoomCodeFallback = "local-studio";
        public const string RuntimeEngineIdleValue = "GoLiveSurfaceRuntimeEngineIdleValue";
        public const string RuntimeEngineLabel = "GoLiveSurfaceRuntimeEngineLabel";
        public const string RuntimeEngineLiveKitValue = "LiveKit";
        public const string RuntimeEngineRecorderValue = "GoLiveSurfaceRuntimeEngineRecorderValue";
        public const string RuntimeEngineVdoNinjaValue = "VDO.Ninja";
        public const string SceneSlidesId = "scene-slides";
        public const string SceneSlidesLabel = "GoLiveSurfaceSceneSlidesLabel";
        public const string SecondarySceneId = "scene-secondary";
        public const string SessionMetricLabel = "GoLiveSurfaceSessionMetricLabel";
        public const string SourcesTitle = "GoLiveSurfaceSourcesTitle";
        public const string StatusBitrateLabel = "GoLiveSurfaceStatusBitrateLabel";
        public const string StatusOutputLabel = "GoLiveSurfaceStatusOutputLabel";
        public const string SingleLocalCameraPreviewHint = "GoLiveSurfaceSingleLocalCameraPreviewHint";
        public const string SingleLocalCameraPreviewLimitDescription = "GoLiveSurfaceSingleLocalCameraPreviewLimitDescription";
        public const string SingleLocalCameraPreviewLimitTitle = "GoLiveSurfaceSingleLocalCameraPreviewLimitTitle";
        public const string SingleLocalCameraPreviewPlaceholderLabel = "GoLiveSurfaceSingleLocalCameraPreviewPlaceholderLabel";
        public const string StreamFormatFullHd30 = "1080p30";
        public const string StreamFormatFullHd60 = "1080p60";
        public const string StreamFormatHd30 = "720p30";
        public const string StreamFormatUltraHd30 = "2160p30";
    }

    public static class Destination
    {
        public const string BlockedStatusLabel = "GoLiveDestinationBlockedStatusLabel";
        public const string BlockedSummary = "GoLiveDestinationBlockedSummary";
        public const string DisabledSummary = "GoLiveDestinationDisabledSummary";
        public const string DisabledStatusLabel = "GoLiveDestinationDisabledStatusLabel";
        public const string EnabledStatusLabel = "GoLiveDestinationEnabledStatusLabel";
        public const string NeedsSetupStatusLabel = "GoLiveDestinationNeedsSetupStatusLabel";
        public const string NoSourceSummary = "GoLiveDestinationNoSourceSummary";
        public const string RecordingTone = "recording";
        public const string RelayStatusLabel = "GoLiveDestinationRelayStatusLabel";
        public const string TransportBindingSummary = "GoLiveDestinationTransportBindingSummary";
        public const string LocalTone = "local";
    }

    public static class Sidebar
    {
        public const string AudioTabLabel = "GoLiveSidebarAudioTabLabel";
        public const string CreateRoomLabel = "GoLiveSidebarCreateRoomLabel";
        public const string CueLabel = "GoLiveSidebarCueLabel";
        public const string DestinationsLabel = "GoLiveSidebarDestinationsLabel";
        public const string GuestsLabel = "GoLiveSidebarGuestsLabel";
        public const string InviteLabel = "GoLiveSidebarInviteLabel";
        public const string LiveBadgeLabel = "GoLiveSidebarLiveBadgeLabel";
        public const string MicrophoneChannelId = "mic";
        public const string MuteAllLabel = "GoLiveSidebarMuteAllLabel";
        public const string RoomDescription = "GoLiveSidebarRoomDescription";
        public const string RoomTabLabel = "GoLiveSidebarRoomTabLabel";
        public const string RoomTitle = "GoLiveSidebarRoomTitle";
        public const string RuntimeLabel = "GoLiveSidebarRuntimeLabel";
        public const string StatusLabel = "GoLiveSidebarStatusLabel";
        public const string StreamTabLabel = "GoLiveSidebarStreamTabLabel";
        public const string TalkLabel = "GoLiveSidebarTalkLabel";
    }
}
