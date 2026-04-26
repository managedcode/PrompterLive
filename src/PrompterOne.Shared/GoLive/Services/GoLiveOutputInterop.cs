using Microsoft.JSInterop;

namespace PrompterOne.Shared.Services;

public sealed class GoLiveOutputInterop(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    internal Task<GoLiveOutputRuntimeSnapshot?> GetSessionStateAsync(string sessionId)
    {
        return _jsRuntime.InvokeAsync<GoLiveOutputRuntimeSnapshot?>(
            GoLiveOutputInteropMethodNames.GetSessionState,
            sessionId).AsTask();
    }

    public Task StartLocalRecordingAsync(
        string sessionId,
        GoLiveOutputRuntimeRequest request)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveOutputInteropMethodNames.StartLocalRecording,
            sessionId,
            request).AsTask();
    }

    public Task RotateLocalRecordingTakeAsync(
        string sessionId,
        GoLiveOutputRuntimeRequest request)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveOutputInteropMethodNames.RotateLocalRecordingTake,
            sessionId,
            request).AsTask();
    }

    public Task StartLiveKitAsync(
        string sessionId,
        GoLiveOutputRuntimeRequest request)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveOutputInteropMethodNames.StartLiveKitSession,
            sessionId,
            request).AsTask();
    }

    public Task StopLiveKitAsync(string sessionId)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveOutputInteropMethodNames.StopLiveKitSession,
            sessionId).AsTask();
    }

    public Task StartVdoNinjaAsync(
        string sessionId,
        GoLiveOutputRuntimeRequest request)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveOutputInteropMethodNames.StartVdoNinjaSession,
            sessionId,
            request).AsTask();
    }

    public Task StopVdoNinjaAsync(string sessionId)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveOutputInteropMethodNames.StopVdoNinjaSession,
            sessionId).AsTask();
    }

    public Task StopLocalRecordingAsync(string sessionId)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveOutputInteropMethodNames.StopLocalRecording,
            sessionId).AsTask();
    }

    public Task UpdateSessionDevicesAsync(
        string sessionId,
        GoLiveOutputRuntimeRequest request)
    {
        return _jsRuntime.InvokeVoidAsync(
            GoLiveOutputInteropMethodNames.UpdateSessionDevices,
            sessionId,
            request).AsTask();
    }
}
