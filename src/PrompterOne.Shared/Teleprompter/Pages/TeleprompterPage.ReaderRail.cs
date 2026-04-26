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
    private const string ReaderRailTooltipVisibleCssClass = "rd-rail-tooltip--visible";

    private string ReaderFocalSliderTitle => Text(UiTextKey.TooltipMoveFocalReadingGuide);

    private string ReaderFontSliderTitle => Text(UiTextKey.TooltipAdjustReaderTextSize);

    private string ReaderSpeedCueDisplayMultiplierTitle => Text(UiTextKey.TooltipReaderSpeedCueDisplayMultiplier);

    private string ReaderSpeedCueDisplayTitle => Text(UiTextKey.TooltipReaderSpeedCueDisplay);

    private string ReaderSpeedCueDisplayWpmTitle => Text(UiTextKey.TooltipReaderSpeedCueDisplayWpm);

    private string ReaderSpeedDialTitle => Text(UiTextKey.TooltipReaderSpeedDial);

    private string ReaderFullscreenTitle => Text(UiTextKey.TooltipToggleBrowserFullscreen);

    private string ReaderMirrorHorizontalTitle => Text(UiTextKey.TooltipMirrorReaderHorizontally);

    private string ReaderMirrorVerticalTitle => Text(UiTextKey.TooltipMirrorReaderVertically);

    private string ReaderOrientationTitle => Text(UiTextKey.TooltipRotateReaderOrientation);

    private string ReaderTextAlignCenterTitle => Text(UiTextKey.TooltipAlignTextCenter);

    private string ReaderTextAlignJustifyTitle => Text(UiTextKey.TooltipAlignTextJustify);

    private string ReaderTextAlignLeftTitle => Text(UiTextKey.TooltipAlignTextLeft);

    private string ReaderTextAlignRightTitle => Text(UiTextKey.TooltipAlignTextRight);

    private string ReaderWidthSliderTitle => Text(UiTextKey.TooltipAdjustReaderTextWidth);

    private string BuildRailTooltipCssClass(string key, bool placeOnRightSide)
    {
        var placementClass = placeOnRightSide
            ? ReaderRailTooltipRightCssClass
            : ReaderRailTooltipLeftCssClass;

        return BuildClassList(
            ReaderRailTooltipCssClass,
            placementClass,
            IsRailTooltipVisible(key) ? ReaderRailTooltipVisibleCssClass : null);
    }

    private static string BuildRailTooltipDomId(string key) => UiDomIds.Teleprompter.RailTooltip(key);

    private static string BuildRailTooltipTestId(string key) => UiTestIds.Teleprompter.RailTooltip(key);
}
