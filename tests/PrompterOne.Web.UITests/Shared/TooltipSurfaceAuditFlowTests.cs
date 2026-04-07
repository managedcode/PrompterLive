using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TooltipSurfaceAuditFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const string GoldAccentSwatchId = "gold";

    private readonly record struct ElementBounds(double Left, double Top, double Right, double Bottom);

    private readonly record struct TooltipMetrics(
        string Placement,
        string TextTransform,
        double BorderAlpha,
        bool HasShadow);

    [Test]
    public Task LibraryScreen_FolderTooltip_UsesReadableSharedSurface() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.TooltipAuditFlow.LibraryFolderScenario);

            await OpenPageAsync(page, BrowserTestConstants.Routes.Library, UiTestIds.Library.Page);
            await AssertSharedTooltipAsync(
                page,
                UiTestIds.Library.FolderCreateStart,
                BrowserTestConstants.TooltipAuditFlow.CreateFolderTooltipText,
                BrowserTestConstants.TooltipAuditFlow.PlacementLeft);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TooltipAuditFlow.LibraryFolderScenario,
                BrowserTestConstants.TooltipAuditFlow.LibraryFolderStep);
        });

    [Test]
    public Task LibraryScreen_CardMenuTooltip_UsesReadableSharedSurface() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.TooltipAuditFlow.LibraryCardMenuScenario);

            await OpenPageAsync(page, BrowserTestConstants.Routes.Library, UiTestIds.Library.Page);
            await AssertSharedTooltipAsync(
                page,
                UiTestIds.Library.CardMenu(BrowserTestConstants.Scripts.DemoId),
                BrowserTestConstants.TooltipAuditFlow.MoreScriptActionsTooltipText,
                BrowserTestConstants.TooltipAuditFlow.PlacementLeft);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TooltipAuditFlow.LibraryCardMenuScenario,
                BrowserTestConstants.TooltipAuditFlow.LibraryCardMenuStep);
        });

    [Test]
    public Task LearnScreen_PlayTooltip_UsesReadableSharedSurface() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.TooltipAuditFlow.LearnScenario);

            await OpenPageAsync(page, BrowserTestConstants.Routes.LearnDemo, UiTestIds.Learn.Page);
            await AssertSharedTooltipAsync(
                page,
                UiTestIds.Learn.PlayToggle,
                BrowserTestConstants.TooltipAuditFlow.PlayPlaybackTooltipText,
                BrowserTestConstants.TooltipAuditFlow.PlacementTop);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TooltipAuditFlow.LearnScenario,
                BrowserTestConstants.TooltipAuditFlow.LearnPlayStep);
        });

    [Test]
    public Task TeleprompterScreen_PlayTooltip_UsesReadableSharedSurface() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.TooltipAuditFlow.TeleprompterScenario);

            await OpenPageAsync(page, BrowserTestConstants.Routes.TeleprompterDemo, UiTestIds.Teleprompter.Page);
            await AssertSharedTooltipAsync(
                page,
                UiTestIds.Teleprompter.PlayToggle,
                BrowserTestConstants.TooltipAuditFlow.PlayPlaybackTooltipText,
                BrowserTestConstants.TooltipAuditFlow.PlacementTop);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TooltipAuditFlow.TeleprompterScenario,
                BrowserTestConstants.TooltipAuditFlow.TeleprompterPlayStep);
        });

    [Test]
    public Task SettingsAppearance_AccentTooltip_UsesReadableSharedSurface() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.TooltipAuditFlow.SettingsScenario);

            await OpenAppearancePanelAsync(page);
            await AssertSharedTooltipAsync(
                page,
                UiTestIds.Settings.AccentSwatch(GoldAccentSwatchId),
                BrowserTestConstants.TooltipAuditFlow.GoldAccentTooltipText,
                BrowserTestConstants.TooltipAuditFlow.PlacementTop);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TooltipAuditFlow.SettingsScenario,
                BrowserTestConstants.TooltipAuditFlow.SettingsAccentStep);
        });

    [Test]
    public Task TeleprompterScreen_PlayTooltip_HidesOnPointerLeaveAndClick() =>
        RunPageAsync(async page =>
        {
            await OpenPageAsync(page, BrowserTestConstants.Routes.TeleprompterDemo, UiTestIds.Teleprompter.Page);
            await AssertSharedTooltipDismissesAsync(
                page,
                UiTestIds.Teleprompter.PlayToggle,
                BrowserTestConstants.TooltipAuditFlow.PlayPlaybackTooltipText);
        });

    private static async Task AssertSharedTooltipAsync(IPage page, string ownerTestId, string expectedText, string expectedPlacement)
    {
        var trigger = page.GetByTestId(ownerTestId);
        var tooltip = page.GetByTestId(UiTestIds.Tooltip.Surface(ownerTestId));

        await trigger.HoverAsync();
        await page.WaitForTimeoutAsync(BrowserTestConstants.TooltipAuditFlow.SharedTooltipSettleDelayMs);

        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(tooltip).ToHaveTextAsync(expectedText);

        var metrics = await ReadTooltipMetricsAsync(tooltip);
        var overlap = CalculateIntersectionArea(await ReadBoundsAsync(trigger), await ReadBoundsAsync(tooltip));

        await Assert.That(metrics.Placement).IsEqualTo(expectedPlacement);
        await Assert.That(metrics.TextTransform).IsEqualTo(BrowserTestConstants.TooltipAuditFlow.TextTransformNone);
        await Assert.That(metrics.BorderAlpha >= BrowserTestConstants.TooltipAuditFlow.MinimumBorderAlpha).IsTrue();
        await Assert.That(metrics.HasShadow).IsTrue();
        await Assert.That(overlap).IsBetween(0, BrowserTestConstants.TooltipAuditFlow.MaximumOverlapPx);
    }

    private static async Task AssertSharedTooltipDismissesAsync(IPage page, string ownerTestId, string expectedText)
    {
        var trigger = page.GetByTestId(ownerTestId);
        var tooltip = page.GetByTestId(UiTestIds.Tooltip.Surface(ownerTestId));

        await trigger.HoverAsync();
        await page.WaitForTimeoutAsync(BrowserTestConstants.TooltipAuditFlow.SharedTooltipSettleDelayMs);
        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(tooltip).ToHaveTextAsync(expectedText);

        await ClearTooltipHoverAsync(page);
        await Expect(tooltip).ToBeHiddenAsync(
            new() { Timeout = BrowserTestConstants.TooltipAuditFlow.SharedTooltipDismissTimeoutMs });

        await trigger.HoverAsync();
        await page.WaitForTimeoutAsync(BrowserTestConstants.TooltipAuditFlow.SharedTooltipSettleDelayMs);
        await Expect(tooltip).ToBeVisibleAsync();

        await trigger.ClickAsync();
        await Expect(tooltip).ToBeHiddenAsync(
            new() { Timeout = BrowserTestConstants.TooltipAuditFlow.SharedTooltipDismissTimeoutMs });
    }

    private static Task ClearTooltipHoverAsync(IPage page) =>
        page.Mouse.MoveAsync(
            BrowserTestConstants.TooltipAuditFlow.ClearHoverX,
            BrowserTestConstants.TooltipAuditFlow.ClearHoverY);

    private static double CalculateIntersectionArea(ElementBounds left, ElementBounds right)
    {
        var overlapWidth = Math.Max(0, Math.Min(left.Right, right.Right) - Math.Max(left.Left, right.Left));
        var overlapHeight = Math.Max(0, Math.Min(left.Bottom, right.Bottom) - Math.Max(left.Top, right.Top));
        return overlapWidth * overlapHeight;
    }

    private static async Task OpenAppearancePanelAsync(IPage page)
    {
        await OpenPageAsync(page, BrowserTestConstants.Routes.Settings, UiTestIds.Settings.Page);
        await page.GetByTestId(UiTestIds.Settings.NavAppearance).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AppearancePanel)).ToBeVisibleAsync(
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
    }

    private static async Task OpenPageAsync(IPage page, string route, string pageTestId)
    {
        await page.GotoAsync(route);
        await Expect(page.GetByTestId(pageTestId)).ToBeVisibleAsync(
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
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

    private static async Task<TooltipMetrics> ReadTooltipMetricsAsync(ILocator locator) =>
        await locator.EvaluateAsync<TooltipMetrics>(
            """
            element => {
                const parseColor = value => (value?.match(/\d+(\.\d+)?/g) ?? []).map(Number);
                const styles = window.getComputedStyle(element);
                const borderColor = parseColor(styles.borderTopColor);

                return {
                    placement: element.dataset.tooltipPlacement ?? '',
                    textTransform: styles.textTransform,
                    borderAlpha: borderColor.length >= 4 ? borderColor[3] : 1,
                    hasShadow: styles.boxShadow !== 'none'
                };
            }
            """);
}
