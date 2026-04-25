using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class TeleprompterControlRailTests : BunitContext
{
    private const string AlignCenterTooltipText = "Center text on the reading lane";
    private const string AlignJustifyTooltipText = "Stretch text across the full readable width";
    private const string AlignLeftTooltipText = "Align text to the left edge";
    private const string AlignRightTooltipText = "Align text to the right edge";
    private const string FocalSliderTooltipText = "Move the focal reading guide";
    private const string FontSliderTooltipText = "Adjust the reader text size";
    private const string FullscreenTooltipText = "Toggle browser fullscreen";
    private const string MirrorHorizontalTooltipText = "Mirror the reader horizontally";
    private const string MirrorVerticalTooltipText = "Mirror the reader vertically";
    private const string OrientationTooltipText = "Rotate the reader between landscape and portrait";
    private const string SpeedCueDisplayMultiplierTooltipText = "Show speed cues as multipliers";
    private const string SpeedCueDisplayTooltipText = "Choose how speed cue labels appear";
    private const string SpeedCueDisplayWpmTooltipText = "Show speed cues as WPM";
    private const string WidthSliderTooltipText = "Adjust the reader text width";

    [Test]
    public void TeleprompterPage_RendersFourIconBasedAlignmentButtons()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);

        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var leftButton = cut.FindByTestId(UiTestIds.Teleprompter.AlignmentLeft);
            var centerButton = cut.FindByTestId(UiTestIds.Teleprompter.AlignmentCenter);
            var rightButton = cut.FindByTestId(UiTestIds.Teleprompter.AlignmentRight);
            var justifyButton = cut.FindByTestId(UiTestIds.Teleprompter.AlignmentJustify);

            AssertIconButton(leftButton, AlignLeftTooltipText);
            AssertIconButton(centerButton, AlignCenterTooltipText);
            AssertIconButton(rightButton, AlignRightTooltipText);
            AssertIconButton(justifyButton, AlignJustifyTooltipText);
        });
    }

    [Test]
    public void TeleprompterPage_RendersRailTooltipsForControlsAndSliders()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);

        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipMirrorHorizontalKey, MirrorHorizontalTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipMirrorVerticalKey, MirrorVerticalTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipOrientationKey, OrientationTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipFullscreenKey, FullscreenTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipLeftKey, AlignLeftTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipCenterKey, AlignCenterTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipRightKey, AlignRightTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipJustifyKey, AlignJustifyTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipFontSizeKey, FontSliderTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipFocalKey, FocalSliderTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.AlignmentTooltipWidthKey, WidthSliderTooltipText);
            AssertTooltip(cut, UiTestIds.Teleprompter.SpeedCueDisplayTooltipKey, SpeedCueDisplayTooltipText);
            Assert.Equal(
                SpeedCueDisplayWpmTooltipText,
                cut.FindByTestId(UiTestIds.Teleprompter.SpeedCueDisplayWpm).GetAttribute("aria-label"));
            Assert.Equal(
                SpeedCueDisplayMultiplierTooltipText,
                cut.FindByTestId(UiTestIds.Teleprompter.SpeedCueDisplayMultiplier).GetAttribute("aria-label"));
        });
    }

    private static void AssertIconButton(AngleSharp.Dom.IElement button, string expectedAriaLabel)
    {
        Assert.Equal(expectedAriaLabel, button.GetAttribute("aria-label"));
        Assert.Equal(string.Empty, button.TextContent.Trim());
        Assert.Contains("<svg", button.InnerHtml, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertTooltip(IRenderedComponent<TeleprompterPage> cut, string tooltipKey, string expectedText)
    {
        var tooltip = cut.FindByTestId(UiTestIds.Teleprompter.RailTooltip(tooltipKey));

        Assert.Equal(UiDomIds.Teleprompter.RailTooltip(tooltipKey), tooltip.Id);
        Assert.Equal(expectedText, tooltip.TextContent.Trim());
        Assert.Equal("tooltip", tooltip.GetAttribute("role"));
    }
}
