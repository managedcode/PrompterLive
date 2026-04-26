using PrompterOne.Core.Models.Streaming;

namespace PrompterOne.Shared.Services;

public sealed class GoLiveOutputRuntimeService(GoLiveOutputInterop interop)
{
    private readonly GoLiveOutputInterop _interop = interop;

    public event Action? StateChanged;

    public GoLiveOutputRuntimeState State { get; private set; } = GoLiveOutputRuntimeState.Default;

    public async Task RefreshStateAsync()
    {
        var snapshot = await _interop.GetSessionStateAsync(GoLiveOutputRuntimeContract.SessionId);
        SetState(GoLiveOutputRuntimeState.FromSnapshot(snapshot));
    }

    public async Task StartStreamAsync(GoLiveOutputRuntimeRequest request)
    {
        await SyncLiveOutputsAsync(request);
    }

    public async Task StartRecordingAsync(GoLiveOutputRuntimeRequest request)
    {
        if (!request.CanStartRecording)
        {
            return;
        }

        await _interop.StartLocalRecordingAsync(
            GoLiveOutputRuntimeContract.SessionId,
            request);
        await RefreshStateAsync();
    }

    public async Task RotateRecordingTakeAsync(GoLiveOutputRuntimeRequest request)
    {
        if (!State.RecordingActive || !request.CanStartRecording)
        {
            return;
        }

        await _interop.RotateLocalRecordingTakeAsync(
            GoLiveOutputRuntimeContract.SessionId,
            request);
        await RefreshStateAsync();
    }

    public async Task UpdateProgramSourceAsync(GoLiveOutputRuntimeRequest request)
    {
        if (!State.HasActiveOutputs)
        {
            return;
        }

        await _interop.UpdateSessionDevicesAsync(
            GoLiveOutputRuntimeContract.SessionId,
            request);
        if (State.HasLiveOutputs)
        {
            await SyncLiveOutputsAsync(request);
            return;
        }

        await RefreshStateAsync();
    }

    public async Task StopStreamAsync()
    {
        if (State.VdoNinjaActive)
        {
            await _interop.StopVdoNinjaAsync(GoLiveOutputRuntimeContract.SessionId);
        }

        if (State.LiveKitActive)
        {
            await _interop.StopLiveKitAsync(GoLiveOutputRuntimeContract.SessionId);
        }

        await RefreshStateAsync();
    }

    public async Task StopRecordingAsync()
    {
        if (!State.RecordingActive)
        {
            return;
        }

        await _interop.StopLocalRecordingAsync(GoLiveOutputRuntimeContract.SessionId);
        await RefreshStateAsync();
    }

    private async Task SyncLiveOutputsAsync(GoLiveOutputRuntimeRequest request)
    {
        var shouldStartVdoNinja = request.GetPublishableConnections(StreamingPlatformKind.VdoNinja).Count > 0;
        var shouldStartLiveKit = request.GetPublishableConnections(StreamingPlatformKind.LiveKit).Count > 0;

        if (shouldStartVdoNinja)
        {
            await _interop.StartVdoNinjaAsync(
                GoLiveOutputRuntimeContract.SessionId,
                request);
        }
        else if (State.VdoNinjaActive)
        {
            await _interop.StopVdoNinjaAsync(GoLiveOutputRuntimeContract.SessionId);
        }

        if (shouldStartLiveKit)
        {
            await _interop.StartLiveKitAsync(
                GoLiveOutputRuntimeContract.SessionId,
                request);
        }
        else if (State.LiveKitActive)
        {
            await _interop.StopLiveKitAsync(GoLiveOutputRuntimeContract.SessionId);
        }

        await RefreshStateAsync();
    }

    private void SetState(GoLiveOutputRuntimeState nextState)
    {
        if (EqualityComparer<GoLiveOutputRuntimeState>.Default.Equals(State, nextState))
        {
            return;
        }

        State = nextState;
        StateChanged?.Invoke();
    }
}
