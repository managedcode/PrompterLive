using Microsoft.JSInterop;

namespace PrompterLive.Shared.Services;

public sealed class BrowserSettingsStore(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await _jsRuntime.InvokeAsync<T?>("PrompterLive.settings.load", cancellationToken, key);
    }

    public Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        return _jsRuntime.InvokeVoidAsync("PrompterLive.settings.save", cancellationToken, key, value).AsTask();
    }
}
