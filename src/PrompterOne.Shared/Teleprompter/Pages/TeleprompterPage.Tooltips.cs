using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    private string ReaderCameraTooltip => Text(UiTextKey.TooltipToggleCameraPreview);

    private string ReaderFontDownTooltip => Text(UiTextKey.TooltipSmallerText);

    private string ReaderFontUpTooltip => Text(UiTextKey.TooltipLargerText);

    private string ReaderNextBlockTooltip => Text(UiTextKey.TooltipNextBlock);

    private string ReaderNextWordTooltip => Text(UiTextKey.TooltipNextWord);

    private string ReaderPlayToggleTooltip => _isReaderPlaying
        ? Text(UiTextKey.TooltipPausePlayback)
        : Text(UiTextKey.TooltipPlayPlayback);

    private string ReaderPreviousBlockTooltip => Text(UiTextKey.TooltipPreviousBlock);

    private string ReaderPreviousWordTooltip => Text(UiTextKey.TooltipPreviousWord);

    private string Text(UiTextKey key) => Localizer[key.ToString()];
}
