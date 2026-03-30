using PrompterLive.Core.Models.Media;
using PrompterLive.Core.Models.Workspace;

namespace PrompterLive.Shared.Services;

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

public sealed class GoLiveSessionService
{
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

        SetState(State with
        {
            ScriptId = scriptId ?? string.Empty,
            ScriptTitle = scriptTitle ?? string.Empty,
            ScriptSubtitle = scriptSubtitle ?? string.Empty,
            SelectedSourceId = selectedSource?.SourceId ?? string.Empty,
            SelectedSourceLabel = selectedSource?.Label ?? string.Empty,
            ActiveSourceId = activeSource?.SourceId ?? string.Empty,
            ActiveSourceLabel = activeSource?.Label ?? string.Empty,
            PrimaryMicrophoneLabel = primaryMicrophoneLabel ?? string.Empty,
            OutputResolution = streaming.OutputResolution,
            BitrateKbps = streaming.BitrateKbps
        });
    }

    public void SelectSource(IReadOnlyList<SceneCameraSource> sceneCameras, string sourceId)
    {
        var source = ResolveSource(sceneCameras, sourceId);
        if (source is null)
        {
            return;
        }

        SetState(State with
        {
            SelectedSourceId = source.SourceId,
            SelectedSourceLabel = source.Label
        });
    }

    public void SwitchToSelectedSource(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        var source = ResolveOperationalSource(sceneCameras, State.SelectedSourceId)
            ?? ResolveDefaultSource(sceneCameras);
        if (source is null)
        {
            return;
        }

        SetState(State with
        {
            ActiveSourceId = source.SourceId,
            ActiveSourceLabel = source.Label
        });
    }

    public void ToggleStream(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        if (State.IsStreamActive)
        {
            SetState(State with
            {
                IsStreamActive = false,
                StreamStartedAt = null
            });
            return;
        }

        SwitchToSelectedSource(sceneCameras);
        SetState(State with
        {
            IsStreamActive = true,
            StreamStartedAt = DateTimeOffset.UtcNow
        });
    }

    public void ToggleRecording(IReadOnlyList<SceneCameraSource> sceneCameras)
    {
        if (State.IsRecordingActive)
        {
            SetState(State with
            {
                IsRecordingActive = false,
                RecordingStartedAt = null
            });
            return;
        }

        SwitchToSelectedSource(sceneCameras);
        SetState(State with
        {
            IsRecordingActive = true,
            RecordingStartedAt = DateTimeOffset.UtcNow
        });
    }

    public void SetState(GoLiveSessionState nextState)
    {
        if (EqualityComparer<GoLiveSessionState>.Default.Equals(State, nextState))
        {
            return;
        }

        State = nextState;
        StateChanged?.Invoke();
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
