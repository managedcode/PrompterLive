using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterChromeFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture){
    private static readonly Regex FilledSegmentStyleRegex = new(
        BrowserTestConstants.TeleprompterFlow.ProgressFilledStylePattern,
        RegexOptions.Compiled);

    private static readonly Regex EmptySegmentStyleRegex = new(
        BrowserTestConstants.TeleprompterFlow.ProgressEmptyStylePattern,
        RegexOptions.Compiled);

    [Test]
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

    [Test]
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
            await Assert.That(await IsFullscreenActiveAsync(page)).IsFalse();

            await fullscreenToggle.ClickAsync();
            await page.WaitForFunctionAsync(BrowserTestConstants.TeleprompterFlow.FullscreenStateScript);
            await Assert.That(await IsFullscreenActiveAsync(page)).IsTrue();

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.FullscreenScenarioName,
                BrowserTestConstants.TeleprompterFlow.FullscreenStep);

            await fullscreenToggle.ClickAsync();
            await page.WaitForFunctionAsync(BrowserTestConstants.TeleprompterFlow.FullscreenInactiveStateScript);
            await Assert.That(await IsFullscreenActiveAsync(page)).IsFalse();
        });

    [Test]
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

            await Assert.That(renderedSegmentCount).IsEqualTo(totalBlockCount);

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

    [Test]
    public Task TeleprompterScreen_DesktopProgressShell_KeepsSegmentsInsideItsBorder() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var progressShell = page.GetByTestId(UiTestIds.Teleprompter.Progress);
            var progressTrack = page.GetByTestId(UiTestIds.Teleprompter.ProgressSegments);

            await Expect(progressShell).ToBeVisibleAsync();
            await Expect(progressTrack).ToBeVisibleAsync();

            var shellBounds = await GetRequiredBoundingBoxAsync(progressShell);
            var segmentBounds = await MeasureProgressSegmentBoundsAsync(progressTrack);
            var viewportBounds = await ReadViewportBoundsAsync(page);

            await Assert.That(shellBounds.X >= -BrowserTestConstants.TeleprompterFlow.MaxProgressShellOverflowPx).IsTrue().Because($"Expected teleprompter progress shell left edge {shellBounds.X:0.##} to stay inside viewport.");
            await Assert.That(shellBounds.X + shellBounds.Width <= viewportBounds.Width + BrowserTestConstants.TeleprompterFlow.MaxProgressShellOverflowPx).IsTrue().Because($"Expected teleprompter progress shell right edge {shellBounds.X + shellBounds.Width:0.##} to stay within viewport width {viewportBounds.Width:0.##}.");
            await Assert.That(segmentBounds.Left >= shellBounds.X - BrowserTestConstants.TeleprompterFlow.MaxProgressShellOverflowPx).IsTrue().Because($"Expected teleprompter progress segments left edge {segmentBounds.Left:0.##} to stay inside shell left edge {shellBounds.X:0.##}.");
            await Assert.That(segmentBounds.Right <= shellBounds.X + shellBounds.Width + BrowserTestConstants.TeleprompterFlow.MaxProgressShellOverflowPx).IsTrue().Because($"Expected teleprompter progress segments right edge {segmentBounds.Right:0.##} to stay inside shell right edge {shellBounds.X + shellBounds.Width:0.##}.");

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.ProgressFitScenarioName,
                BrowserTestConstants.TeleprompterFlow.ProgressFitStep);
        });

    private static async Task<bool> IsFullscreenActiveAsync(Microsoft.Playwright.IPage page) =>
        await page.EvaluateAsync<bool>(BrowserTestConstants.TeleprompterFlow.FullscreenStateScript);

    private static async Task<LayoutBounds> GetRequiredBoundingBoxAsync(ILocator locator) =>
        await locator.EvaluateAsync<LayoutBounds>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                return {
                    x: rect.x,
                    y: rect.y,
                    width: rect.width,
                    height: rect.height
                };
            }
            """);

    private static async Task<SegmentBounds> MeasureProgressSegmentBoundsAsync(ILocator locator) =>
        await locator.EvaluateAsync<SegmentBounds>(
            """
            element => {
                const childRects = Array.from(element.children, child => child.getBoundingClientRect());
                const left = childRects.reduce((minimum, rect) => Math.min(minimum, rect.left), Number.POSITIVE_INFINITY);
                const right = childRects.reduce((maximum, rect) => Math.max(maximum, rect.right), Number.NEGATIVE_INFINITY);
                return {
                    left: Number.isFinite(left) ? left : 0,
                    right: Number.isFinite(right) ? right : 0
                };
            }
            """);

    private static async Task<ViewportBounds> ReadViewportBoundsAsync(IPage page) =>
        await page.EvaluateAsync<ViewportBounds>(
            """
            () => ({
                width: window.innerWidth,
                height: window.innerHeight
            })
            """);

    private static async Task<int> ReadTotalBlockCountAsync(Microsoft.Playwright.IPage page)
    {
        var blockIndicatorText = await page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}").TextContentAsync() ?? string.Empty;
        var parts = blockIndicatorText.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 2 && int.TryParse(parts[1], out var totalBlockCount)
            ? totalBlockCount
            : throw new InvalidOperationException($"Unable to parse teleprompter block count from '{blockIndicatorText}'.");
    }

    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);
    private readonly record struct SegmentBounds(double Left, double Right);
    private readonly record struct ViewportBounds(double Width, double Height);
}