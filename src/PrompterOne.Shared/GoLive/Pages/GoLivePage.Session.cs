using System.Globalization;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private const string CameraFallbackLabel = "No camera selected";
    private const string DefaultProgramTimerLabel = "00:00:00";
    private const string GoLiveStartRecordingMessage = "Unable to start recording right now.";
    private const string GoLiveStartRecordingOperation = "Go Live start recording";
    private const string GoLiveStopRecordingMessage = "Unable to stop recording right now.";
    private const string GoLiveStopRecordingOperation = "Go Live stop recording";
    private const string ProgramBadgeIdleLabel = "Ready";
    private const string ProgramBadgeLiveLabel = "Live";
    private const string ProgramBadgeRecordingLabel = "Rec";
    private const string ProgramBadgeStreamingRecordingLabel = "Live + Rec";
    private const string RecordingIndicatorLabel = "REC";
    private const string SessionIdleLabel = "Ready";
    private const string SessionBadgeIdleCssClass = "gl-badge-idle";
    private const string SessionBadgeLiveCssClass = "gl-badge-live";
    private const string SessionBadgeRecordingCssClass = "gl-badge-rec";
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
        && SelectedCamera.Transform.Visible
        && !string.Equals(SelectedCamera.SourceId, ActiveCamera?.SourceId, StringComparison.Ordinal);

    private bool IsLiveSessionActive => GoLiveSession.State.IsStreamActive || GoLiveSession.State.IsRecordingActive;

    private string PrimarySessionBadge => (GoLiveSession.State.IsStreamActive, GoLiveSession.State.IsRecordingActive) switch
    {
        (true, true) => ProgramBadgeStreamingRecordingLabel,
        (true, false) => ProgramBadgeLiveLabel,
        (false, true) => ProgramBadgeRecordingLabel,
        _ => ProgramBadgeIdleLabel
    };

    private string ProgramResolutionLabel => $"{ResolveResolutionDimensions(_studioSettings.Streaming.OutputResolution)} • {ActiveSourceLabel}";

    private string ProgramTimerLabel => FormatSessionElapsed(SessionStartedAt);

    private string SessionBadgeCssClass => (GoLiveSession.State.IsStreamActive, GoLiveSession.State.IsRecordingActive) switch
    {
        (true, _) => SessionBadgeLiveCssClass,
        (false, true) => SessionBadgeRecordingCssClass,
        _ => SessionBadgeIdleCssClass
    };

    private string SelectedSourceLabel => SelectedCamera?.Label ?? CameraFallbackLabel;

    private SceneCameraSource? SelectedCamera => ResolveSessionSource(GoLiveSession.State.SelectedSourceId) ?? ActiveCamera;

    private string StageFrameRateLabel => BuildStageFrameRateLabel(_studioSettings.Streaming.OutputResolution);

    private string StreamActionLabel => GoLiveSession.State.IsStreamActive ? StreamStopLabel : StreamButtonLabel;

    private string StreamButtonDisplayText => StreamActionLabel.ToUpperInvariant();

    private string SwitchActionLabel => CanSwitchProgram ? SwitchButtonLabel : SwitchButtonDisabledLabel;

    private string BitrateTelemetry => $"{_studioSettings.Streaming.BitrateKbps} kbps";

    private DateTimeOffset? SessionStartedAt => GoLiveSession.State.IsRecordingActive
        ? GoLiveSession.State.RecordingStartedAt ?? GoLiveSession.State.StreamStartedAt
        : GoLiveSession.State.StreamStartedAt;

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
        return SelectSourceAfterReadyAsync(sourceId);
    }

    private async Task SelectSourceAfterReadyAsync(string sourceId)
    {
        await EnsurePageReadyAsync();
        GoLiveSession.SelectSource(SceneCameras, sourceId);
    }

    private async Task SwitchSelectedSourceAsync()
    {
        await EnsurePageReadyAsync();
        await EnsureSelectedCameraReadyForProgramAsync();
        await Diagnostics.RunAsync(
            GoLiveSwitchProgramOperation,
            GoLiveSwitchProgramMessage,
            async () =>
            {
                var nextCamera = SelectedCamera;
                if ((GoLiveSession.State.IsStreamActive || GoLiveSession.State.IsRecordingActive) && nextCamera is not null)
                {
                    await GoLiveOutputRuntime.UpdateProgramSourceAsync(BuildRuntimeRequest(nextCamera));
                }

                GoLiveSession.SwitchToSelectedSource(SceneCameras);
            });
    }

    private async Task ToggleStreamSessionAsync()
    {
        await RunSerializedInteractionAsync(async () =>
        {
            if (GoLiveSession.State.IsStreamActive)
            {
                await Diagnostics.RunAsync(
                    GoLiveStopStreamOperation,
                    GoLiveStopStreamMessage,
                    async () =>
                    {
                        await GoLiveOutputRuntime.StopStreamAsync();
                        GoLiveSession.ToggleStream(SceneCameras);
                    });
                return;
            }

            await EnsureSelectedCameraReadyForProgramAsync();
            await Diagnostics.RunAsync(
                GoLiveStartStreamOperation,
                GoLiveStartStreamMessage,
                async () =>
                {
                    await GoLiveOutputRuntime.StartStreamAsync(BuildRuntimeRequest(SelectedCamera));
                    GoLiveSession.ToggleStream(SceneCameras);
                });
        });
    }

    private async Task ToggleRecordingSessionAsync()
    {
        await RunSerializedInteractionAsync(async () =>
        {
            if (GoLiveSession.State.IsRecordingActive)
            {
                await Diagnostics.RunAsync(
                    GoLiveStopRecordingOperation,
                    GoLiveStopRecordingMessage,
                    async () =>
                    {
                        await GoLiveOutputRuntime.StopRecordingAsync();
                        GoLiveSession.ToggleRecording(SceneCameras);
                    });
                return;
            }

            await EnsureSelectedCameraReadyForProgramAsync();
            await EnsureRecordingOutputEnabledAsync();
            await Diagnostics.RunAsync(
                GoLiveStartRecordingOperation,
                GoLiveStartRecordingMessage,
                async () =>
                {
                    await GoLiveOutputRuntime.StartRecordingAsync(BuildRuntimeRequest(SelectedCamera));
                    GoLiveSession.ToggleRecording(SceneCameras);
                });
        });
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

    private static string FormatSessionElapsed(DateTimeOffset? startedAt)
    {
        if (startedAt is null)
        {
            return DefaultProgramTimerLabel;
        }

        var elapsed = DateTimeOffset.UtcNow - startedAt.Value;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        return elapsed.TotalHours >= 1
            ? elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
            : elapsed.ToString(@"mm\:ss", CultureInfo.InvariantCulture).Insert(0, "00:");
    }

    private async Task EnsureSelectedCameraReadyForProgramAsync()
    {
        if (SelectedCamera is null ||
            !SelectedCamera.Transform.Visible ||
            SelectedCamera.Transform.IncludeInOutput)
        {
            return;
        }

        MediaSceneService.SetIncludeInOutput(SelectedCamera.SourceId, true);
        await PersistSceneAsync();
    }

    private async Task EnsureRecordingOutputEnabledAsync()
    {
        if (_studioSettings.Streaming.LocalRecordingEnabled)
        {
            return;
        }

        _studioSettings = _studioSettings with
        {
            Streaming = _studioSettings.Streaming with
            {
                LocalRecordingEnabled = true,
                OutputMode = StreamingOutputMode.LocalRecording
            }
        };

        await PersistStudioSettingsAsync();
    }
}
