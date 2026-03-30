using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrompterLive.Core.Models.Editor;
using PrompterLive.Shared.Components.Editor;

namespace PrompterLive.Shared.Services.Editor;

public sealed class EditorInterop(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;

    public ValueTask SyncScrollAsync(ElementReference textarea, ElementReference overlay) =>
        _jsRuntime.InvokeVoidAsync(EditorSurfaceInteropMethodNames.SyncScroll, textarea, overlay);

    public async Task<EditorSelectionViewModel> GetSelectionAsync(ElementReference textarea)
    {
        var result = await _jsRuntime.InvokeAsync<EditorSelectionInteropResult?>(
            EditorSurfaceInteropMethodNames.GetSelectionState,
            textarea);

        return MapSelection(result);
    }

    public async Task<EditorSelectionViewModel> SetSelectionAsync(ElementReference textarea, int start, int end)
    {
        var result = await _jsRuntime.InvokeAsync<EditorSelectionInteropResult?>(
            EditorSurfaceInteropMethodNames.SetSelection,
            textarea,
            start,
            end);

        return MapSelection(result);
    }

    private static EditorSelectionViewModel MapSelection(EditorSelectionInteropResult? result) =>
        result is null
            ? EditorSelectionViewModel.Empty
            : new EditorSelectionViewModel(
                new EditorSelectionRange(result.Start, result.End),
                result.Line,
                result.Column,
                result.ToolbarTop,
                result.ToolbarLeft);

    private sealed record EditorSelectionInteropResult(
        int Start,
        int End,
        int Line,
        int Column,
        double ToolbarTop,
        double ToolbarLeft);
}
