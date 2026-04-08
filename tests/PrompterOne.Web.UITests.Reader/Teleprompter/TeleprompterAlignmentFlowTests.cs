using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterAlignmentFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const int OpeningCardIndex = 0;

    [Test]
    public Task TeleprompterScreen_ExposesFourAlignmentModes() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var clusterWrap = page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap);
            var cardText = page.GetByTestId(UiTestIds.Teleprompter.CardText(OpeningCardIndex));

            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.ReaderTextAlignmentAttribute,
                BrowserTestConstants.TeleprompterFlow.AlignmentLeftValue);
            await AssertAlignmentProbeAsync(
                cardText,
                BrowserTestConstants.TeleprompterFlow.AlignmentLeftValue,
                requireLeadingInset: true,
                requireTrailingInset: false);

            await page.GetByTestId(UiTestIds.Teleprompter.AlignmentCenter).ClickAsync();
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.ReaderTextAlignmentAttribute,
                BrowserTestConstants.TeleprompterFlow.AlignmentCenterValue);
            await AssertAlignmentProbeAsync(
                cardText,
                BrowserTestConstants.TeleprompterFlow.AlignmentCenterValue,
                requireLeadingInset: false,
                requireTrailingInset: false);

            await page.GetByTestId(UiTestIds.Teleprompter.AlignmentRight).ClickAsync();
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.ReaderTextAlignmentAttribute,
                BrowserTestConstants.TeleprompterFlow.AlignmentRightValue);
            await AssertAlignmentProbeAsync(
                cardText,
                BrowserTestConstants.TeleprompterFlow.AlignmentRightValue,
                requireLeadingInset: false,
                requireTrailingInset: true);

            await page.GetByTestId(UiTestIds.Teleprompter.AlignmentJustify).ClickAsync();
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.ReaderTextAlignmentAttribute,
                BrowserTestConstants.TeleprompterFlow.AlignmentJustifyValue);
            await AssertAlignmentProbeAsync(
                cardText,
                BrowserTestConstants.TeleprompterFlow.AlignmentJustifyValue,
                requireLeadingInset: false,
                requireTrailingInset: false);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.CenterAlignmentScenarioName,
                BrowserTestConstants.TeleprompterFlow.CenterAlignmentStep);
        });

    private static async Task AssertAlignmentProbeAsync(
        Microsoft.Playwright.ILocator locator,
        string expectedTextAlign,
        bool requireLeadingInset,
        bool requireTrailingInset)
    {
        var probe = await locator.EvaluateAsync<TextAlignmentProbe>(
            """
            element => {
                const style = window.getComputedStyle(element);
                return {
                    textAlign: style.textAlign,
                    paddingInlineStartPx: parseFloat(style.paddingInlineStart || "0"),
                    paddingInlineEndPx: parseFloat(style.paddingInlineEnd || "0")
                };
            }
            """);

        await Assert.That(probe.TextAlign).IsEqualTo(expectedTextAlign);

        if (requireLeadingInset)
        {
            await Assert.That(probe.PaddingInlineStartPx).IsBetween(BrowserTestConstants.TeleprompterFlow.MinimumOpticalInsetPx, double.MaxValue);
        }
        else
        {
            await Assert.That(probe.PaddingInlineStartPx).IsBetween(0, BrowserTestConstants.TeleprompterFlow.MinimumOpticalInsetPx);
        }

        if (requireTrailingInset)
        {
            await Assert.That(probe.PaddingInlineEndPx).IsBetween(BrowserTestConstants.TeleprompterFlow.MinimumOpticalInsetPx, double.MaxValue);
        }
        else
        {
            await Assert.That(probe.PaddingInlineEndPx).IsBetween(0, BrowserTestConstants.TeleprompterFlow.MinimumOpticalInsetPx);
        }
    }

    private sealed class TextAlignmentProbe
    {
        public string TextAlign { get; init; } = string.Empty;

        public double PaddingInlineStartPx { get; init; }

        public double PaddingInlineEndPx { get; init; }
    }
}
