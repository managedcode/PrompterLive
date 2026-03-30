using Microsoft.JSInterop;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Services;

namespace PrompterLive.Shared.Pages;

public partial class TeleprompterPage : IAsyncDisposable
{
    private const string HandleDesignKeyboardMethodName = nameof(HandleDesignKeyboardAsync);

    private static readonly string[] HandledKeyboardKeys =
    [
        UiKeyboardKeys.ArrowLeft,
        UiKeyboardKeys.ArrowRight,
        UiKeyboardKeys.CameraLower,
        UiKeyboardKeys.CameraUpper,
        UiKeyboardKeys.Escape,
        UiKeyboardKeys.PageDown,
        UiKeyboardKeys.PageUp,
        UiKeyboardKeys.Space
    ];

    private DotNetObjectReference<TeleprompterPage>? _browserBridge;
    private bool _isKeyboardBridgeAttached;

    private async Task EnsureReaderBridgeAttachedAsync()
    {
        _browserBridge ??= DotNetObjectReference.Create(this);

        if (!_isKeyboardBridgeAttached)
        {
            await JS.InvokeVoidAsync(
                AppJsInterop.AttachDesignKeyboardMethod,
                UiDomIds.Design.TeleprompterScreen,
                _browserBridge,
                HandleDesignKeyboardMethodName,
                HandledKeyboardKeys);
            _isKeyboardBridgeAttached = true;
        }
    }

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
                await ToggleReaderPlaybackAsync();
                break;
            case UiKeyboardKeys.ArrowLeft:
            case UiKeyboardKeys.PageUp:
                await JumpToPreviousReaderCardAsync();
                break;
            case UiKeyboardKeys.ArrowRight:
            case UiKeyboardKeys.PageDown:
                await JumpToNextReaderCardAsync();
                break;
            case UiKeyboardKeys.CameraLower:
            case UiKeyboardKeys.CameraUpper:
                await ToggleReaderCameraAsync();
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        StopReaderPlaybackLoop();
        await DetachReaderCameraAsync();

        if (_isKeyboardBridgeAttached)
        {
            await JS.InvokeVoidAsync(AppJsInterop.DetachDesignKeyboardMethod, UiDomIds.Design.TeleprompterScreen);
        }

        _browserBridge?.Dispose();
        _browserBridge = null;
    }
}
