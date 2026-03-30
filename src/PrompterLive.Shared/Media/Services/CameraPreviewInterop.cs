using Microsoft.JSInterop;

namespace PrompterLive.Shared.Services;

public sealed class CameraPreviewInterop(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public Task AttachCameraAsync(string elementId, string deviceId, bool muted = true)
    {
        return _jsRuntime.InvokeVoidAsync(
            BrowserMediaInteropMethodNames.AttachCamera,
            elementId,
            deviceId,
            muted).AsTask();
    }

    public Task DetachCameraAsync(string elementId)
    {
        return _jsRuntime.InvokeVoidAsync(
            BrowserMediaInteropMethodNames.DetachCamera,
            elementId).AsTask();
    }
}
