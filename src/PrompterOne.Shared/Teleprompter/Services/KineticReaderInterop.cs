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

    public ValueTask CommitFrameAsync() =>
        _jsRuntime.InvokeVoidAsync(KineticReaderInteropMethodNames.CommitFrame);

    public ValueTask HideLensAsync(string lensId) =>
        _jsRuntime.InvokeVoidAsync(KineticReaderInteropMethodNames.HideLens, lensId);

    public ValueTask PositionLensAsync(
        string lensId,
        string targetId,
        IReadOnlyList<string> cueTags,
        int targetDurationMs) =>
        _jsRuntime.InvokeVoidAsync(
            KineticReaderInteropMethodNames.PositionLens,
            lensId,
            targetId,
            cueTags,
            targetDurationMs);

}
