using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class PlaybackRouteDriver
{
    internal static async Task OpenLearnAsync(IPage page, string route, string? failureLabel = null)
    {
        await OpenAsync(page, route, UiTestIds.Learn.Page, failureLabel);
        await WaitForLearnReadyAsync(page, route);
    }

    internal static async Task OpenTeleprompterAsync(
        IPage page,
        string route,
        string? failureLabel = null,
        bool requireContent = true)
    {
        await OpenAsync(page, route, UiTestIds.Teleprompter.Page, failureLabel);
        await WaitForTeleprompterReadyAsync(page, route, requireContent);
    }

    internal static async Task WaitForLearnReadyAsync(IPage page, string route)
    {
        await BrowserRouteDriver.WaitForRouteAsync(page, route);
        await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Learn.Display)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Learn.Word)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Learn.ProgressLabel)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Learn.PlayToggle)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Learn.StepForward)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Learn.StepForwardLarge)).ToBeVisibleAsync();
    }

    internal static async Task WaitForTeleprompterReadyAsync(
        IPage page,
        string route,
        bool requireContent = true)
    {
        await BrowserRouteDriver.WaitForRouteAsync(page, route);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
        if (!requireContent)
        {
            return;
        }

        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Stage)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.ClusterWrap)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardText(0))).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.CardWord(0, 0, 0))).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.PlayToggle)).ToBeVisibleAsync();
    }

    private static Task OpenAsync(
        IPage page,
        string route,
        string pageTestId,
        string? failureLabel) =>
        BrowserRouteDriver.OpenPageAsync(
            page,
            route,
            pageTestId,
            failureLabel ?? $"{pageTestId}-route-open");
}
