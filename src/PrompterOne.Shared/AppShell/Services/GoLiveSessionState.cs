using PrompterOne.Core.Models.Media;
using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Shared.Services;

public sealed record GoLiveSessionState(
    string ScriptId,
    string ScriptTitle,
    string ScriptSubtitle,
    string SelectedSourceId,
    string SelectedSourceLabel,
    string ActiveSourceId,
    string ActiveSourceLabel,
    string PrimaryMicrophoneLabel,
    StreamingResolutionPreset OutputResolution,
    int BitrateKbps,
    bool IsStreamActive,
    bool IsRecordingActive,
    DateTimeOffset? StreamStartedAt,
    DateTimeOffset? RecordingStartedAt)
{
    public static GoLiveSessionState Default { get; } = new(
        ScriptId: string.Empty,
        ScriptTitle: string.Empty,
        ScriptSubtitle: string.Empty,
        SelectedSourceId: string.Empty,
        SelectedSourceLabel: string.Empty,
        ActiveSourceId: string.Empty,
        ActiveSourceLabel: string.Empty,
        PrimaryMicrophoneLabel: string.Empty,
        OutputResolution: StreamingResolutionPreset.FullHd1080p30,
        BitrateKbps: 0,
        IsStreamActive: false,
        IsRecordingActive: false,
        StreamStartedAt: null,
        RecordingStartedAt: null);

    public bool HasActiveSession => IsStreamActive || IsRecordingActive;
}

internal sealed partial class GoLiveSessionService : IDisposable
{
    private readonly CrossTabMessageBus _crossTabMessageBus;

    public GoLiveSessionService(CrossTabMessageBus crossTabMessageBus)
    {
        _crossTabMessageBus = crossTabMessageBus;
        _crossTabMessageBus.MessageReceived += HandleCrossTabMessageAsync;
    }

    public event Action? StateChanged;

    public GoLiveSessionState State { get; private set; } = GoLiveSessionState.Default;

    public void EnsureSession(
        string scriptId,
        string scriptTitle,
        string scriptSubtitle,
        string primaryMicrophoneLabel,
        StreamStudioSettings streaming,
        IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var selectedSource = ResolveSource(sceneCameras, State.SelectedSourceId)
            ?? ResolveSource(sceneCameras, State.ActiveSourceId)
            ?? ResolveDefaultSource(sceneCameras);
        var activeSource = ResolveOperationalSource(sceneCameras, State.ActiveSourceId)
            ?? ResolveOperationalSource(sceneCameras, selectedSource?.SourceId ?? string.Empty)
            ?? ResolveDefaultSource(sceneCameras);

        ApplyState(State with
        {
            ScriptId = scriptId ?? string.Empty,
            ScriptTitle = scriptTitle ?? string.Empty,
            ScriptSubtitle = scriptSubtitle ?? string.Empty,
            SelectedSourceId = selectedSource?.SourceId ?? string.Empty,
            SelectedSourceLabel = selectedSource?.Label ?? string.Empty,
            ActiveSourceId = activeSource?.SourceId ?? string.Empty,
            ActiveSourceLabel = activeSource?.Label ?? string.Empty,
            PrimaryMicrophoneLabel = primaryMicrophoneLabel ?? string.Empty,
            OutputResolution = streaming.ProgramCaptureSettings.ResolutionPreset,
            BitrateKbps = streaming.ProgramCaptureSettings.BitrateKbps
        }, publishToCrossTab: false);
    }

    public void SelectSource(IReadOnlyList<SceneCameraSource> sceneCameras, string sourceId)
    {
        var source = ResolveSource(sceneCameras, sourceId);
        if (source is null)
        {
            return;
        }

        ApplyState(State with
        {
            SelectedSourceId = source.SourceId,
            SelectedSourceLabel = source.Label
        }, publishToCrossTab: false);
    }

    public void SwitchToSelectedSource(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var source = ResolveOperationalSource(sceneCameras, State.SelectedSourceId)
            ?? ResolveDefaultSource(sceneCameras);
        if (source is null)
        {
            return;
        }

        ApplyState(State with
        {
            ActiveSourceId = source.SourceId,
            ActiveSourceLabel = source.Label
        }, publishToCrossTab: State.HasActiveSession);
    }

    public void StartStream(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        if (State.IsStreamActive)
        {
            return;
        }

        SwitchToSelectedSource(sceneCameras);
        ApplyState(State with
        {
            IsStreamActive = true,
            StreamStartedAt = DateTimeOffset.UtcNow
        }, publishToCrossTab: true);
    }

    public void StopStream()
    {
        if (!State.IsStreamActive)
        {
            return;
        }

        ApplyState(State with
        {
            IsStreamActive = false,
            StreamStartedAt = null
        }, publishToCrossTab: true);
    }

    public void StartRecording(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        if (State.IsRecordingActive)
        {
            return;
        }

        SwitchToSelectedSource(sceneCameras);
        ApplyState(State with
        {
            IsRecordingActive = true,
            RecordingStartedAt = DateTimeOffset.UtcNow
        }, publishToCrossTab: true);
    }

    public void StopRecording()
    {
        if (!State.IsRecordingActive)
        {
            return;
        }

        ApplyState(State with
        {
            IsRecordingActive = false,
            RecordingStartedAt = null
        }, publishToCrossTab: true);
    }

    public void ToggleStream(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        if (State.IsStreamActive)
        {
            StopStream();
            return;
        }

        StartStream(sceneCameras);
    }

    public void ToggleRecording(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        if (State.IsRecordingActive)
        {
            StopRecording();
            return;
        }

        StartRecording(sceneCameras);
    }

    public void SetState(GoLiveSessionState nextState)
    {
        ApplyState(nextState, publishToCrossTab: false);
    }

    private void ApplyState(GoLiveSessionState nextState, bool publishToCrossTab)
    {
        if (EqualityComparer<GoLiveSessionState>.Default.Equals(State, nextState))
        {
            return;
        }

        State = nextState;
        StateChanged?.Invoke();

        if (publishToCrossTab)
        {
            PublishStateInBackground(nextState);
        }
    }

    private static SceneCameraSource? ResolveDefaultSource(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        SceneCameraSource? firstVisibleIncludedSource = null;
        SceneCameraSource? firstVisibleSource = null;

        for (var index = 0; index < sceneCameras.Count; index++)
        {
            var camera = sceneCameras[index];
            if (camera.Transform.Visible && camera.Transform.IncludeInOutput)
            {
                return camera;
            }

            if (firstVisibleSource is null && camera.Transform.Visible)
            {
                firstVisibleSource = camera;
            }

            firstVisibleIncludedSource ??= camera;
        }

        return firstVisibleSource ?? firstVisibleIncludedSource;
    }

    private static SceneCameraSource? ResolveSource(IReadOnlyList<SceneCameraSource> sceneCameras, string sourceId)
    {
        return sceneCameras.FirstOrDefault(camera => string.Equals(camera.SourceId, sourceId, StringComparison.Ordinal));
    }

    private static SceneCameraSource? ResolveOperationalSource(IReadOnlyList<SceneCameraSource> sceneCameras, string sourceId)
    {
        var source = ResolveSource(sceneCameras, sourceId);
        return source is not null && source.Transform.Visible && source.Transform.IncludeInOutput
            ? source
            : null;
    }
}
