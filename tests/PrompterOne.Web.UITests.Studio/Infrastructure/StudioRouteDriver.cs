using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class StudioRouteDriver
{
    internal static async Task OpenLibraryAsync(
        IPage page,
        [CallerMemberName] string scenarioName = "")
    {
        await OpenAsync(page, BrowserTestConstants.Routes.Library, UiTestIds.Library.Page, scenarioName);
        await WaitForLibraryReadyAsync(page);
    }

    internal static async Task OpenSettingsAsync(
        IPage page,
        [CallerMemberName] string scenarioName = "")
    {
        await OpenAsync(page, BrowserTestConstants.Routes.Settings, UiTestIds.Settings.Page, scenarioName);
        await WaitForSettingsReadyAsync(page);
    }

    internal static Task OpenGoLiveAsync(
        IPage page,
        [CallerMemberName] string scenarioName = "") =>
        OpenGoLiveRouteAsync(page, BrowserTestConstants.Routes.GoLiveDemo, scenarioName);

    internal static async Task OpenGoLiveRouteAsync(
        IPage page,
        string route,
        [CallerMemberName] string scenarioName = "")
    {
        await OpenAsync(page, route, UiTestIds.GoLive.Page, scenarioName);
        await WaitForGoLiveReadyAsync(page, route);
    }

    internal static Task OpenTeleprompterAsync(
        IPage page,
        [CallerMemberName] string scenarioName = "") =>
        OpenAsync(page, BrowserTestConstants.Routes.TeleprompterDemo, UiTestIds.Teleprompter.Page, scenarioName);

    internal static async Task NavigateToGoLiveFromSettingsAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.Settings.CameraRoutingCta).ClickAsync();
        await WaitForGoLiveReadyAsync(page, AppRoutes.GoLive);
    }

    internal static async Task NavigateToGoLiveFromHeaderAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.Header.GoLive).ClickAsync();
        await WaitForGoLiveReadyAsync(page, AppRoutes.GoLive);
    }

    internal static async Task NavigateBackToLibraryAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.GoLive.Back).ClickAsync();
        await WaitForLibraryReadyAsync(page);
    }

    internal static async Task NavigateBackToSettingsAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.GoLive.Back).ClickAsync();
        await WaitForSettingsReadyAsync(page);
    }

    internal static async Task NavigateToSettingsFromGoLiveAsync(IPage page)
    {
        await page.GetByTestId(UiTestIds.GoLive.OpenSettings).ClickAsync();
        await WaitForSettingsReadyAsync(page);
    }

    internal static Task WaitForLibraryReadyAsync(IPage page) =>
        WaitForLibraryReadyAsync(page, BrowserTestConstants.Routes.Library);

    internal static async Task WaitForLibraryReadyAsync(
        IPage page,
        string route)
    {
        await BrowserRouteDriver.WaitForRouteAsync(page, route);
        await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Library.SortLabel)).ToBeVisibleAsync();
    }

    internal static Task WaitForSettingsReadyAsync(IPage page) =>
        WaitForSettingsReadyAsync(page, BrowserTestConstants.Routes.Settings);

    internal static async Task WaitForSettingsReadyAsync(
        IPage page,
        string route)
    {
        await BrowserRouteDriver.WaitForRouteAsync(page, route);
        await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.Title)).ToBeVisibleAsync();
    }

    internal static async Task WaitForGoLiveReadyAsync(
        IPage page,
        string route)
    {
        await BrowserRouteDriver.WaitForRouteAsync(page, route);
        await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.GoLive.ProgramCard)).ToBeVisibleAsync();
        await Expect(page.GetByTestId(UiTestIds.GoLive.SourcesCard)).ToBeVisibleAsync();
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
}
