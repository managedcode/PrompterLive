using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    private string ReaderCameraTooltip => Text(UiTextKey.TooltipToggleCameraPreview);

    private string ReaderNextBlockTooltip => Text(UiTextKey.TooltipNextBlock);

    private string ReaderNextWordTooltip => Text(UiTextKey.TooltipNextWord);

    private string ReaderPlayToggleTooltip => _isReaderPlaying
        ? Text(UiTextKey.TooltipPausePlayback)
        : Text(UiTextKey.TooltipPlayPlayback);

    private string ReaderPreviousBlockTooltip => Text(UiTextKey.TooltipPreviousBlock);

    private string ReaderPreviousWordTooltip => Text(UiTextKey.TooltipPreviousWord);

    private string ReaderSpeedDownTooltip => Text(UiTextKey.TooltipDecreaseSpeed);

    private string ReaderSpeedUpTooltip => Text(UiTextKey.TooltipIncreaseSpeed);

    private string Text(UiTextKey key) => Localizer[key.ToString()];
}
