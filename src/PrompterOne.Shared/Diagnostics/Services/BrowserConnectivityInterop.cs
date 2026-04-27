using Microsoft.JSInterop;

namespace PrompterOne.Shared.Services.Diagnostics;

public sealed class BrowserConnectivityInterop(IJSRuntime jsRuntime) : IDisposable, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private Task<IJSObjectReference?>? _moduleTask;

    public async ValueTask<bool?> GetOnlineStatusAsync(CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return null;
        }

        try
        {
            return await module.InvokeAsync<bool>(
                BrowserConnectivityInteropMethodNames.GetOnlineStatus,
                cancellationToken);
        }
        catch (JSException)
        {
            return null;
        }
    }

    public void Dispose()
    {
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask is null)
        {
            return;
        }

        var module = await _moduleTask;
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync(BrowserConnectivityInteropMethodNames.Dispose);
            }
            catch (JSException)
            {
            }

            await module.DisposeAsync();
        }
    }

    private async Task<IJSObjectReference?> GetModuleAsync()
    {
        if (_moduleTask is not null)
        {
            var existingModule = await _moduleTask;
            if (existingModule is not null)
            {
                return existingModule;
            }

            _moduleTask = null;
        }

        _moduleTask = ImportModuleAsync();
        var module = await _moduleTask;
        if (module is null)
        {
            _moduleTask = null;
        }

        return module;
    }

    private async Task<IJSObjectReference?> ImportModuleAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<IJSObjectReference>(
                BrowserConnectivityInteropMethodNames.JSImportMethodName,
                BrowserConnectivityInteropMethodNames.ModulePath);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (JSException)
        {
            return null;
        }
    }
}
