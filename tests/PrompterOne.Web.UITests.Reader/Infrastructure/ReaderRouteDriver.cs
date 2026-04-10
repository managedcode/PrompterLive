using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class ReaderRouteDriver
{
    internal static async Task OpenLearnAsync(
        IPage page,
        string route,
        [CallerMemberName] string scenarioName = "")
    {
        await OpenAsync(page, route, UiTestIds.Learn.Page, scenarioName);
        await WaitForLearnReadyAsync(page);
    }

    internal static async Task OpenTeleprompterAsync(
        IPage page,
        string route,
        [CallerMemberName] string scenarioName = "")
    {
        await OpenAsync(page, route, UiTestIds.Teleprompter.Page, scenarioName);
        await WaitForTeleprompterReadyAsync(page);
    }

    internal static async Task OpenSettingsAsync(
        IPage page,
        [CallerMemberName] string scenarioName = "")
    {
        await OpenAsync(page, BrowserTestConstants.Routes.Settings, UiTestIds.Settings.Page, scenarioName);
        await WaitForSettingsReadyAsync(page);
    }

    private static Task OpenAsync(
        IPage page,
        string route,
        string pageTestId,
        string scenarioName) =>
        BrowserRouteDriver.OpenPageAsync(
            page,
            route,
            pageTestId,
            $"{scenarioName}-{pageTestId}");

    private static async Task WaitForLearnReadyAsync(IPage page)
    {
        await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Learn.ProgressLabel)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Learn.PlayToggle)).ToBeVisibleAsync();
    }

    private static async Task WaitForTeleprompterReadyAsync(IPage page)
    {
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Stage)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.PlayToggle)).ToBeVisibleAsync();
    }

    private static async Task WaitForSettingsReadyAsync(IPage page)
    {
        await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.Title)).ToBeVisibleAsync();
    }
}
