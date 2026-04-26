using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class LearnPage
{
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    private string BackFiveWordsTooltip => Text(UiTextKey.TooltipBackFiveWords);

    private string BackOneWordTooltip => Text(UiTextKey.TooltipBackOneWord);

    private string DecreaseSpeedTooltip => Text(UiTextKey.TooltipDecreaseSpeed);

    private string ForwardFiveWordsTooltip => Text(UiTextKey.TooltipForwardFiveWords);

    private string ForwardOneWordTooltip => Text(UiTextKey.TooltipForwardOneWord);

    private string IncreaseSpeedTooltip => Text(UiTextKey.TooltipIncreaseSpeed);

    private string PlayToggleTooltip => _isPlaying
        ? Text(UiTextKey.TooltipPausePlayback)
        : Text(UiTextKey.TooltipPlayPlayback);

    private string RestartPhraseTooltip => Text(UiTextKey.TooltipRestartPhrase);

    private string Text(UiTextKey key) => Localizer[key.ToString()];

    private string Format(UiTextKey key, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Text(key), arguments);
}
