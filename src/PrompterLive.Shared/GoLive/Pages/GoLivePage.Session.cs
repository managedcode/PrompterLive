using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Models.Workspace;

namespace PrompterLive.Shared.Pages;

public partial class GoLivePage
{
    private const string CameraFallbackLabel = "No camera selected";
    private const string DefaultProgramTimerLabel = "00:00:00";
    private const string ProgramBadgeIdleLabel = "Ready";
    private const string ProgramBadgeLiveLabel = "Live";
    private const string ProgramBadgeRecordingLabel = "Rec";
    private const string ProgramBadgeStreamingRecordingLabel = "Live + Rec";
    private const string RecordingButtonLabel = "Start Recording";
    private const string RecordingStopLabel = "Stop Recording";
    private const string SessionIdleLabel = "Ready";
    private const string SessionRecordingLabel = "Recording";
    private const string SessionStreamingLabel = "Streaming";
    private const string SessionStreamingRecordingLabel = "Streaming + Recording";
    private const string StageFrameRate30Label = "30 FPS";
    private const string StageFrameRate60Label = "60 FPS";
    private const string StreamButtonLabel = "Start Stream";
    private const string StreamStopLabel = "Stop Stream";
    private const string SwitchButtonDisabledLabel = "On Program";
    private const string SwitchButtonLabel = "Switch";

    private SceneCameraSource? ActiveCamera => ResolveSessionSource(GoLiveSession.State.ActiveSourceId) ?? PreviewCamera;

    private string ActiveSessionLabel => (GoLiveSession.State.IsStreamActive, GoLiveSession.State.IsRecordingActive) switch
    {
        (true, true) => SessionStreamingRecordingLabel,
        (true, false) => SessionStreamingLabel,
        (false, true) => SessionRecordingLabel,
        _ => SessionIdleLabel
    };

    private string ActiveSourceLabel => ActiveCamera?.Label ?? CameraFallbackLabel;

    private bool CanControlProgram => SelectedCamera is not null;

    private bool CanSwitchProgram => SelectedCamera is not null
        && IsOperationalCamera(SelectedCamera)
        && !string.Equals(SelectedCamera.SourceId, ActiveCamera?.SourceId, StringComparison.Ordinal);

    private string PrimarySessionBadge => (GoLiveSession.State.IsStreamActive, GoLiveSession.State.IsRecordingActive) switch
    {
        (true, true) => ProgramBadgeStreamingRecordingLabel,
        (true, false) => ProgramBadgeLiveLabel,
        (false, true) => ProgramBadgeRecordingLabel,
        _ => ProgramBadgeIdleLabel
    };

    private string ProgramResolutionLabel => $"{ResolveResolutionDimensions(_studioSettings.Streaming.OutputResolution)} • {ActiveSourceLabel}";

    private static string ProgramTimerLabel => DefaultProgramTimerLabel;

    private string RecordingBadgeText => GoLiveSession.State.IsRecordingActive ? RecordingStopLabel : RecordingButtonLabel;

    private string SelectedSourceLabel => SelectedCamera?.Label ?? CameraFallbackLabel;

    private SceneCameraSource? SelectedCamera => ResolveSessionSource(GoLiveSession.State.SelectedSourceId) ?? ActiveCamera;

    private string StageFrameRateLabel => BuildStageFrameRateLabel(_studioSettings.Streaming.OutputResolution);

    private string StreamActionLabel => GoLiveSession.State.IsStreamActive ? StreamStopLabel : StreamButtonLabel;

    private string SwitchActionLabel => CanSwitchProgram ? SwitchButtonLabel : SwitchButtonDisabledLabel;

    private string BitrateTelemetry => $"{_studioSettings.Streaming.BitrateKbps} kbps";

    private void SyncGoLiveSessionState()
    {
        GoLiveSession.EnsureSession(
            SessionService.State.ScriptId,
            _screenTitle,
            _screenSubtitle,
            PrimaryMicrophoneLabel,
            _studioSettings.Streaming,
            SceneCameras);
    }

    private Task SelectSourceAsync(string sourceId)
    {
        GoLiveSession.SelectSource(SceneCameras, sourceId);
        return Task.CompletedTask;
    }

    private Task SwitchSelectedSourceAsync()
    {
        GoLiveSession.SwitchToSelectedSource(SceneCameras);
        return Task.CompletedTask;
    }

    private Task ToggleStreamSessionAsync()
    {
        GoLiveSession.ToggleStream(SceneCameras);
        return Task.CompletedTask;
    }

    private Task ToggleRecordingSessionAsync()
    {
        GoLiveSession.ToggleRecording(SceneCameras);
        return Task.CompletedTask;
    }

    private SceneCameraSource? ResolveSessionSource(string sourceId)
    {
        return SceneCameras.FirstOrDefault(camera => string.Equals(camera.SourceId, sourceId, StringComparison.Ordinal));
    }

    private static string FormatOutputResolution(StreamingResolutionPreset resolution)
    {
        return resolution switch
        {
            StreamingResolutionPreset.FullHd1080p60 => "1080p60",
            StreamingResolutionPreset.Hd720p30 => "720p30",
            StreamingResolutionPreset.UltraHd2160p30 => "2160p30",
            _ => "1080p30"
        };
    }

    private static string ResolveResolutionDimensions(StreamingResolutionPreset resolution)
    {
        return resolution switch
        {
            StreamingResolutionPreset.FullHd1080p60 => "1920 × 1080",
            StreamingResolutionPreset.Hd720p30 => "1280 × 720",
            StreamingResolutionPreset.UltraHd2160p30 => "3840 × 2160",
            _ => "1920 × 1080"
        };
    }

    private static string BuildStageFrameRateLabel(StreamingResolutionPreset resolution) => resolution switch
    {
        StreamingResolutionPreset.FullHd1080p60 => StageFrameRate60Label,
        _ => StageFrameRate30Label
    };

    private static bool IsOperationalCamera(SceneCameraSource camera) =>
        camera.Transform.Visible && camera.Transform.IncludeInOutput;
}
