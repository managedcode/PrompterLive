namespace PrompterOne.Shared.Services;

public sealed class GoLiveRemoteSourceRuntimeService(GoLiveRemoteSourceInterop interop)
{
    private readonly GoLiveRemoteSourceInterop _interop = interop;

    public event Action? StateChanged;

    public GoLiveRemoteSourceRuntimeState State { get; private set; } = GoLiveRemoteSourceRuntimeState.Default;

    public async Task RefreshStateAsync()
    {
        var snapshot = await _interop.GetSessionStateAsync(GoLiveOutputRuntimeContract.SessionId);
        SetState(GoLiveRemoteSourceRuntimeState.FromSnapshot(snapshot));
    }

    public async Task StopAsync()
    {
        await _interop.StopSessionAsync(GoLiveOutputRuntimeContract.SessionId);
        SetState(GoLiveRemoteSourceRuntimeState.Default);
    }

    public async Task SyncConnectionsAsync(GoLiveOutputRuntimeRequest request)
    {
        await _interop.SyncConnectionsAsync(
            GoLiveOutputRuntimeContract.SessionId,
            request);
        await RefreshStateAsync();
    }

    private void SetState(GoLiveRemoteSourceRuntimeState nextState)
    {
        if (EqualityComparer<GoLiveRemoteSourceRuntimeState>.Default.Equals(State, nextState))
        {
            return;
        }

        State = nextState;
        StateChanged?.Invoke();
    }
}
