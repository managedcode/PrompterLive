using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[Collection(EditorAuthoringCollection.Name)]
public sealed class EditorToolbarTooltipFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private const string WrappedTooltipSelectionFragment = "[motivational]script[/motivational]";

    private readonly record struct ElementBounds(double Left, double Top, double Right, double Bottom);

    [Fact]
    public Task EditorScreen_ToolbarTooltip_AppearsOnlyAfterDelay() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.ToolbarTooltipScenario);

            await OpenEditorAsync(page);

            var emotionTrigger = page.GetByTestId(UiTestIds.Editor.EmotionTrigger);
            var tooltip = GetTooltipLocator(page, BrowserTestConstants.EditorFlow.EmotionTooltipText);

            await emotionTrigger.HoverAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.EditorFlow.TooltipEarlyCheckDelayMs);

            Assert.InRange(
                await ReadOpacityAsync(tooltip),
                0,
                BrowserTestConstants.EditorFlow.MaximumEarlyTooltipOpacity);

            await page.WaitForTimeoutAsync(
                BrowserTestConstants.EditorFlow.TooltipSettleDelayMs - BrowserTestConstants.EditorFlow.TooltipEarlyCheckDelayMs);
            await Expect(tooltip).ToBeVisibleAsync();
            await Expect(tooltip).ToHaveTextAsync(BrowserTestConstants.EditorFlow.EmotionTooltipText);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.ToolbarTooltipScenario,
                BrowserTestConstants.EditorFlow.ToolbarTooltipDelayStep);
        });

    [Fact]
    public Task EditorScreen_DropdownTooltip_StaysOutsideMenuAndDoesNotBlockAction() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.ToolbarTooltipScenario);

            await OpenEditorAsync(page);
            await EditorMonacoDriver.SetTextAsync(page, BrowserTestConstants.Editor.TypedScript);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TypedSelectionTarget);

            var emotionTrigger = page.GetByTestId(UiTestIds.Editor.EmotionTrigger);
            var emotionMenu = page.GetByTestId(UiTestIds.Editor.MenuEmotion);
            var motivationalEmotion = page.GetByTestId(UiTestIds.Editor.EmotionMotivational);
            var tooltip = GetTooltipLocator(page, BrowserTestConstants.EditorFlow.MotivationalEmotionTooltipText);

            await emotionTrigger.ClickAsync();
            await Expect(emotionMenu).ToBeVisibleAsync();
            await motivationalEmotion.HoverAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.EditorFlow.TooltipEarlyCheckDelayMs);

            Assert.InRange(
                await ReadOpacityAsync(tooltip),
                0,
                BrowserTestConstants.EditorFlow.MaximumEarlyTooltipOpacity);

            await page.WaitForTimeoutAsync(
                BrowserTestConstants.EditorFlow.TooltipSettleDelayMs - BrowserTestConstants.EditorFlow.TooltipEarlyCheckDelayMs);
            await Expect(tooltip).ToBeVisibleAsync();
            await Expect(tooltip).ToHaveTextAsync(BrowserTestConstants.EditorFlow.MotivationalEmotionTooltipText);

            var tooltipSurface = await ReadTooltipSurfaceAsync(tooltip);
            Assert.True(tooltipSurface.BorderAlpha >= BrowserTestConstants.EditorFlow.MinimumTooltipSurfaceBorderAlpha);
            Assert.True(tooltipSurface.BorderContrast > 0);
            Assert.True(tooltipSurface.HasShadow);

            var menuBounds = await ReadBoundsAsync(emotionMenu);
            var tooltipBounds = await ReadBoundsAsync(tooltip);
            var overlap = CalculateIntersectionArea(menuBounds, tooltipBounds);

            Assert.InRange(overlap, 0, BrowserTestConstants.EditorFlow.MaximumTooltipMenuOverlapPx);

            await motivationalEmotion.ClickAsync();
            await Expect(EditorMonacoDriver.SourceInput(page))
                .ToHaveValueAsync(new Regex(Regex.Escape(WrappedTooltipSelectionFragment)));

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.ToolbarTooltipScenario,
                BrowserTestConstants.EditorFlow.ToolbarTooltipDropdownStep);
        });

    [Fact]
    public Task EditorScreen_ToolbarTooltip_StaysInsideViewport() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.ToolbarTooltipScenario);

            await OpenEditorAsync(page);

            var emotionTrigger = page.GetByTestId(UiTestIds.Editor.EmotionTrigger);
            var tooltip = GetTooltipLocator(page, BrowserTestConstants.EditorFlow.EmotionTooltipText);

            await emotionTrigger.HoverAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.EditorFlow.TooltipSettleDelayMs);

            await Expect(tooltip).ToBeVisibleAsync();

            var tooltipBounds = await ReadBoundsAsync(tooltip);
            var viewportHeight = await ReadViewportHeightAsync(page);
            var overlap = CalculateIntersectionArea(await ReadBoundsAsync(emotionTrigger), tooltipBounds);

            Assert.True(tooltipBounds.Top >= 0, $"Expected tooltip top to stay within viewport, but it was {tooltipBounds.Top:0.##}.");
            Assert.True(
                tooltipBounds.Bottom <= viewportHeight + BrowserTestConstants.EditorFlow.ToolbarOverflowTolerancePx,
                $"Expected tooltip bottom to stay within viewport height {viewportHeight:0.##}, but it was {tooltipBounds.Bottom:0.##}.");
            Assert.InRange(overlap, 0, BrowserTestConstants.EditorFlow.MaximumTooltipMenuOverlapPx);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.ToolbarTooltipScenario,
                BrowserTestConstants.EditorFlow.ToolbarTooltipViewportStep);
        });

    private static double CalculateIntersectionArea(ElementBounds left, ElementBounds right)
    {
        var overlapWidth = Math.Max(0, Math.Min(left.Right, right.Right) - Math.Max(left.Left, right.Left));
        var overlapHeight = Math.Max(0, Math.Min(left.Bottom, right.Bottom) - Math.Max(left.Top, right.Top));
        return overlapWidth * overlapHeight;
    }

    private static ILocator GetTooltipLocator(IPage page, string tooltipText) =>
        page.GetByTestId(UiTestIds.Editor.ToolbarTooltip)
            .Filter(new() { HasTextString = tooltipText });

    private static async Task<double> ReadOpacityAsync(ILocator locator) =>
        await locator.EvaluateAsync<double>(
            """
            element => Number.parseFloat(getComputedStyle(element).opacity)
            """);

    private static async Task OpenEditorAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
        await Expect(page.GetByTestId(UiTestIds.Editor.Page))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
        await EditorMonacoDriver.WaitUntilReadyAsync(page);
    }

    private static async Task<ElementBounds> ReadBoundsAsync(ILocator locator) =>
        await locator.EvaluateAsync<ElementBounds>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                return {
                    left: rect.left,
                    top: rect.top,
                    right: rect.right,
                    bottom: rect.bottom
                };
            }
            """);

    private static async Task<double> ReadViewportHeightAsync(IPage page) =>
        await page.EvaluateAsync<double>("() => window.innerHeight");

    private static async Task<TooltipSurfaceMetrics> ReadTooltipSurfaceAsync(ILocator locator) =>
        await locator.EvaluateAsync<TooltipSurfaceMetrics>(
            """
            element => {
                const parseColor = value => (value?.match(/\d+(\.\d+)?/g) ?? []).map(Number);
                const distance = (left, right) =>
                    left.length < 3 || right.length < 3
                        ? 0
                        : Math.abs(left[0] - right[0]) + Math.abs(left[1] - right[1]) + Math.abs(left[2] - right[2]);
                const styles = window.getComputedStyle(element);
                const borderColor = parseColor(styles.borderTopColor);
                const backgroundColor = parseColor(styles.backgroundColor);

                return {
                    borderAlpha: borderColor.length >= 4 ? borderColor[3] : 1,
                    borderContrast: distance(borderColor, backgroundColor),
                    hasShadow: styles.boxShadow !== 'none'
                };
            }
            """);

    private readonly record struct TooltipSurfaceMetrics(
        double BorderAlpha,
        double BorderContrast,
        bool HasShadow);
}
