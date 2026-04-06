using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class TeleprompterPage
{
    private const string ReaderMirrorHorizontalLabel = "H";
    private const string ReaderMirrorVerticalLabel = "V";
    private const string ReaderRailTooltipCssClass = "rd-rail-tooltip";
    private const string ReaderRailTooltipLeftCssClass = "rd-rail-tooltip--left";
    private const string ReaderRailTooltipRightCssClass = "rd-rail-tooltip--right";

    private string ReaderFocalSliderTitle => Text(UiTextKey.TooltipMoveFocalReadingGuide);

    private string ReaderFontSliderTitle => Text(UiTextKey.TooltipAdjustReaderTextSize);

    private string ReaderFullscreenTitle => Text(UiTextKey.TooltipToggleBrowserFullscreen);

    private string ReaderMirrorHorizontalTitle => Text(UiTextKey.TooltipMirrorReaderHorizontally);

    private string ReaderMirrorVerticalTitle => Text(UiTextKey.TooltipMirrorReaderVertically);

    private string ReaderOrientationTitle => Text(UiTextKey.TooltipRotateReaderOrientation);

    private string ReaderTextAlignCenterTitle => Text(UiTextKey.TooltipAlignTextCenter);

    private string ReaderTextAlignJustifyTitle => Text(UiTextKey.TooltipAlignTextJustify);

    private string ReaderTextAlignLeftTitle => Text(UiTextKey.TooltipAlignTextLeft);

    private string ReaderTextAlignRightTitle => Text(UiTextKey.TooltipAlignTextRight);

    private string ReaderWidthSliderTitle => Text(UiTextKey.TooltipAdjustReaderTextWidth);

    private static string BuildRailTooltipCssClass(bool placeOnRightSide) =>
        placeOnRightSide
            ? $"{ReaderRailTooltipCssClass} {ReaderRailTooltipRightCssClass}"
            : $"{ReaderRailTooltipCssClass} {ReaderRailTooltipLeftCssClass}";

    private static string BuildRailTooltipDomId(string key) => UiDomIds.Teleprompter.RailTooltip(key);

    private static string BuildRailTooltipTestId(string key) => UiTestIds.Teleprompter.RailTooltip(key);
}
