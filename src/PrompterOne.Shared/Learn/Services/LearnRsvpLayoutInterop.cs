using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PrompterOne.Shared.Services;

public sealed class LearnRsvpLayoutInterop(IJSRuntime jsRuntime) : IDisposable, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private Task<IJSObjectReference?>? _moduleTask;

    public async ValueTask<bool> SyncLayoutAsync(
        ElementReference display,
        ElementReference row,
        ElementReference focusWord,
        ElementReference focusOrp)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return false;
        }

        return await module.InvokeAsync<bool>(
            LearnRsvpLayoutInteropMethodNames.SyncLayout,
            display,
            row,
            focusWord,
            focusOrp,
            LearnRsvpLayoutContract.FocusLeftExtentCssCustomProperty,
            LearnRsvpLayoutContract.FocusRightExtentCssCustomProperty,
            LearnRsvpLayoutContract.LayoutReadyAttributeName,
            LearnRsvpLayoutContract.FontSyncReadyAttributeName);
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
            await module.DisposeAsync();
        }
    }

    private Task<IJSObjectReference?> GetModuleAsync() =>
        _moduleTask ??= ImportModuleAsync();

    private async Task<IJSObjectReference?> ImportModuleAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<IJSObjectReference>(
                LearnRsvpLayoutInteropMethodNames.JSImportMethodName,
                LearnRsvpLayoutInteropMethodNames.ModulePath);
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
