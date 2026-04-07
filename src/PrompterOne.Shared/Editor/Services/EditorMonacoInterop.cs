using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Shared.Components.Editor;

namespace PrompterOne.Shared.Services.Editor;

public sealed class EditorMonacoInterop(IJSRuntime jsRuntime) : IDisposable, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private Task<IJSObjectReference?>? _moduleTask;

    public async ValueTask<bool> InitializeAsync(
        ElementReference host,
        ElementReference proxy,
        ElementReference semanticSnapshot,
        DotNetObjectReference<EditorMonacoCallbackBridge> callbackBridge,
        object options)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return false;
        }

        return await module.InvokeAsync<bool>(
            EditorMonacoInteropMethodNames.InitializeEditor,
            host,
            proxy,
            semanticSnapshot,
            callbackBridge,
            options);
    }

    public async ValueTask SyncEditorStateAsync(
        ElementReference host,
        string text,
        EditorSelectionViewModel selection)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync(
            EditorMonacoInteropMethodNames.SyncEditorState,
            host,
            text ?? string.Empty,
            selection.Range.Start,
            selection.Range.End);
    }

    public async Task<EditorSelectionViewModel> GetSelectionAsync(ElementReference host)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return EditorSelectionViewModel.Empty;
        }

        var result = await module.InvokeAsync<EditorSelectionInteropResult?>(
            EditorMonacoInteropMethodNames.GetSelectionState,
            host);

        return MapSelection(result);
    }

    public async Task<EditorSelectionViewModel> SetSelectionAsync(
        ElementReference host,
        int start,
        int end,
        bool revealSelection = true,
        bool focusEditor = true)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return EditorSelectionViewModel.Empty;
        }

        var result = await module.InvokeAsync<EditorSelectionInteropResult?>(
            EditorMonacoInteropMethodNames.SetSelection,
            host,
            start,
            end,
            revealSelection,
            focusEditor);

        return MapSelection(result);
    }

    public async ValueTask DisposeEditorAsync(ElementReference host)
    {
        var module = await GetModuleAsync();
        if (module is null)
        {
            return;
        }

        await module.InvokeVoidAsync(EditorMonacoInteropMethodNames.DisposeEditor, host);
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

    public void Dispose()
    {
    }

    private Task<IJSObjectReference?> GetModuleAsync() =>
        _moduleTask ??= ImportModuleAsync();

    private async Task<IJSObjectReference?> ImportModuleAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<IJSObjectReference>(
                EditorMonacoInteropMethodNames.JSImportMethodName,
                EditorMonacoInteropMethodNames.ModulePath);
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
