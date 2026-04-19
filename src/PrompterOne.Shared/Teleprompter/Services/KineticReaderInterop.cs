using Microsoft.JSInterop;

namespace PrompterOne.Shared.Services;

public sealed class KineticReaderInterop(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public ValueTask ActivateWordAsync(
        int durationMs,
        IReadOnlyList<string> cueTags,
        double playbackRate) =>
        _jsRuntime.InvokeVoidAsync(
            KineticReaderInteropMethodNames.ActivateWord,
            durationMs,
            cueTags,
            playbackRate);

    public ValueTask ClearAsync() =>
        _jsRuntime.InvokeVoidAsync(KineticReaderInteropMethodNames.ClearAll);

    public ValueTask SetPlaybackRateAsync(double playbackRate) =>
        _jsRuntime.InvokeVoidAsync(
            KineticReaderInteropMethodNames.SetPlaybackRate,
            playbackRate);
}
