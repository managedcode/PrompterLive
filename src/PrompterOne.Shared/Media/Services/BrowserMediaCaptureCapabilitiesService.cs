using Microsoft.JSInterop;

namespace PrompterOne.Shared.Services;

public sealed class BrowserMediaCaptureCapabilitiesService(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task<BrowserMediaCaptureCapabilities> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _jsRuntime.InvokeAsync<BrowserMediaCaptureCapabilities>(
                BrowserMediaInteropMethodNames.GetCaptureCapabilities,
                cancellationToken)
            ?? BrowserMediaCaptureCapabilities.Default;
    }
}
