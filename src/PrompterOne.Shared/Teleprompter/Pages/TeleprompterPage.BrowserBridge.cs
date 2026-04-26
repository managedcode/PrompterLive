using Microsoft.AspNetCore.Components.Web;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage : IAsyncDisposable
{
    private Task HandleTeleprompterKeyDownAsync(KeyboardEventArgs args) =>
        HandleTeleprompterKeyboardAsync(args);

    private async Task HandleTeleprompterKeyboardAsync(KeyboardEventArgs args)
    {
        if (!AppHotkeys.TryResolve(AppHotkeySurface.Teleprompter, args, out var action))
        {
            return;
        }

        switch (action)
        {
            case AppHotkeyAction.TeleprompterBack:
                if (!await ExitReaderFullscreenIfActiveAsync())
                {
                    await NavigateBackToEditorAsync();
                }
                break;
            case AppHotkeyAction.TeleprompterPlayPause:
                await ToggleReaderPlaybackAsync();
                break;
            case AppHotkeyAction.TeleprompterPreviousBlock:
                await JumpToPreviousReaderCardAsync();
                break;
            case AppHotkeyAction.TeleprompterNextBlock:
                await JumpToNextReaderCardAsync();
                break;
            case AppHotkeyAction.TeleprompterMirrorHorizontal:
                await ToggleReaderMirrorHorizontalAsync();
                break;
            case AppHotkeyAction.TeleprompterMirrorVertical:
                await ToggleReaderMirrorVerticalAsync();
                break;
            case AppHotkeyAction.TeleprompterOrientation:
                await ToggleReaderOrientationAsync();
                break;
            case AppHotkeyAction.TeleprompterFullscreen:
                await ToggleReaderFullscreenAsync();
                break;
            case AppHotkeyAction.TeleprompterAlignmentLeft:
                await SetReaderTextAlignmentAsync(ReaderTextAlignment.Left);
                break;
            case AppHotkeyAction.TeleprompterAlignmentCenter:
                await SetReaderTextAlignmentAsync(ReaderTextAlignment.Center);
                break;
            case AppHotkeyAction.TeleprompterAlignmentRight:
                await SetReaderTextAlignmentAsync(ReaderTextAlignment.Right);
                break;
            case AppHotkeyAction.TeleprompterAlignmentJustify:
                await SetReaderTextAlignmentAsync(ReaderTextAlignment.Justify);
                break;
            case AppHotkeyAction.TeleprompterCamera:
                await ToggleReaderCameraAsync();
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        StopReaderPlaybackLoop();
        await StopReaderRecordingAsync(updateUi: false);
        await DetachReaderCameraAsync();
    }
}
