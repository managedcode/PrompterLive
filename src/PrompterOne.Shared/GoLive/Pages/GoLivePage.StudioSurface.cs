using System.Globalization;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Components.GoLive;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private const string ActiveDestinationsMetricLabel = "Destinations";
    private const string ActiveWorkLabel = "Primary channel";
    private const string AudioIdleDetailLabel = "Idle";
    private const string AudioMicChannelId = "mic";
    private const string AudioProgramChannelId = "program";
    private const string AudioProgramChannelLabel = "Program";
    private const string AudioRecordingChannelId = "recording";
    private const string AudioRecordingChannelLabel = "Recording";
    private const string CustomScenePrefix = "custom-scene-";
    private const string CustomSceneTitlePrefix = "Scene ";
    private const string DetailLocalProgramLabel = "Local program";
    private const string DestinationToneLiveKit = "livekit";
    private const string DestinationToneLocal = "local";
    private const string DestinationToneRecording = "recording";
    private const string DestinationToneYoutube = "youtube";
    private const string GuestRoomLabel = "Guest room";
    private const string HostParticipantId = "host";
    private const string HostParticipantInitial = "H";
    private const string HostParticipantName = "Host";
    private const string InterviewSceneFallback = "Interview";
    private const string LocalRoomPrefix = "local-";
    private const string MainSceneFallback = "Camera 1";
    private const string MicrophoneMetricLabel = "Mic";
    private const string PictureInPictureSceneId = "scene-picture-in-picture";
    private const string PictureInPictureSceneLabel = "PiP Slides";
    private const string PrimarySceneId = "scene-primary";
    private const string ProgramMetricLabel = "Camera";
    private const string ProgramStandbyDetailLabel = "Program idle";
    private const string RecordingActiveMetricValue = "Saving";
    private const string RecordingMetricLabel = "Recording";
    private const string RecordingReadyDetailLabel = "Ready";
    private const string RecordingReadyMetricValue = "Armed";
    private const string RelayPlatformLabel = "Relay preset";
    private const string RemoteTalentSourceId = "prompter-display";
    private const string RemoteTalentTitle = "Prompter Display";
    private const string RoomCodeFallback = "local-studio";
    private const string RuntimeEngineIdleValue = "Idle";
    private const string RuntimeEngineLabel = "Runtime";
    private const string RuntimeEngineLiveKitValue = "LiveKit";
    private const string RuntimeEngineObsBrowserValue = "OBS browser";
    private const string RuntimeEngineObsLiveKitValue = "OBS + LiveKit";
    private const string RuntimeEngineRecorderValue = "Recorder";
    private const string SceneSlidesId = "scene-slides";
    private const string SceneSlidesLabel = "Slides";
    private const string ScreenShareSourceId = "screen-share";
    private const string ScreenShareTitle = "Share Screen";
    private const string SecondarySceneId = "scene-secondary";
    private const string SettingsPlatformLabel = "Settings preset";
    private const string StatusBitrateLabel = "Bitrate";
    private const string StatusOutputLabel = "Output";
    private const string SessionMetricLabel = "Session";
    private const string SlidesSourceId = "slides";
    private const string SlidesSourceTitle = "Slides";
    private const string UtilitySourceClickLabel = "Click to share";
    private const string UtilitySourcePrompterBadge = "Prompter";
    private const string UtilitySourceShareBadge = "Add";
    private const string UtilitySourceSlidesBadge = "Slides";
    private const string UtilitySourceSlidesLabel = "Keynote";
    private const string UtilitySourceTalentFacingLabel = "Talent-facing only";
    private const string GoLiveContentBaseClass = "gl-content";
    private const string GoLiveFullProgramClass = "gl-layout-fullpgm";
    private const string GoLiveHideLeftClass = "gl-hide-left";
    private const string GoLiveHideRightClass = "gl-hide-right";

    private static readonly IReadOnlyList<GoLiveUtilitySourceViewModel> StudioUtilitySources =
    [
        new(RemoteTalentSourceId, RemoteTalentTitle, UtilitySourceTalentFacingLabel, UtilitySourcePrompterBadge),
        new(SlidesSourceId, SlidesSourceTitle, UtilitySourceSlidesLabel, UtilitySourceSlidesBadge),
        new(ScreenShareSourceId, ScreenShareTitle, UtilitySourceClickLabel, UtilitySourceShareBadge)
    ];

    private string _activeSceneId = PrimarySceneId;
    private GoLiveSceneLayout _activeSceneLayout = GoLiveSceneLayout.Full;
    private GoLiveStudioMode _activeStudioMode = GoLiveStudioMode.Director;
    private GoLiveStudioTab _activeStudioTab = GoLiveStudioTab.Stream;
    private GoLiveTransitionDuration _activeTransitionDuration = GoLiveTransitionDuration.Quick;
    private GoLiveTransitionKind _activeTransitionKind = GoLiveTransitionKind.Cut;
    private bool _cueArmed;
    private int _customSceneCount;
    private bool _fullProgramView;
    private bool _muteAllGuests;
    private bool _roomCreated;
    private bool _showLeftRail = true;
    private bool _showRightRail = true;
    private bool _talkbackEnabled;

    private IReadOnlyList<GoLiveAudioChannelViewModel> AudioChannels => BuildAudioChannels();

    private IReadOnlyList<GoLiveDestinationSummaryViewModel> DestinationSummary => BuildDestinationSummary();

    private bool IsRoomActive =>
        _roomCreated
        || GoLiveOutputRuntime.State.LiveKitActive
        || !string.IsNullOrWhiteSpace(_studioSettings.Streaming.LiveKitRoomName)
        || _studioSettings.Streaming.VdoNinjaEnabled;

    private IReadOnlyList<GoLiveRoomParticipantViewModel> Participants => BuildParticipants();

    private string RoomCode => BuildRoomCode();

    private IReadOnlyList<GoLiveSceneChipViewModel> SceneChips => BuildSceneChips();

    private IReadOnlyList<GoLiveMetricViewModel> RuntimeMetrics => BuildRuntimeMetrics();

    private IReadOnlyList<GoLiveMetricViewModel> StatusMetrics => BuildStatusMetrics();

    private static IReadOnlyList<GoLiveUtilitySourceViewModel> UtilitySources => StudioUtilitySources;

    private string GoLiveContentClass
    {
        get
        {
            var classes = new List<string> { GoLiveContentBaseClass };
            if (_fullProgramView)
            {
                classes.Add(GoLiveFullProgramClass);
            }

            if (!ShowLeftRail)
            {
                classes.Add(GoLiveHideLeftClass);
            }

            if (!ShowRightRail)
            {
                classes.Add(GoLiveHideRightClass);
            }

            return string.Join(' ', classes);
        }
    }

    private bool ShowLeftRail => _showLeftRail && !_fullProgramView;

    private bool ShowRightRail => _showRightRail && !_fullProgramView;

    private bool CanAddSceneCamera =>
        _mediaDevices.Any(device =>
            device.Kind == MediaDeviceKind.Camera
            && SceneCameras.All(camera => !string.Equals(camera.DeviceId, device.DeviceId, StringComparison.Ordinal)));

    private void EnsureStudioSurfaceState()
    {
        if (SceneChips.Count > 0 && SceneChips.All(scene => !string.Equals(scene.Id, _activeSceneId, StringComparison.Ordinal)))
        {
            _activeSceneId = SceneChips[0].Id;
        }
    }

    private IReadOnlyList<GoLiveAudioChannelViewModel> BuildAudioChannels()
    {
        var microphoneLevel = HasPrimaryMicrophone ? 100 : 0;
        var programLevel = GoLiveSession.State.HasActiveSession ? 100 : 0;
        var recordingLevel = GoLiveOutputRuntime.State.RecordingActive
            ? 100
            : _studioSettings.Streaming.LocalRecordingEnabled
                ? 55
                : 0;

        return
        [
            new(
                AudioMicChannelId,
                PrimaryMicrophoneLabel,
                HasPrimaryMicrophone ? PrimaryMicrophoneRoute : NoMicrophoneLabel,
                microphoneLevel),
            new(
                AudioProgramChannelId,
                AudioProgramChannelLabel,
                GoLiveSession.State.HasActiveSession ? ActiveSourceLabel : ProgramStandbyDetailLabel,
                programLevel),
            new(
                AudioRecordingChannelId,
                AudioRecordingChannelLabel,
                GoLiveOutputRuntime.State.RecordingActive
                    ? RecordingActiveMetricValue
                    : _studioSettings.Streaming.LocalRecordingEnabled
                        ? RecordingReadyDetailLabel
                        : AudioIdleDetailLabel,
                recordingLevel)
        ];
    }

    private IReadOnlyList<GoLiveRoomParticipantViewModel> BuildParticipants()
    {
        if (!IsRoomActive)
        {
            return [];
        }

        var participantLevel = GoLiveSession.State.HasActiveSession ? 100 : 52;
        return
        [
            new(
                HostParticipantId,
                HostParticipantInitial,
                HostParticipantName,
                DetailLocalProgramLabel,
                participantLevel,
                true)
        ];
    }

    private string BuildRoomCode()
    {
        if (!string.IsNullOrWhiteSpace(_studioSettings.Streaming.LiveKitRoomName))
        {
            return _studioSettings.Streaming.LiveKitRoomName;
        }

        if (!string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            return string.Concat(LocalRoomPrefix, SessionService.State.ScriptId);
        }

        return RoomCodeFallback;
    }

    private IReadOnlyList<GoLiveMetricViewModel> BuildRuntimeMetrics()
    {
        return
        [
            new(string.IsNullOrWhiteSpace(ActiveSourceLabel) ? CameraFallbackLabel : ActiveSourceLabel, ProgramMetricLabel),
            new(PrimaryMicrophoneLabel, MicrophoneMetricLabel),
            new(BuildRecordingMetricValue(), RecordingMetricLabel),
            new(BuildRuntimeEngineValue(), RuntimeEngineLabel)
        ];
    }

    private IReadOnlyList<GoLiveSceneChipViewModel> BuildSceneChips()
    {
        var primaryCamera = SceneCameras.Count > 0 ? SceneCameras[0] : null;
        var secondaryCamera = SceneCameras.Count > 1 ? SceneCameras[1] : null;
        var scenes = new List<GoLiveSceneChipViewModel>
        {
            new(PrimarySceneId, primaryCamera?.Label ?? MainSceneFallback, GoLiveSceneChipKind.Camera, primaryCamera?.SourceId),
            new(SecondarySceneId, secondaryCamera?.Label ?? InterviewSceneFallback, GoLiveSceneChipKind.Split, secondaryCamera?.SourceId),
            new(SceneSlidesId, SceneSlidesLabel, GoLiveSceneChipKind.Slides, null),
            new(PictureInPictureSceneId, PictureInPictureSceneLabel, GoLiveSceneChipKind.PictureInPicture, primaryCamera?.SourceId)
        };

        for (var index = 1; index <= _customSceneCount; index++)
        {
            scenes.Add(new($"{CustomScenePrefix}{index}", $"{CustomSceneTitlePrefix}{index + 4}", GoLiveSceneChipKind.Custom, null));
        }

        return scenes;
    }

    private IReadOnlyList<GoLiveMetricViewModel> BuildStatusMetrics()
    {
        var enabledDestinations = DestinationSummary.Count(destination => destination.IsEnabled);
        return
        [
            new(BitrateTelemetry, StatusBitrateLabel),
            new(FormatOutputResolution(_studioSettings.Streaming.OutputResolution), StatusOutputLabel),
            new(enabledDestinations.ToString(CultureInfo.InvariantCulture), ActiveDestinationsMetricLabel),
            new(ActiveSessionLabel, SessionMetricLabel)
        ];
    }

    private string BuildRecordingMetricValue()
    {
        if (GoLiveOutputRuntime.State.RecordingActive)
        {
            return RecordingActiveMetricValue;
        }

        return _studioSettings.Streaming.LocalRecordingEnabled
            ? RecordingReadyMetricValue
            : AudioIdleDetailLabel;
    }

    private string BuildRuntimeEngineValue()
    {
        return (GoLiveOutputRuntime.State.ObsActive, GoLiveOutputRuntime.State.LiveKitActive, GoLiveOutputRuntime.State.RecordingActive) switch
        {
            (true, true, _) => RuntimeEngineObsLiveKitValue,
            (true, false, _) => RuntimeEngineObsBrowserValue,
            (false, true, _) => RuntimeEngineLiveKitValue,
            (false, false, true) => RuntimeEngineRecorderValue,
            _ => RuntimeEngineIdleValue
        };
    }

    private IReadOnlyList<GoLiveDestinationSummaryViewModel> BuildDestinationSummary()
    {
        return
        [
            BuildDestinationSummary(
                GoLiveTargetCatalog.TargetIds.Obs,
                GoLiveTargetCatalog.TargetNames.Obs,
                SettingsPlatformLabel,
                _studioSettings.Streaming.ObsVirtualCameraEnabled,
                DestinationToneLocal),
            BuildDestinationSummary(
                GoLiveTargetCatalog.TargetIds.Recording,
                GoLiveTargetCatalog.TargetNames.Recording,
                ActiveWorkLabel,
                _studioSettings.Streaming.LocalRecordingEnabled,
                DestinationToneRecording),
            BuildRemoteDestinationSummary(
                GoLiveTargetCatalog.TargetIds.LiveKit,
                GoLiveTargetCatalog.TargetNames.LiveKit,
                GuestRoomLabel,
                _studioSettings.Streaming.LiveKitEnabled,
                DestinationToneLiveKit,
                _studioSettings.Streaming.LiveKitServerUrl,
                _studioSettings.Streaming.LiveKitRoomName,
                _studioSettings.Streaming.LiveKitToken),
            BuildRemoteDestinationSummary(
                GoLiveTargetCatalog.TargetIds.Youtube,
                GoLiveTargetCatalog.TargetNames.Youtube,
                RelayPlatformLabel,
                _studioSettings.Streaming.YoutubeEnabled,
                DestinationToneYoutube,
                _studioSettings.Streaming.YoutubeRtmpUrl,
                _studioSettings.Streaming.YoutubeStreamKey)
        ];
    }

    private GoLiveDestinationSummaryViewModel BuildDestinationSummary(
        string targetId,
        string name,
        string platformLabel,
        bool isEnabled,
        string tone)
    {
        var isReady = BuildDestinationIsReady(isEnabled, targetId);
        return new GoLiveDestinationSummaryViewModel(
            targetId,
            name,
            platformLabel,
            isEnabled,
            isReady,
            BuildLocalSummary(targetId),
            BuildTargetStatusLabel(isEnabled, targetId),
            tone);
    }

    private GoLiveDestinationSummaryViewModel BuildRemoteDestinationSummary(
        string targetId,
        string name,
        string platformLabel,
        bool isEnabled,
        string tone,
        params string[] requiredValues)
    {
        var isReady = BuildDestinationIsReady(isEnabled, targetId, requiredValues);
        return new GoLiveDestinationSummaryViewModel(
            targetId,
            name,
            platformLabel,
            isEnabled,
            isReady,
            BuildRemoteSummary(isEnabled, targetId, requiredValues),
            BuildTargetStatusLabel(isEnabled, targetId, requiredValues),
            tone);
    }

    private async Task ToggleDestinationSummaryAsync(string targetId)
    {
        if (string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Youtube, StringComparison.Ordinal))
        {
            await ToggleYoutubeSettingsAsync();
            return;
        }

        if (string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Obs, StringComparison.Ordinal))
        {
            await ToggleObsOutputAsync();
            return;
        }

        if (string.Equals(targetId, GoLiveTargetCatalog.TargetIds.LiveKit, StringComparison.Ordinal))
        {
            await ToggleLiveKitSettingsAsync();
            return;
        }

        if (string.Equals(targetId, GoLiveTargetCatalog.TargetIds.Recording, StringComparison.Ordinal))
        {
            await ToggleRecordingOutputAsync();
        }
    }

    private Task SelectStudioModeAsync(GoLiveStudioMode mode)
    {
        _activeStudioMode = mode;
        return Task.CompletedTask;
    }

    private Task SelectStudioTabAsync(GoLiveStudioTab tab)
    {
        _activeStudioTab = tab;
        return Task.CompletedTask;
    }

    private Task ToggleLeftRailAsync()
    {
        _showLeftRail = !_showLeftRail;
        return Task.CompletedTask;
    }

    private Task ToggleRightRailAsync()
    {
        _showRightRail = !_showRightRail;
        return Task.CompletedTask;
    }

    private Task ToggleFullProgramViewAsync()
    {
        _fullProgramView = !_fullProgramView;
        return Task.CompletedTask;
    }

    private Task SelectSceneAsync(string sceneId)
    {
        _activeSceneId = sceneId;
        var linkedSource = SceneChips.FirstOrDefault(scene => string.Equals(scene.Id, sceneId, StringComparison.Ordinal))?.SourceId;
        if (!string.IsNullOrWhiteSpace(linkedSource))
        {
            GoLiveSession.SelectSource(SceneCameras, linkedSource);
        }

        return Task.CompletedTask;
    }

    private Task AddSceneAsync()
    {
        _customSceneCount++;
        _activeSceneId = $"{CustomScenePrefix}{_customSceneCount}";
        return Task.CompletedTask;
    }

    private Task SelectSceneLayoutAsync(GoLiveSceneLayout layout)
    {
        _activeSceneLayout = layout;
        return Task.CompletedTask;
    }

    private Task SelectTransitionKindAsync(GoLiveTransitionKind kind)
    {
        _activeTransitionKind = kind;
        return Task.CompletedTask;
    }

    private Task SelectTransitionDurationAsync(GoLiveTransitionDuration duration)
    {
        _activeTransitionDuration = duration;
        return Task.CompletedTask;
    }

    private Task CreateRoomAsync()
    {
        _roomCreated = true;
        _activeStudioTab = GoLiveStudioTab.Room;
        return Task.CompletedTask;
    }

    private Task ToggleMuteAllGuestsAsync()
    {
        _muteAllGuests = !_muteAllGuests;
        return Task.CompletedTask;
    }

    private Task ToggleTalkbackAsync()
    {
        _talkbackEnabled = !_talkbackEnabled;
        return Task.CompletedTask;
    }

    private Task SendCueAsync()
    {
        _cueArmed = !_cueArmed;
        return Task.CompletedTask;
    }
}
