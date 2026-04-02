using System.Globalization;
using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.GoLive.Models;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    private const string SessionBadgeIdleCssClass = "gl-badge-idle";
    private const string SessionBadgeLiveCssClass = "gl-badge-live";
    private const string SessionBadgeRecordingCssClass = "gl-badge-rec";

    private SceneCameraSource? ActiveCamera => ResolveSessionSource(GoLiveSession.State.ActiveSourceId) ?? PreviewCamera;

    private string ActiveSessionLabel => (GoLiveSession.State.IsStreamActive, GoLiveSession.State.IsRecordingActive) switch
    {
        (true, true) => GoLiveText.Session.SessionStreamingRecordingLabel,
        (true, false) => GoLiveText.Session.SessionStreamingLabel,
        (false, true) => GoLiveText.Session.SessionRecordingLabel,
        _ => GoLiveText.Session.SessionIdleLabel
    };

    private string ActiveSourceLabel => ActiveCamera?.Label ?? GoLiveText.Session.CameraFallbackLabel;

    private bool CanControlProgram => SelectedCamera is not null;

    private bool CanSwitchProgram => SelectedCamera is not null
        && SelectedCamera.Transform.Visible
        && !string.Equals(SelectedCamera.SourceId, ActiveCamera?.SourceId, StringComparison.Ordinal);

    private bool IsLiveSessionActive => !string.Equals(SessionIndicatorState, GoLiveText.Session.IdleStateValue, StringComparison.Ordinal);

    private string SessionIndicatorState => (GoLiveSession.State.IsStreamActive, GoLiveSession.State.IsRecordingActive) switch
    {
        (true, true) => GoLiveText.Session.RecordingStateValue,
        (true, false) => GoLiveText.Session.StreamingStateValue,
        (false, true) => GoLiveText.Session.RecordingStateValue,
        _ => GoLiveText.Session.IdleStateValue
    };

    private string PrimarySessionBadge => (GoLiveSession.State.IsStreamActive, GoLiveSession.State.IsRecordingActive) switch
    {
        (true, true) => GoLiveText.Session.ProgramBadgeStreamingRecordingLabel,
        (true, false) => GoLiveText.Session.ProgramBadgeLiveLabel,
        (false, true) => GoLiveText.Session.ProgramBadgeRecordingLabel,
        _ => GoLiveText.Session.ProgramBadgeIdleLabel
    };

    private string ProgramResolutionLabel => $"{ResolveResolutionDimensions(_studioSettings.Streaming.OutputResolution)} • {ActiveSourceLabel}";

    private string ProgramTimerLabel => FormatSessionElapsed(SessionStartedAt);

    private string SessionBadgeCssClass => (GoLiveSession.State.IsStreamActive, GoLiveSession.State.IsRecordingActive) switch
    {
        (true, _) => SessionBadgeLiveCssClass,
        (false, true) => SessionBadgeRecordingCssClass,
        _ => SessionBadgeIdleCssClass
    };

    private string SelectedSourceLabel => SelectedCamera?.Label ?? GoLiveText.Session.CameraFallbackLabel;

    private SceneCameraSource? SelectedCamera => ResolveSessionSource(GoLiveSession.State.SelectedSourceId) ?? ActiveCamera;

    private string StageFrameRateLabel => BuildStageFrameRateLabel(_studioSettings.Streaming.OutputResolution);

    private string StreamActionLabel => GoLiveSession.State.IsStreamActive
        ? GoLiveText.Session.StreamStopLabel
        : GoLiveText.Session.StreamButtonLabel;

    private string StreamButtonDisplayText => StreamActionLabel.ToUpperInvariant();

    private string SwitchActionLabel => CanSwitchProgram
        ? GoLiveText.Session.SwitchButtonLabel
        : GoLiveText.Session.SwitchButtonDisabledLabel;

    private string BitrateTelemetry => $"{_studioSettings.Streaming.BitrateKbps} kbps";

    private DateTimeOffset? SessionStartedAt => GoLiveSession.State.IsRecordingActive
        ? GoLiveSession.State.RecordingStartedAt ?? GoLiveSession.State.StreamStartedAt
        : GoLiveSession.State.StreamStartedAt;

    private void SyncGoLiveSessionState()
    {
        GoLiveSession.EnsureSession(
            SessionService.State.ScriptId,
            _sessionTitle,
            _sessionSubtitle,
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
            GoLiveText.Session.SwitchProgramOperation,
            GoLiveText.Session.SwitchProgramMessage,
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
                    GoLiveText.Session.StopStreamOperation,
                    GoLiveText.Session.StopStreamMessage,
                    async () =>
                    {
                        await GoLiveOutputRuntime.StopStreamAsync();
                        GoLiveSession.StopStream();
                    });
                return;
            }

            await EnsureSelectedCameraReadyForProgramAsync();
            await Diagnostics.RunAsync(
                GoLiveText.Session.StartStreamOperation,
                GoLiveText.Session.StartStreamMessage,
                async () =>
                {
                    await GoLiveOutputRuntime.StartStreamAsync(BuildRuntimeRequest(SelectedCamera));

                    if (!GoLiveOutputRuntime.State.HasLiveOutputs)
                    {
                        Diagnostics.ReportRecoverable(
                            GoLiveText.Session.StreamPrerequisiteOperation,
                            GoLiveText.Session.StreamPrerequisiteMessage,
                            GoLiveText.Session.StreamPrerequisiteDetail);
                        return;
                    }

                    GoLiveSession.StartStream(SceneCameras);
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
                        GoLiveText.Session.StopRecordingOperation,
                        GoLiveText.Session.StopRecordingMessage,
                        async () =>
                    {
                        await GoLiveOutputRuntime.StopRecordingAsync();
                        GoLiveSession.StopRecording();
                    });
                return;
            }

            await EnsureSelectedCameraReadyForProgramAsync();
            await EnsureRecordingOutputEnabledAsync();
            await Diagnostics.RunAsync(
                    GoLiveText.Session.StartRecordingOperation,
                    GoLiveText.Session.StartRecordingMessage,
                    async () =>
                {
                    await GoLiveOutputRuntime.StartRecordingAsync(BuildRuntimeRequest(SelectedCamera));
                    GoLiveSession.StartRecording(SceneCameras);
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
            StreamingResolutionPreset.FullHd1080p60 => GoLiveText.Surface.StreamFormatFullHd60,
            StreamingResolutionPreset.Hd720p30 => GoLiveText.Surface.StreamFormatHd30,
            StreamingResolutionPreset.UltraHd2160p30 => GoLiveText.Surface.StreamFormatUltraHd30,
            _ => GoLiveText.Surface.StreamFormatFullHd30
        };
    }

    private static string ResolveResolutionDimensions(StreamingResolutionPreset resolution)
    {
        return resolution switch
        {
            StreamingResolutionPreset.FullHd1080p60 => GoLiveText.Surface.ResolutionDimensionsFullHd,
            StreamingResolutionPreset.Hd720p30 => GoLiveText.Surface.ResolutionDimensionsHd,
            StreamingResolutionPreset.UltraHd2160p30 => GoLiveText.Surface.ResolutionDimensionsUltraHd,
            _ => GoLiveText.Surface.ResolutionDimensionsFullHd
        };
    }

    private static string BuildStageFrameRateLabel(StreamingResolutionPreset resolution) => resolution switch
    {
        StreamingResolutionPreset.FullHd1080p60 => GoLiveText.Session.StageFrameRate60Label,
        _ => GoLiveText.Session.StageFrameRate30Label
    };

    private static string FormatSessionElapsed(DateTimeOffset? startedAt)
    {
        if (startedAt is null)
        {
            return GoLiveText.Session.DefaultProgramTimerLabel;
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
