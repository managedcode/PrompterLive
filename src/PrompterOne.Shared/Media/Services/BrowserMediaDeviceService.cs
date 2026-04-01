using System.Text.RegularExpressions;
using Microsoft.JSInterop;
using PrompterOne.Core.Abstractions;
using PrompterOne.Core.Models.Media;

namespace PrompterOne.Shared.Services;

public sealed partial class BrowserMediaDeviceService(IJSRuntime jsRuntime) : IMediaDeviceService
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public async Task<IReadOnlyList<MediaDeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var devices = await _jsRuntime.InvokeAsync<BrowserMediaDeviceDto[]>(
            BrowserMediaInteropMethodNames.ListDevices,
            cancellationToken);

        return devices.Select(device => new MediaDeviceInfo(
            device.DeviceId,
            SanitizeLabel(device.Label),
            device.Kind switch
            {
                "videoinput" => MediaDeviceKind.Camera,
                "audioinput" => MediaDeviceKind.Microphone,
                "audiooutput" => MediaDeviceKind.Speaker,
                _ => MediaDeviceKind.Unknown
            },
            device.IsDefault)).ToList();
    }

    private static string SanitizeLabel(string? rawLabel)
    {
        if (string.IsNullOrWhiteSpace(rawLabel))
        {
            return string.Empty;
        }

        var cleaned = VendorIdPattern().Replace(rawLabel, string.Empty).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? string.Empty : cleaned;
    }

    [GeneratedRegex(@"\s*\([0-9a-fA-F]{4}:[0-9a-fA-F]{4}\)")]
    private static partial Regex VendorIdPattern();

    private sealed record BrowserMediaDeviceDto(string DeviceId, string? Label, string Kind, bool IsDefault);
}
