using Microsoft.JSInterop;

namespace PrompterLive.Shared.Services;

public sealed class MicrophoneLevelInterop(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public Task StartAsync(
        string elementId,
        string deviceId,
        DotNetObjectReference<MicrophoneLevelObserver> observer)
    {
        return _jsRuntime.InvokeVoidAsync(
            BrowserMediaInteropMethodNames.StartMicrophoneLevelMonitor,
            elementId,
            deviceId,
            observer).AsTask();
    }

    public Task StopAsync(string elementId)
    {
        return _jsRuntime.InvokeVoidAsync(
            BrowserMediaInteropMethodNames.StopMicrophoneLevelMonitor,
            elementId).AsTask();
    }
}

public sealed class MicrophoneLevelObserver(Func<int, Task> onLevelChanged)
{
    private readonly Func<int, Task> _onLevelChanged = onLevelChanged;

    [JSInvokable]
    public Task UpdateLevel(int levelPercent) => _onLevelChanged(levelPercent);
}
