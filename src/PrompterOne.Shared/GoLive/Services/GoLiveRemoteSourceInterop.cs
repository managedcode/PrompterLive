using Microsoft.JSInterop;

namespace PrompterOne.Shared.Services;

public sealed class GoLiveRemoteSourceInterop(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    internal Task<GoLiveRemoteSourceRuntimeSnapshot?> GetSessionStateAsync(string sessionId)
    {
        return _jsRuntime.InvokeAsync<GoLiveRemoteSourceRuntimeSnapshot?>(
            GoLiveRemoteSourceInteropMethodNames.GetSessionState,
            sessionId).AsTask();
    }

    public Task StopSessionAsync(string sessionId)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveRemoteSourceInteropMethodNames.StopSession,
            sessionId).AsTask();
    }

    public Task SyncConnectionsAsync(
        string sessionId,
        GoLiveOutputRuntimeRequest request)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveRemoteSourceInteropMethodNames.SyncConnections,
            sessionId,
            request).AsTask();
    }
}
