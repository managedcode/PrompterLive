using Microsoft.JSInterop;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Services;

namespace PrompterLive.Shared.Pages;

public partial class LearnPage : IAsyncDisposable
{
    [JSInvokable(HandleDesignKeyboardMethodName)]
    public async Task HandleDesignKeyboardAsync(string key, bool isEditableTarget)
    {
        if (isEditableTarget)
        {
            return;
        }

        switch (key)
        {
            case UiKeyboardKeys.Escape:
                await NavigateBackToEditorAsync();
                break;
            case UiKeyboardKeys.Space:
                await ToggleRsvpPlaybackAsync();
                break;
            case UiKeyboardKeys.ArrowLeft:
                await StepRsvpBackwardAsync();
                break;
            case UiKeyboardKeys.ArrowRight:
                await StepRsvpForwardAsync();
                break;
            case UiKeyboardKeys.PageUp:
                await StepRsvpBackwardLargeAsync();
                break;
            case UiKeyboardKeys.PageDown:
                await StepRsvpForwardLargeAsync();
                break;
            case UiKeyboardKeys.ArrowUp:
                await IncreaseRsvpSpeedAsync();
                break;
            case UiKeyboardKeys.ArrowDown:
                await DecreaseRsvpSpeedAsync();
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        StopPlaybackLoop();

        if (_isKeyboardAttached)
        {
            await JS.InvokeVoidAsync(AppJsInterop.DetachDesignKeyboardMethod, UiDomIds.Design.LearnScreen);
        }

        _keyboardBridge?.Dispose();
        _keyboardBridge = null;
    }
}
