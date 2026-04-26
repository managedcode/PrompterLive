using System.Globalization;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Streaming;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Preview;
using PrompterOne.Shared.Components.GoLive;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.GoLive.Models;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private const string GoLiveContentBaseClass = "gl-content";
    private const string GoLiveFullProgramClass = "gl-layout-fullpgm";
    private const string GoLiveHideLeftClass = "gl-hide-left";
    private const string GoLiveHideRightClass = "gl-hide-right";
    private const long KilobyteSize = 1024;
    private const long MegabyteSize = 1024 * KilobyteSize;
    private const long GigabyteSize = 1024 * MegabyteSize;
    private const string DownloadSaveMode = "download";
    private const string FileSystemSaveMode = "file-system";
    private const string MetricSeparator = " • ";
    private const string RecordingTakeFileStemSeparator = " take ";
    private const string RecordingTakeNumberFormat = "00";
    private const string RecordingTakeTitleFallback = "Block";
    private const string ResolutionSeparator = " × ";

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
    private int _recordingBlockIndex;
    private bool _roomCreated;
    private bool _showLeftRail = true;
    private bool _showRecordingBlockContext;
    private bool _showRightRail = true;
    private bool _talkbackEnabled;

    private IReadOnlyList<GoLiveAudioChannelViewModel> AudioChannels => BuildAudioChannels();

    private IReadOnlyList<GoLiveDestinationSummaryViewModel> DestinationSummary => BuildDestinationSummary();

    private bool IsRoomActive =>
        _roomCreated
        || GoLiveOutputRuntime.State.VdoNinjaActive
        || GoLiveOutputRuntime.State.LiveKitActive
        || GoLiveRemoteSourceRuntime.State.HasActiveConnections
        || ResolvePrimaryRoomDestination() is not null;

    private IReadOnlyList<GoLiveRoomParticipantViewModel> Participants => BuildParticipants();

    private IReadOnlyList<GoLiveRecordingBlockCueViewModel> RecordingBlockCues => BuildRecordingBlockCues();

    private IReadOnlyList<BlockPreviewModel> RecordingBlocks => BuildRecordingBlocks();

    private string RoomCode => BuildRoomCode();

    private IReadOnlyList<GoLiveSceneChipViewModel> SceneChips => BuildSceneChips();

    private IReadOnlyList<GoLiveMetricViewModel> RuntimeMetrics => BuildRuntimeMetrics();

    private IReadOnlyList<GoLiveMetricViewModel> StatusMetrics => BuildStatusMetrics();

    private static IReadOnlyList<GoLiveUtilitySourceViewModel> UtilitySources => [];

    private IReadOnlyList<SceneCameraSource> VisibleSceneCameras =>
        _activeStudioMode == GoLiveStudioMode.Studio && AvailableSceneSources.Count > 0
            ? [AvailableSceneSources[0]]
            : AvailableSceneSources;

    private string SourcesHeaderTitle =>
        _activeStudioMode == GoLiveStudioMode.Director
            ? Text(GoLiveText.Surface.DirectorSourcesTitle)
            : Text(GoLiveText.Surface.SourcesTitle);

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

    private bool ShowRecordingBlockContext =>
        _showRecordingBlockContext
        && GoLiveSession.State.IsRecordingActive
        && RecordingBlocks.Count > 0;

    private bool ShowRightRail => _showRightRail && !_fullProgramView;

    private bool CanMoveRecordingBlockBackward => NormalizeRecordingBlockIndex() > 0;

    private bool CanMoveRecordingBlockForward => NormalizeRecordingBlockIndex() < RecordingBlocks.Count - 1;

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
        var microphoneLevel = HasPrimaryMicrophone ? _primaryMicrophoneLevelPercent : 0;
        var programLevel = GoLiveOutputRuntime.State.Audio.ProgramLevelPercent;
        var recordingLevel = GoLiveOutputRuntime.State.Audio.RecordingLevelPercent;

        return
        [
            new(
                GoLiveText.Surface.AudioMicChannelId,
                PrimaryMicrophoneLabel,
                HasPrimaryMicrophone ? PrimaryMicrophoneRoute : Text(GoLiveText.Audio.NoMicrophoneLabel),
                microphoneLevel),
            new(
                GoLiveText.Surface.AudioProgramChannelId,
                Text(GoLiveText.Surface.AudioProgramChannelLabel),
                GoLiveSession.State.HasActiveSession ? ActiveSourceLabel : Text(GoLiveText.Surface.ProgramStandbyDetailLabel),
                programLevel),
            new(
                GoLiveText.Surface.AudioRecordingChannelId,
                Text(GoLiveText.Surface.AudioRecordingChannelLabel),
                GoLiveOutputRuntime.State.RecordingActive
                    ? Text(GoLiveText.Surface.RecordingActiveMetricValue)
                    : _studioSettings.Streaming.RecordingSettings.IsEnabled
                        ? Text(GoLiveText.Surface.RecordingReadyDetailLabel)
                        : Text(GoLiveText.Surface.AudioIdleDetailLabel),
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
        var participants = new List<GoLiveRoomParticipantViewModel>
        {
            new(
                GoLiveText.Surface.HostParticipantId,
                Text(GoLiveText.Surface.HostParticipantInitial),
                Text(GoLiveText.Surface.HostParticipantName),
                Text(GoLiveText.Surface.DetailLocalProgramLabel),
                participantLevel,
                true)
        };

        participants.AddRange(GoLiveRemoteSourceRuntime.State.Sources.Select(source => new GoLiveRoomParticipantViewModel(
            source.SourceId,
            ResolveParticipantInitial(source.Label),
            source.Label,
            ResolveParticipantPlatformLabel(source.PlatformKind),
            source.IsConnected ? participantLevel : 0,
            false)));

        return participants;
    }

    private IReadOnlyList<BlockPreviewModel> BuildRecordingBlocks()
    {
        var blocks = new List<BlockPreviewModel>();
        foreach (var segment in SessionService.State.PreviewSegments)
        {
            if (segment.Blocks.Count > 0)
            {
                blocks.AddRange(segment.Blocks);
                continue;
            }

            if (string.IsNullOrWhiteSpace(segment.Content))
            {
                continue;
            }

            blocks.Add(new BlockPreviewModel
            {
                Title = segment.Title,
                TargetWpm = segment.TargetWpm,
                Emotion = segment.Emotion,
                EmotionKey = segment.EmotionKey,
                Text = segment.Content
            });
        }

        return blocks;
    }

    private IReadOnlyList<GoLiveRecordingBlockCueViewModel> BuildRecordingBlockCues()
    {
        var blocks = RecordingBlocks;
        if (blocks.Count == 0)
        {
            return [];
        }

        var activeIndex = NormalizeRecordingBlockIndex(blocks.Count);
        var cues = new List<GoLiveRecordingBlockCueViewModel>(3);
        AddRecordingBlockCue(cues, blocks, activeIndex - 1, Text(UiTextKey.EditorFindPrevious.ToString()), UiTestIds.GoLive.RecordingBlockPrevious, isActive: false);
        AddRecordingBlockCue(cues, blocks, activeIndex, Text(UiTextKey.EditorStructureActiveBlock.ToString()), UiTestIds.GoLive.RecordingBlockActive, isActive: true);
        AddRecordingBlockCue(cues, blocks, activeIndex + 1, Text(UiTextKey.EditorFindNext.ToString()), UiTestIds.GoLive.RecordingBlockNext, isActive: false);
        return cues;
    }

    private string BuildRecordingFileStem()
    {
        var blocks = RecordingBlocks;
        if (blocks.Count == 0)
        {
            return _sessionTitle;
        }

        var activeIndex = NormalizeRecordingBlockIndex(blocks.Count);
        var blockTitle = blocks[activeIndex].Title;
        if (string.IsNullOrWhiteSpace(blockTitle))
        {
            blockTitle = string.Concat(
                RecordingTakeTitleFallback,
                " ",
                (activeIndex + 1).ToString(RecordingTakeNumberFormat, CultureInfo.InvariantCulture));
        }

        return string.Concat(
            _sessionTitle,
            RecordingTakeFileStemSeparator,
            (activeIndex + 1).ToString(RecordingTakeNumberFormat, CultureInfo.InvariantCulture),
            " ",
            blockTitle.Trim());
    }

    private static void AddRecordingBlockCue(
        List<GoLiveRecordingBlockCueViewModel> cues,
        IReadOnlyList<BlockPreviewModel> blocks,
        int index,
        string roleLabel,
        string testId,
        bool isActive)
    {
        if (index < 0 || index >= blocks.Count)
        {
            return;
        }

        var block = blocks[index];
        cues.Add(new GoLiveRecordingBlockCueViewModel(
            testId,
            roleLabel,
            block.Title,
            block.Text,
            isActive));
    }

    private string BuildRoomCode()
    {
        var roomDestination = ResolvePrimaryRoomDestination();
        if (!string.IsNullOrWhiteSpace(roomDestination?.RoomName))
        {
            return roomDestination.RoomName;
        }

        var remoteRoomName = GoLiveRemoteSourceRuntime.State.Connections
            .Select(connection => connection.RoomName)
            .FirstOrDefault(roomName => !string.IsNullOrWhiteSpace(roomName));
        if (!string.IsNullOrWhiteSpace(remoteRoomName))
        {
            return remoteRoomName;
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
            new(
                GoLiveMetricIds.RuntimeCamera,
                string.IsNullOrWhiteSpace(ActiveSourceLabel) ? Text(GoLiveText.Session.CameraFallbackLabel) : ActiveSourceLabel,
                Text(GoLiveText.Surface.ProgramMetricLabel)),
            new(
                GoLiveMetricIds.RuntimeMicrophone,
                PrimaryMicrophoneLabel,
                Text(GoLiveText.Surface.MicrophoneMetricLabel)),
            new(
                GoLiveMetricIds.RuntimeRecording,
                BuildRecordingMetricValue(),
                Text(GoLiveText.Surface.RecordingMetricLabel)),
            new(
                GoLiveMetricIds.RuntimeEngine,
                BuildRuntimeEngineValue(),
                Text(GoLiveText.Surface.RuntimeEngineLabel))
        ];
    }

    private IReadOnlyList<GoLiveSceneChipViewModel> BuildSceneChips()
    {
        var primaryCamera = AvailableSceneSources.Count > 0 ? AvailableSceneSources[0] : null;
        var secondaryCamera = AvailableSceneSources.Count > 1 ? AvailableSceneSources[1] : null;
        var scenes = new List<GoLiveSceneChipViewModel>
        {
            new(GoLiveText.Surface.PrimarySceneId, primaryCamera is null ? Text(GoLiveText.Session.CameraFallbackLabel) : MediaDeviceLabelSanitizer.Sanitize(primaryCamera.Label), GoLiveSceneChipKind.Camera, primaryCamera?.SourceId),
            new(GoLiveText.Surface.SecondarySceneId, secondaryCamera is null ? Text(GoLiveText.Surface.InterviewSceneFallback) : MediaDeviceLabelSanitizer.Sanitize(secondaryCamera.Label), GoLiveSceneChipKind.Split, secondaryCamera?.SourceId),
            new(GoLiveText.Surface.SceneSlidesId, Text(GoLiveText.Surface.SceneSlidesLabel), GoLiveSceneChipKind.Slides, null),
            new(GoLiveText.Surface.PictureInPictureSceneId, Text(GoLiveText.Surface.PictureInPictureSceneLabel), GoLiveSceneChipKind.PictureInPicture, primaryCamera?.SourceId)
        };

        for (var index = 1; index <= _customSceneCount; index++)
        {
            scenes.Add(new(
                $"{GoLiveText.Surface.CustomScenePrefix}{index}",
                string.Concat(Text(GoLiveText.Surface.CustomSceneTitlePrefix), index + 4),
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
            new(
                GoLiveMetricIds.StatusBitrate,
                BuildBitrateMetricValue(),
                Text(GoLiveText.Surface.StatusBitrateLabel)),
            new(
                GoLiveMetricIds.StatusOutput,
                BuildOutputMetricValue(),
                Text(GoLiveText.Surface.StatusOutputLabel)),
            new(
                GoLiveMetricIds.StatusDestinations,
                enabledDestinations.ToString(CultureInfo.InvariantCulture),
                Text(GoLiveText.Surface.ActiveDestinationsMetricLabel)),
            new(
                GoLiveMetricIds.StatusSession,
                ActiveSessionLabel,
                Text(GoLiveText.Surface.SessionMetricLabel))
        ];
    }

    private string BuildRecordingMetricValue()
    {
        var recording = GoLiveOutputRuntime.State.Recording;
        if (GoLiveOutputRuntime.State.RecordingActive)
        {
            return FormatFileSize(recording.SizeBytes);
        }

        return _studioSettings.Streaming.RecordingSettings.IsEnabled
            ? Text(GoLiveText.Surface.RecordingReadyMetricValue)
            : Text(GoLiveText.Surface.AudioIdleDetailLabel);
    }

    private string BuildRuntimeEngineValue()
    {
        var activeModules = new List<string>();
        if (GoLiveOutputRuntime.State.RecordingActive)
        {
            activeModules.Add(string.Join(MetricSeparator, new[]
            {
                Text(GoLiveText.Surface.RuntimeEngineRecorderValue),
                ResolveRecordingContainerValue(),
                ResolveRecordingSaveModeValue()
            }.Where(value => !string.IsNullOrWhiteSpace(value))));
        }

        if (GoLiveOutputRuntime.State.LiveKitActive)
        {
            activeModules.Add(GoLiveText.Surface.RuntimeEngineLiveKitValue);
        }

        if (GoLiveOutputRuntime.State.VdoNinjaActive)
        {
            activeModules.Add(GoLiveText.Surface.RuntimeEngineVdoNinjaValue);
        }

        return activeModules.Count == 0
            ? Text(GoLiveText.Surface.RuntimeEngineIdleValue)
            : string.Join(MetricSeparator, activeModules);
    }

    private string BuildBitrateMetricValue()
    {
        var videoBitrateKbps = GoLiveOutputRuntime.State.RecordingActive && GoLiveOutputRuntime.State.Recording.VideoBitrateKbps > 0
            ? GoLiveOutputRuntime.State.Recording.VideoBitrateKbps
            : _studioSettings.Streaming.ProgramCaptureSettings.BitrateKbps;

        return string.Concat(videoBitrateKbps.ToString(CultureInfo.InvariantCulture), " kbps");
    }

    private string BuildOutputMetricValue()
    {
        var program = GoLiveOutputRuntime.State.Program;
        if (program.Width > 0 && program.Height > 0)
        {
            return string.Concat(
                program.Width.ToString(CultureInfo.InvariantCulture),
                ResolutionSeparator,
                program.Height.ToString(CultureInfo.InvariantCulture));
        }

        return ResolveResolutionDimensions(_studioSettings.Streaming.ProgramCaptureSettings.ResolutionPreset);
    }

    private static string FormatFileSize(long sizeBytes)
    {
        if (sizeBytes >= GigabyteSize)
        {
            return string.Concat(
                (sizeBytes / (double)GigabyteSize).ToString("0.0", CultureInfo.InvariantCulture),
                " GB");
        }

        if (sizeBytes >= MegabyteSize)
        {
            return string.Concat(
                (sizeBytes / (double)MegabyteSize).ToString("0.0", CultureInfo.InvariantCulture),
                " MB");
        }

        if (sizeBytes >= KilobyteSize)
        {
            return string.Concat(
                (sizeBytes / (double)KilobyteSize).ToString("0.0", CultureInfo.InvariantCulture),
                " KB");
        }

        return string.Concat(sizeBytes.ToString(CultureInfo.InvariantCulture), " B");
    }

    private string ResolveRecordingContainerValue()
    {
        var mimeType = GoLiveOutputRuntime.State.Recording.MimeType;
        if (mimeType.Contains("mp4", StringComparison.OrdinalIgnoreCase))
        {
            return GoLiveText.Surface.RecordingContainerMp4Value;
        }

        if (mimeType.Contains("webm", StringComparison.OrdinalIgnoreCase))
        {
            return GoLiveText.Surface.RecordingContainerWebmValue;
        }

        return GoLiveOutputRuntime.State.Recording.RequestedContainer;
    }

    private string ResolveRecordingSaveModeValue() => GoLiveOutputRuntime.State.Recording.SaveMode switch
    {
        FileSystemSaveMode => Text(GoLiveText.Surface.RecordingSaveModeLocalFileValue),
        DownloadSaveMode => Text(GoLiveText.Surface.RecordingSaveModeBrowserDownloadValue),
        _ => string.Empty
    };

    private Task SelectStudioModeAsync(GoLiveStudioMode mode)
    {
        _activeStudioMode = mode;
        return Task.CompletedTask;
    }

    private Task SelectStudioTabAsync(GoLiveStudioTab tab)
    {
        _activeStudioTab = tab;
        return SyncPrimaryMicrophoneMonitorAsync();
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
            GoLiveSession.SelectSource(AvailableSceneSources, linkedSource);
        }

        return Task.CompletedTask;
    }

    private Task AddSceneAsync()
    {
        _customSceneCount++;
        _activeSceneId = $"{GoLiveText.Surface.CustomScenePrefix}{_customSceneCount}";
        return Task.CompletedTask;
    }

    private async Task SelectSceneLayoutAsync(GoLiveSceneLayout layout)
    {
        _activeSceneLayout = layout;
        if (!GoLiveOutputRuntime.State.HasActiveOutputs)
        {
            return;
        }

        await EnsurePageReadyAsync();
        var programCamera = ActiveCamera ?? SelectedCamera;
        if (programCamera is null)
        {
            return;
        }

        await GoLiveOutputRuntime.UpdateProgramSourceAsync(BuildRuntimeRequest(programCamera));
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

    private string ResolveParticipantInitial(string label) =>
        string.IsNullOrWhiteSpace(label)
            ? Text(GoLiveText.Surface.HostParticipantInitial)
            : label.Trim()[0].ToString(CultureInfo.InvariantCulture).ToUpperInvariant();

    private string ResolveParticipantPlatformLabel(StreamingPlatformKind platformKind) =>
        platformKind switch
        {
            StreamingPlatformKind.LiveKit => GoLiveTargetCatalog.TargetNames.LiveKit,
            StreamingPlatformKind.VdoNinja => GoLiveTargetCatalog.TargetNames.VdoNinja,
            _ => Text(GoLiveText.Surface.DetailLocalProgramLabel)
        };
}
