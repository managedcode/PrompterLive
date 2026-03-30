using Microsoft.JSInterop;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Media;

namespace PrompterLive.Shared.Services;

public sealed class BrowserMediaPermissionService(IJSRuntime jsRuntime) : IMediaPermissionService
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task<MediaPermissionsState> QueryAsync(CancellationToken cancellationToken = default)
    {
        return await _jsRuntime.InvokeAsync<MediaPermissionsState>(
            BrowserMediaInteropMethodNames.QueryPermissions,
            cancellationToken);
    }

    public async Task<MediaPermissionsState> RequestAsync(CancellationToken cancellationToken = default)
    {
        return await _jsRuntime.InvokeAsync<MediaPermissionsState>(
            BrowserMediaInteropMethodNames.RequestPermissions,
            cancellationToken);
    }
}
