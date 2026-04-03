using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private async Task ToggleReaderFullscreenAsync()
    {
        _isReaderFullscreenActive = await ReaderInterop.ToggleFullscreenAsync(UiDomIds.Design.TeleprompterScreen);
        await InvokeAsync(StateHasChanged);
    }

    private async Task<bool> ExitReaderFullscreenIfActiveAsync()
    {
        var isFullscreenActive = _isReaderFullscreenActive ||
            await ReaderInterop.IsFullscreenActiveAsync(UiDomIds.Design.TeleprompterScreen);

        if (!isFullscreenActive)
        {
            return false;
        }

        _isReaderFullscreenActive = await ReaderInterop.ToggleFullscreenAsync(UiDomIds.Design.TeleprompterScreen);
        await InvokeAsync(StateHasChanged);
        return true;
    }
}
