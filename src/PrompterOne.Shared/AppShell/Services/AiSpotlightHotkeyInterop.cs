using Microsoft.JSInterop;

namespace PrompterOne.Shared.Services;

public sealed class AiSpotlightHotkeyInterop(
    IJSRuntime jsRuntime,
    AiSpotlightService spotlight) : IAsyncDisposable
{
    private const string ModulePath = "./_content/PrompterOne.Shared/app/ai-spotlight-hotkeys.js";

    private IJSObjectReference? _module;
    private DotNetObjectReference<AiSpotlightHotkeyCallback>? _callbackReference;

    public async Task InitializeAsync()
    {
        if (_module is not null)
        {
            return;
        }

        _module = await jsRuntime.InvokeAsync<IJSObjectReference?>("import", ModulePath);
        if (_module is null)
        {
            return;
        }

        _callbackReference = DotNetObjectReference.Create(new AiSpotlightHotkeyCallback(spotlight));
        await _module.InvokeVoidAsync("initialize", _callbackReference);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("dispose");
            await _module.DisposeAsync();
        }

        _callbackReference?.Dispose();
    }

    private sealed class AiSpotlightHotkeyCallback(AiSpotlightService spotlightService)
    {
        [JSInvokable]
        public void Toggle() => spotlightService.Toggle();

        [JSInvokable]
        public void Close() => spotlightService.Close();
    }
}
