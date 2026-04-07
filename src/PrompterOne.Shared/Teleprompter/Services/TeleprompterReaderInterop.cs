using Microsoft.JSInterop;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Services;

public sealed class TeleprompterReaderInterop(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public ValueTask<double?> MeasureClusterOffsetAsync(
        string stageId,
        string textId,
        string targetWordId,
        int focalPointPercent,
        bool neutralizeCard = false) =>
        _jsRuntime.InvokeAsync<double?>(
            TeleprompterReaderInteropMethodNames.MeasureClusterOffset,
            stageId,
            textId,
            targetWordId,
            focalPointPercent,
            neutralizeCard,
            UiDataAttributes.Teleprompter.CardState);

    public ValueTask<bool> ToggleFullscreenAsync(string elementId) =>
        _jsRuntime.InvokeAsync<bool>(
            TeleprompterReaderInteropMethodNames.ToggleFullscreen,
            elementId);

    public ValueTask<bool> IsFullscreenActiveAsync(string elementId) =>
        _jsRuntime.InvokeAsync<bool>(
            TeleprompterReaderInteropMethodNames.IsFullscreenActive,
            elementId);
}
