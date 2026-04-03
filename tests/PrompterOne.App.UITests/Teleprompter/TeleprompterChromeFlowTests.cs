using System.Text.RegularExpressions;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class TeleprompterChromeFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private static readonly Regex FilledSegmentStyleRegex = new(
        BrowserTestConstants.TeleprompterFlow.ProgressFilledStylePattern,
        RegexOptions.Compiled);

    private static readonly Regex EmptySegmentStyleRegex = new(
        BrowserTestConstants.TeleprompterFlow.ProgressEmptyStylePattern,
        RegexOptions.Compiled);

    [Fact]
    public Task TeleprompterScreen_ExposesOrientationToggle_AndSwitchesReaderOrientation() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var orientationToggle = page.GetByTestId(UiTestIds.Teleprompter.OrientationToggle);
            var clusterWrap = page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap);

            await Expect(orientationToggle).ToBeVisibleAsync();
            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.ReaderOrientationAttribute,
                BrowserTestConstants.TeleprompterFlow.OrientationLandscapeValue);

            await orientationToggle.ClickAsync();

            await Expect(clusterWrap).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.ReaderOrientationAttribute,
                BrowserTestConstants.TeleprompterFlow.OrientationPortraitValue);
        });

    [Fact]
    public Task TeleprompterScreen_FullscreenToggle_UsesBrowserFullscreenMode() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var fullscreenToggle = page.GetByTestId(UiTestIds.Teleprompter.FullscreenToggle);

            await Expect(fullscreenToggle).ToBeVisibleAsync();
            Assert.False(await IsFullscreenActiveAsync(page));

            await fullscreenToggle.ClickAsync();
            await page.WaitForFunctionAsync(BrowserTestConstants.TeleprompterFlow.FullscreenStateScript);
            Assert.True(await IsFullscreenActiveAsync(page));

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.FullscreenScenarioName,
                BrowserTestConstants.TeleprompterFlow.FullscreenStep);

            await fullscreenToggle.ClickAsync();
            await page.WaitForFunctionAsync(BrowserTestConstants.TeleprompterFlow.FullscreenInactiveStateScript);
            Assert.False(await IsFullscreenActiveAsync(page));
        });

    [Fact]
    public Task TeleprompterScreen_RendersSegmentedProgress_ByBlock() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var progress = page.GetByTestId(UiTestIds.Teleprompter.Progress);
            var progressSegments = page.GetByTestId(UiTestIds.Teleprompter.ProgressSegments);
            var firstSegmentFill = page.GetByTestId(UiTestIds.Teleprompter.ProgressSegmentFill(0));
            var secondSegmentFill = page.GetByTestId(UiTestIds.Teleprompter.ProgressSegmentFill(1));

            await Expect(progress).ToBeVisibleAsync();
            await Expect(progressSegments).ToBeVisibleAsync();

            var totalBlockCount = await ReadTotalBlockCountAsync(page);
            var renderedSegmentCount = await progressSegments.EvaluateAsync<int>("element => element.children.length");

            Assert.Equal(totalBlockCount, renderedSegmentCount);

            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();

            await Expect(firstSegmentFill).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                FilledSegmentStyleRegex);
            await Expect(secondSegmentFill).ToHaveAttributeAsync(
                BrowserTestConstants.TeleprompterFlow.StyleAttribute,
                EmptySegmentStyleRegex);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.ProgressScenarioName,
                BrowserTestConstants.TeleprompterFlow.ProgressStep);
        });

    private static async Task<bool> IsFullscreenActiveAsync(Microsoft.Playwright.IPage page) =>
        await page.EvaluateAsync<bool>(BrowserTestConstants.TeleprompterFlow.FullscreenStateScript);

    private static async Task<int> ReadTotalBlockCountAsync(Microsoft.Playwright.IPage page)
    {
        var blockIndicatorText = await page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}").TextContentAsync() ?? string.Empty;
        var parts = blockIndicatorText.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 2 && int.TryParse(parts[1], out var totalBlockCount)
            ? totalBlockCount
            : throw new Xunit.Sdk.XunitException($"Unable to parse teleprompter block count from '{blockIndicatorText}'.");
    }
}
