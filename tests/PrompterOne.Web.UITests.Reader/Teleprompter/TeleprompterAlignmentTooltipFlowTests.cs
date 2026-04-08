using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterAlignmentTooltipFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const string LeftRailTooltipScenario = "teleprompter-alignment-tooltips-left";
    private const string LeftRailTooltipStep = "01-left-rail-tooltip";
    private const string RightRailTooltipScenario = "teleprompter-alignment-tooltips-right";
    private const string RightRailTooltipStep = "02-right-rail-tooltip";

    private readonly record struct ElementBounds(double Left, double Top, double Right, double Bottom);

    [Test]
    public Task TeleprompterScreen_LeftRailTooltips_AppearOnlyAfterDelayAndStayOutsideButtons() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(LeftRailTooltipScenario);

            await OpenTeleprompterAsync(page);

            var trigger = page.GetByTestId(UiTestIds.Teleprompter.AlignmentJustify);
            var tooltip = page.GetByTestId(UiTestIds.Teleprompter.RailTooltip(UiTestIds.Teleprompter.AlignmentTooltipJustifyKey));

            await trigger.HoverAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.TeleprompterFlow.TooltipEarlyCheckDelayMs);

            await Assert.That(await ReadOpacityAsync(tooltip)).IsBetween(0, BrowserTestConstants.TeleprompterFlow.MaximumEarlyTooltipOpacity);

            await page.WaitForTimeoutAsync(
                BrowserTestConstants.TeleprompterFlow.TooltipSettleDelayMs - BrowserTestConstants.TeleprompterFlow.TooltipEarlyCheckDelayMs);
            await Expect(tooltip).ToBeVisibleAsync();
            await Expect(tooltip).ToHaveTextAsync(BrowserTestConstants.TeleprompterFlow.AlignmentJustifyTooltipText);

            var overlap = CalculateIntersectionArea(await ReadBoundsAsync(trigger), await ReadBoundsAsync(tooltip));
            await Assert.That(overlap).IsBetween(0, BrowserTestConstants.TeleprompterFlow.MaximumTooltipControlOverlapPx);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                LeftRailTooltipScenario,
                LeftRailTooltipStep);

            await AssertTooltipDismissesAsync(page, trigger, tooltip);
        });

    [Test]
    public Task TeleprompterScreen_RightRailTooltips_AppearOnlyAfterDelayAndStayOutsideSliders() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(RightRailTooltipScenario);

            await OpenTeleprompterAsync(page);

            var trigger = page.GetByTestId(UiTestIds.Teleprompter.WidthSlider);
            var tooltip = page.GetByTestId(UiTestIds.Teleprompter.RailTooltip(UiTestIds.Teleprompter.AlignmentTooltipWidthKey));

            await trigger.HoverAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.TeleprompterFlow.TooltipEarlyCheckDelayMs);

            await Assert.That(await ReadOpacityAsync(tooltip)).IsBetween(0, BrowserTestConstants.TeleprompterFlow.MaximumEarlyTooltipOpacity);

            await page.WaitForTimeoutAsync(
                BrowserTestConstants.TeleprompterFlow.TooltipSettleDelayMs - BrowserTestConstants.TeleprompterFlow.TooltipEarlyCheckDelayMs);
            await Expect(tooltip).ToBeVisibleAsync();
            await Expect(tooltip).ToHaveTextAsync(BrowserTestConstants.TeleprompterFlow.WidthSliderTooltipText);

            var overlap = CalculateIntersectionArea(await ReadBoundsAsync(trigger), await ReadBoundsAsync(tooltip));
            await Assert.That(overlap).IsBetween(0, BrowserTestConstants.TeleprompterFlow.MaximumTooltipControlOverlapPx);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                RightRailTooltipScenario,
                RightRailTooltipStep);

            await AssertTooltipDismissesAsync(page, trigger, tooltip);
        });

    private static double CalculateIntersectionArea(ElementBounds left, ElementBounds right)
    {
        var overlapWidth = Math.Max(0, Math.Min(left.Right, right.Right) - Math.Max(left.Left, right.Left));
        var overlapHeight = Math.Max(0, Math.Min(left.Bottom, right.Bottom) - Math.Max(left.Top, right.Top));
        return overlapWidth * overlapHeight;
    }

    private static async Task OpenTeleprompterAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
    }

    private static async Task AssertTooltipDismissesAsync(IPage page, ILocator trigger, ILocator tooltip)
    {
        await page.Mouse.MoveAsync(
            BrowserTestConstants.TooltipAuditFlow.ClearHoverX,
            BrowserTestConstants.TooltipAuditFlow.ClearHoverY);
        await Expect(tooltip).ToBeHiddenAsync(
            new() { Timeout = BrowserTestConstants.TeleprompterFlow.TooltipDismissTimeoutMs });

        await trigger.HoverAsync();
        await page.WaitForTimeoutAsync(BrowserTestConstants.TeleprompterFlow.TooltipSettleDelayMs);
        await Expect(tooltip).ToBeVisibleAsync();

        await trigger.ClickAsync();
        await Expect(tooltip).ToBeHiddenAsync(
            new() { Timeout = BrowserTestConstants.TeleprompterFlow.TooltipDismissTimeoutMs });
    }

    private static async Task<double> ReadOpacityAsync(ILocator locator) =>
        await locator.EvaluateAsync<double>(
            """
            element => Number.parseFloat(getComputedStyle(element).opacity)
            """);

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
}
