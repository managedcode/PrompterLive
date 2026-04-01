using System.Globalization;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Components.GoLive;
using PrompterOne.Shared.GoLive.Models;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private const string GoLiveContentBaseClass = "gl-content";
    private const string GoLiveFullProgramClass = "gl-layout-fullpgm";
    private const string GoLiveHideLeftClass = "gl-hide-left";
    private const string GoLiveHideRightClass = "gl-hide-right";


    private string _activeSceneId = GoLiveText.Surface.PrimarySceneId;
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
        || ResolvePrimaryRoomDestination() is not null;

    private IReadOnlyList<GoLiveRoomParticipantViewModel> Participants => BuildParticipants();

    private string RoomCode => BuildRoomCode();

    private IReadOnlyList<GoLiveSceneChipViewModel> SceneChips => BuildSceneChips();

    private IReadOnlyList<GoLiveMetricViewModel> RuntimeMetrics => BuildRuntimeMetrics();

    private IReadOnlyList<GoLiveMetricViewModel> StatusMetrics => BuildStatusMetrics();

    private static IReadOnlyList<GoLiveUtilitySourceViewModel> UtilitySources => [];

    private IReadOnlyList<SceneCameraSource> VisibleSceneCameras =>
        _activeStudioMode == GoLiveStudioMode.Studio && SceneCameras.Count > 0
            ? [SceneCameras[0]]
            : SceneCameras;

    private string SourcesHeaderTitle =>
        _activeStudioMode == GoLiveStudioMode.Director
            ? GoLiveText.Surface.DirectorSourcesTitle
            : GoLiveText.Surface.SourcesTitle;

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
                GoLiveText.Surface.AudioMicChannelId,
                PrimaryMicrophoneLabel,
                HasPrimaryMicrophone ? PrimaryMicrophoneRoute : GoLiveText.Audio.NoMicrophoneLabel,
                microphoneLevel),
            new(
                GoLiveText.Surface.AudioProgramChannelId,
                GoLiveText.Surface.AudioProgramChannelLabel,
                GoLiveSession.State.HasActiveSession ? ActiveSourceLabel : GoLiveText.Surface.ProgramStandbyDetailLabel,
                programLevel),
            new(
                GoLiveText.Surface.AudioRecordingChannelId,
                GoLiveText.Surface.AudioRecordingChannelLabel,
                GoLiveOutputRuntime.State.RecordingActive
                    ? GoLiveText.Surface.RecordingActiveMetricValue
                    : _studioSettings.Streaming.LocalRecordingEnabled
                        ? GoLiveText.Surface.RecordingReadyDetailLabel
                        : GoLiveText.Surface.AudioIdleDetailLabel,
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
                GoLiveText.Surface.HostParticipantId,
                GoLiveText.Surface.HostParticipantInitial,
                GoLiveText.Surface.HostParticipantName,
                GoLiveText.Surface.DetailLocalProgramLabel,
                participantLevel,
                true)
        ];
    }

    private string BuildRoomCode()
    {
        var roomDestination = ResolvePrimaryRoomDestination();
        if (!string.IsNullOrWhiteSpace(roomDestination?.RoomName))
        {
            return roomDestination.RoomName;
        }

        if (!string.IsNullOrWhiteSpace(SessionService.State.ScriptId))
        {
            return string.Concat(GoLiveText.Surface.LocalRoomPrefix, SessionService.State.ScriptId);
        }

        return GoLiveText.Surface.RoomCodeFallback;
    }

    private IReadOnlyList<GoLiveMetricViewModel> BuildRuntimeMetrics()
    {
        return
        [
            new(string.IsNullOrWhiteSpace(ActiveSourceLabel) ? GoLiveText.Session.CameraFallbackLabel : ActiveSourceLabel, GoLiveText.Surface.ProgramMetricLabel),
            new(PrimaryMicrophoneLabel, GoLiveText.Surface.MicrophoneMetricLabel),
            new(BuildRecordingMetricValue(), GoLiveText.Surface.RecordingMetricLabel),
            new(BuildRuntimeEngineValue(), GoLiveText.Surface.RuntimeEngineLabel)
        ];
    }

    private IReadOnlyList<GoLiveSceneChipViewModel> BuildSceneChips()
    {
        var primaryCamera = SceneCameras.Count > 0 ? SceneCameras[0] : null;
        var secondaryCamera = SceneCameras.Count > 1 ? SceneCameras[1] : null;
        var scenes = new List<GoLiveSceneChipViewModel>
        {
            new(GoLiveText.Surface.PrimarySceneId, primaryCamera?.Label ?? GoLiveText.Surface.MainSceneFallback, GoLiveSceneChipKind.Camera, primaryCamera?.SourceId),
            new(GoLiveText.Surface.SecondarySceneId, secondaryCamera?.Label ?? GoLiveText.Surface.InterviewSceneFallback, GoLiveSceneChipKind.Split, secondaryCamera?.SourceId),
            new(GoLiveText.Surface.SceneSlidesId, GoLiveText.Surface.SceneSlidesLabel, GoLiveSceneChipKind.Slides, null),
            new(GoLiveText.Surface.PictureInPictureSceneId, GoLiveText.Surface.PictureInPictureSceneLabel, GoLiveSceneChipKind.PictureInPicture, primaryCamera?.SourceId)
        };

        for (var index = 1; index <= _customSceneCount; index++)
        {
            scenes.Add(new(
                $"{GoLiveText.Surface.CustomScenePrefix}{index}",
                $"{GoLiveText.Surface.CustomSceneTitlePrefix}{index + 4}",
                GoLiveSceneChipKind.Custom,
                null));
        }

        return scenes;
    }

    private IReadOnlyList<GoLiveMetricViewModel> BuildStatusMetrics()
    {
        var enabledDestinations = DestinationSummary.Count(destination => destination.IsEnabled);
        return
        [
            new(BitrateTelemetry, GoLiveText.Surface.StatusBitrateLabel),
            new(FormatOutputResolution(_studioSettings.Streaming.OutputResolution), GoLiveText.Surface.StatusOutputLabel),
            new(enabledDestinations.ToString(CultureInfo.InvariantCulture), GoLiveText.Surface.ActiveDestinationsMetricLabel),
            new(ActiveSessionLabel, GoLiveText.Surface.SessionMetricLabel)
        ];
    }

    private string BuildRecordingMetricValue()
    {
        if (GoLiveOutputRuntime.State.RecordingActive)
        {
            return GoLiveText.Surface.RecordingActiveMetricValue;
        }

        return _studioSettings.Streaming.LocalRecordingEnabled
            ? GoLiveText.Surface.RecordingReadyMetricValue
            : GoLiveText.Surface.AudioIdleDetailLabel;
    }

    private string BuildRuntimeEngineValue()
    {
        return (GoLiveOutputRuntime.State.ObsActive, GoLiveOutputRuntime.State.LiveKitActive, GoLiveOutputRuntime.State.RecordingActive) switch
        {
            (true, true, _) => GoLiveText.Surface.RuntimeEngineObsLiveKitValue,
            (true, false, _) => GoLiveText.Surface.RuntimeEngineObsBrowserValue,
            (false, true, _) => GoLiveText.Surface.RuntimeEngineLiveKitValue,
            (false, false, true) => GoLiveText.Surface.RuntimeEngineRecorderValue,
            _ => GoLiveText.Surface.RuntimeEngineIdleValue
        };
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
        _activeSceneId = $"{GoLiveText.Surface.CustomScenePrefix}{_customSceneCount}";
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
