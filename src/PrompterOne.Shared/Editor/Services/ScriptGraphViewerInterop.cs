using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PrompterOne.Core.AI.Models;

namespace PrompterOne.Shared.Services.Editor;

public sealed class ScriptGraphViewerInterop(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private const string ModulePath = "./_content/PrompterOne.Shared/app/script-graph-viewer.js";

    private IJSObjectReference? _module;

    public async Task RenderAsync(ElementReference element, ScriptKnowledgeGraphArtifact artifact)
    {
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference?>("import", ModulePath);
        if (_module is null)
        {
            return;
        }

        await _module.InvokeVoidAsync("render", element, artifact);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
