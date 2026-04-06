using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

public sealed class TeleprompterReadingChromeFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private static readonly Regex ReadingActiveClassRegex = new(
        $@"\b{BrowserTestConstants.TeleprompterFlow.ReadingActiveCssClass}\b",
        RegexOptions.Compiled);

    [Fact]
    public Task TeleprompterScreen_ActivePlayback_MutesChromeIntensity() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var controls = page.GetByTestId(UiTestIds.Teleprompter.Controls);
            var progressShell = page.GetByTestId(UiTestIds.Teleprompter.Progress);
            var edgeInfo = page.GetByTestId(UiTestIds.Teleprompter.EdgeInfo);
            var headerBack = page.GetByTestId(UiTestIds.Header.Back);
            var headerGoLive = page.GetByTestId(UiTestIds.Header.GoLive);
            var readerBack = page.GetByTestId(UiTestIds.Teleprompter.Back);

            var initialControls = await ReadChromeVisualStateAsync(controls);
            var initialProgress = await ReadChromeVisualStateAsync(progressShell);
            var initialEdgeInfo = await ReadChromeVisualStateAsync(edgeInfo);
            var initialHeaderBack = await ReadChromeVisualStateAsync(headerBack);
            var initialHeaderGoLive = await ReadChromeVisualStateAsync(headerGoLive);
            var initialReaderBack = await ReadChromeVisualStateAsync(readerBack);

            await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
            await page.GetByTestId(UiTestIds.Teleprompter.PlayToggle).ClickAsync();

            await Expect(controls).ToHaveClassAsync(ReadingActiveClassRegex);
            await Expect(progressShell).ToHaveClassAsync(ReadingActiveClassRegex);
            await Expect(edgeInfo).ToHaveClassAsync(ReadingActiveClassRegex);
            await page.WaitForTimeoutAsync(BrowserTestConstants.TeleprompterFlow.ReadingChromeSettleDelayMs);

            var activeControls = await ReadChromeVisualStateAsync(controls);
            var activeProgress = await ReadChromeVisualStateAsync(progressShell);
            var activeEdgeInfo = await ReadChromeVisualStateAsync(edgeInfo);
            var activeHeaderBack = await ReadChromeVisualStateAsync(headerBack);
            var activeHeaderGoLive = await ReadChromeVisualStateAsync(headerGoLive);
            var activeReaderBack = await ReadChromeVisualStateAsync(readerBack);

            Assert.True(
                initialControls.BackgroundAlpha - activeControls.BackgroundAlpha >= BrowserTestConstants.TeleprompterFlow.MinimumChromeBackgroundAlphaReduction,
                $"Expected controls background alpha {activeControls.BackgroundAlpha:0.###} to be at least {BrowserTestConstants.TeleprompterFlow.MinimumChromeBackgroundAlphaReduction:0.###} lower than {initialControls.BackgroundAlpha:0.###} during active playback.");
            Assert.True(
                initialProgress.BackgroundAlpha - activeProgress.BackgroundAlpha >= BrowserTestConstants.TeleprompterFlow.MinimumChromeBackgroundAlphaReduction,
                $"Expected progress shell background alpha {activeProgress.BackgroundAlpha:0.###} to be at least {BrowserTestConstants.TeleprompterFlow.MinimumChromeBackgroundAlphaReduction:0.###} lower than {initialProgress.BackgroundAlpha:0.###} during active playback.");
            Assert.True(
                initialEdgeInfo.Opacity - activeEdgeInfo.Opacity >= BrowserTestConstants.TeleprompterFlow.MinimumEdgeInfoOpacityReduction,
                $"Expected edge info opacity {activeEdgeInfo.Opacity:0.###} to be at least {BrowserTestConstants.TeleprompterFlow.MinimumEdgeInfoOpacityReduction:0.###} lower than {initialEdgeInfo.Opacity:0.###} during active playback.");
            Assert.True(
                initialHeaderBack.BackgroundAlpha - activeHeaderBack.BackgroundAlpha >= BrowserTestConstants.TeleprompterFlow.MinimumShellButtonBackgroundAlphaReduction,
                $"Expected header back background alpha {activeHeaderBack.BackgroundAlpha:0.###} to be at least {BrowserTestConstants.TeleprompterFlow.MinimumShellButtonBackgroundAlphaReduction:0.###} lower than {initialHeaderBack.BackgroundAlpha:0.###} during active playback.");
            Assert.True(
                initialHeaderGoLive.BackgroundAlpha - activeHeaderGoLive.BackgroundAlpha >= BrowserTestConstants.TeleprompterFlow.MinimumShellButtonBackgroundAlphaReduction,
                $"Expected header Go Live background alpha {activeHeaderGoLive.BackgroundAlpha:0.###} to be at least {BrowserTestConstants.TeleprompterFlow.MinimumShellButtonBackgroundAlphaReduction:0.###} lower than {initialHeaderGoLive.BackgroundAlpha:0.###} during active playback.");
            Assert.True(
                initialReaderBack.BackgroundAlpha - activeReaderBack.BackgroundAlpha >= BrowserTestConstants.TeleprompterFlow.MinimumChromeBackgroundAlphaReduction,
                $"Expected reader back background alpha {activeReaderBack.BackgroundAlpha:0.###} to be at least {BrowserTestConstants.TeleprompterFlow.MinimumChromeBackgroundAlphaReduction:0.###} lower than {initialReaderBack.BackgroundAlpha:0.###} during active playback.");

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.ReadingChromeScenarioName,
                BrowserTestConstants.TeleprompterFlow.ReadingChromeStep);
        });

    private static async Task<ChromeVisualState> ReadChromeVisualStateAsync(ILocator locator) =>
        await locator.EvaluateAsync<ChromeVisualState>(
            """
            element => {
                const styles = window.getComputedStyle(element);
                const parseAlpha = value => {
                    const match = value.match(/rgba?\(([^)]+)\)/);
                    if (!match) {
                        return 1;
                    }

                    const parts = match[1].split(',').map(part => part.trim());
                    return parts.length >= 4 ? Number.parseFloat(parts[3]) : 1;
                };

                return {
                    backgroundAlpha: parseAlpha(styles.backgroundColor),
                    opacity: Number.parseFloat(styles.opacity || "1")
                };
            }
            """);

    private readonly record struct ChromeVisualState(double BackgroundAlpha, double Opacity);
}
