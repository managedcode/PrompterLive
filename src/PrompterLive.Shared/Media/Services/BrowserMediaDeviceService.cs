using Microsoft.JSInterop;
using PrompterLive.Core.Abstractions;
using PrompterLive.Core.Models.Media;

namespace PrompterLive.Shared.Services;

public sealed class BrowserMediaDeviceService(IJSRuntime jsRuntime) : IMediaDeviceService
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task<IReadOnlyList<MediaDeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var devices = await _jsRuntime.InvokeAsync<BrowserMediaDeviceDto[]>(
            BrowserMediaInteropMethodNames.ListDevices,
            cancellationToken);

        return devices.Select(device => new MediaDeviceInfo(
            device.DeviceId,
            string.IsNullOrWhiteSpace(device.Label) ? "Unnamed device" : device.Label,
            device.Kind switch
            {
                "videoinput" => MediaDeviceKind.Camera,
                "audioinput" => MediaDeviceKind.Microphone,
                "audiooutput" => MediaDeviceKind.Speaker,
                _ => MediaDeviceKind.Unknown
            },
            device.IsDefault)).ToList();
    }

    private sealed record BrowserMediaDeviceDto(string DeviceId, string Label, string Kind, bool IsDefault);
}
