namespace PrompterOne.Shared.GoLive.Models;

public static class GoLiveText
{
    public static class Chrome
    {
        public const string BackLabel = "Back";
        public const string DirectorModeLabel = "Director";
        public const string LivePreviewTitle = "Live";
        public const string ScreenTitle = "Go Live";
        public const string StreamingSubtitle = "Program routing";
        public const string StudioModeLabel = "Studio";
    }

    public static class Load
    {
        public const string LoadMessage = "Unable to prepare live routing right now.";
        public const string LoadOperation = "Go Live load";
        public const string SaveSceneMessage = "Unable to save the current live scene.";
        public const string SaveSceneOperation = "Go Live save scene";
        public const string SaveStudioMessage = "Unable to save live routing settings.";
        public const string SaveStudioOperation = "Go Live save studio";
    }

    public static class Audio
    {
        public const string DefaultMicrophoneRouteLabel = "Monitor + Stream";
        public const string MonitorOnlyLabel = "Monitor only";
        public const string NoMicrophoneLabel = "No microphone";
        public const string StreamOnlyLabel = "Stream only";
    }

    public static class Session
    {
        public const string CameraFallbackLabel = "No camera selected";
        public const string DefaultProgramTimerLabel = "00:00:00";
        public const string IdleStateValue = "idle";
        public const string ProgramBadgeIdleLabel = "Ready";
        public const string ProgramBadgeLiveLabel = "Live";
        public const string ProgramBadgeRecordingLabel = "Rec";
        public const string ProgramBadgeStreamingRecordingLabel = "Live + Rec";
        public const string RecordingIndicatorLabel = "REC";
        public const string RecordingStateValue = "recording";
        public const string SessionIdleLabel = "Ready";
        public const string SessionRecordingLabel = "Recording";
        public const string SessionStreamingLabel = "Streaming";
        public const string SessionStreamingRecordingLabel = "Streaming + Recording";
        public const string StartStreamMessage = "Unable to start live outputs right now.";
        public const string StartStreamOperation = "Go Live start stream";
        public const string StageFrameRate30Label = "30 FPS";
        public const string StageFrameRate60Label = "60 FPS";
        public const string StopStreamMessage = "Unable to stop live outputs right now.";
        public const string StopStreamOperation = "Go Live stop stream";
        public const string StreamingStateValue = "streaming";
        public const string StreamButtonLabel = "Start Stream";
        public const string StreamPrerequisiteDetail = "No direct browser live output is armed. RTMP destinations still require an external relay.";
        public const string StreamPrerequisiteMessage = "Arm at least one direct browser publishing transport before starting the stream.";
        public const string StreamPrerequisiteOperation = "Go Live stream prerequisites";
        public const string StreamStopLabel = "Stop Stream";
        public const string SwitchProgramMessage = "Unable to switch the live program source right now.";
        public const string SwitchProgramOperation = "Go Live switch program source";
        public const string SwitchButtonDisabledLabel = "On Program";
        public const string SwitchButtonLabel = "Switch";
        public const string StartRecordingMessage = "Unable to start recording right now.";
        public const string StartRecordingOperation = "Go Live start recording";
        public const string StopRecordingMessage = "Unable to stop recording right now.";
        public const string StopRecordingOperation = "Go Live stop recording";
    }

    public static class Surface
    {
        public const string ActiveDestinationsMetricLabel = "Destinations";
        public const string AudioIdleDetailLabel = "Idle";
        public const string AudioMicChannelId = "mic";
        public const string AudioProgramChannelId = "program";
        public const string AudioProgramChannelLabel = "Program";
        public const string AudioRecordingChannelId = "recording";
        public const string AudioRecordingChannelLabel = "Recording";
        public const string CustomScenePrefix = "custom-scene-";
        public const string CustomSceneTitlePrefix = "Scene ";
        public const string DetailLocalProgramLabel = "Local program";
        public const string DirectorSourcesTitle = "Cameras";
        public const string HostParticipantId = "host";
        public const string HostParticipantInitial = "H";
        public const string HostParticipantName = "Host";
        public const string InterviewSceneFallback = "Interview";
        public const string LocalRoomPrefix = "local-";
        public const string MicrophoneMetricLabel = "Mic";
        public const string NoScriptProgressLabel = "No script loaded";
        public const string PictureInPictureSceneId = "scene-picture-in-picture";
        public const string PictureInPictureSceneLabel = "PiP Slides";
        public const string PrimarySceneId = "scene-primary";
        public const string ProgramMetricLabel = "Camera";
        public const string ProgramStandbyDetailLabel = "Program idle";
        public const string RecordingActiveMetricValue = "Saving";
        public const string RecordingContainerMp4Value = "MP4";
        public const string RecordingMetricLabel = "Recording";
        public const string RecordingReadyDetailLabel = "Ready";
        public const string RecordingReadyMetricValue = "Armed";
        public const string RecordingSaveModeBrowserDownloadValue = "Browser download";
        public const string RecordingSaveModeLocalFileValue = "Local file";
        public const string RecordingContainerWebmValue = "WEBM";
        public const string ResolutionDimensionsFullHd = "1920 × 1080";
        public const string ResolutionDimensionsHd = "1280 × 720";
        public const string ResolutionDimensionsUltraHd = "3840 × 2160";
        public const string RoomCodeFallback = "local-studio";
        public const string RuntimeEngineIdleValue = "Idle";
        public const string RuntimeEngineLabel = "Runtime";
        public const string RuntimeEngineLiveKitValue = "LiveKit";
        public const string RuntimeEngineRecorderValue = "Recorder";
        public const string RuntimeEngineVdoNinjaValue = "VDO.Ninja";
        public const string SceneSlidesId = "scene-slides";
        public const string SceneSlidesLabel = "Slides";
        public const string SecondarySceneId = "scene-secondary";
        public const string SessionMetricLabel = "Session";
        public const string SourcesTitle = "Sources";
        public const string StatusBitrateLabel = "Bitrate";
        public const string StatusOutputLabel = "Output";
        public const string SingleLocalCameraPreviewHint = "iPad browsers can preview one local camera at a time. Select a camera, then switch it live. Remote guest feeds can stay live.";
        public const string SingleLocalCameraPreviewLimitDescription = "This browser is previewing the selected local camera only. Use Switch to move the live feed.";
        public const string SingleLocalCameraPreviewLimitTitle = "Live preview limited";
        public const string SingleLocalCameraPreviewPlaceholderLabel = "Select to preview";
        public const string StreamFormatFullHd30 = "1080p30";
        public const string StreamFormatFullHd60 = "1080p60";
        public const string StreamFormatHd30 = "720p30";
        public const string StreamFormatUltraHd30 = "2160p30";
    }

    public static class Destination
    {
        public const string BlockedStatusLabel = "Blocked";
        public const string BlockedSummary = "This target is bound only to transports that cannot deliver a downstream relay path.";
        public const string DisabledSummary = "Disabled in this live session.";
        public const string DisabledStatusLabel = "Disabled";
        public const string EnabledStatusLabel = "Ready";
        public const string LocalSummarySuffix = " source(s) armed for this output.";
        public const string NeedsSetupStatusLabel = "Needs setup";
        public const string NoSourceSummary = "No routed source is armed for this destination yet.";
        public const string PrimaryChannelPlatformLabel = "Primary channel";
        public const string RecordingTone = "recording";
        public const string RelayStatusLabel = "Relay";
        public const string SettingsPlatformLabel = "Settings preset";
        public const string TransportBindingSummary = "Bind this target to at least one armed transport connection.";
        public const string LocalTone = "local";
    }

    public static class Sidebar
    {
        public const string AudioTabLabel = "Audio";
        public const string CreateRoomLabel = "Create Room";
        public const string CueLabel = "Cue";
        public const string DestinationsLabel = "Destinations";
        public const string GuestsLabel = "Guests";
        public const string InviteLabel = "Invite";
        public const string LiveBadgeLabel = "Live";
        public const string MicrophoneChannelId = "mic";
        public const string MuteAllLabel = "Mute All";
        public const string RoomDescription = "Invite remote guests to send their camera, mic, or screen. You control everything from here.";
        public const string RoomTabLabel = "Room";
        public const string RoomTitle = "Remote Room";
        public const string RuntimeLabel = "Runtime";
        public const string StatusLabel = "Status";
        public const string StreamTabLabel = "Stream";
        public const string TalkLabel = "Talk";
    }
}
