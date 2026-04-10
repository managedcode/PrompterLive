using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class ShellRouteDriver
{
    internal static Task OpenLibraryAsync(
        IPage page,
        [CallerMemberName] string scenarioName = "") =>
        OpenLibraryRouteAsync(page, BrowserTestConstants.Routes.Library, scenarioName);

    internal static async Task OpenLibraryRouteAsync(
        IPage page,
        string route,
        [CallerMemberName] string scenarioName = "")
    {
        await BrowserRouteDriver.OpenPageAsync(
            page,
            route,
            UiTestIds.Library.Page,
            $"{scenarioName}-{UiTestIds.Library.Page}");
        await WaitForLibraryReadyAsync(page, route);
    }

    internal static Task OpenSettingsAsync(
        IPage page,
        [CallerMemberName] string scenarioName = "") =>
        OpenSettingsRouteAsync(page, BrowserTestConstants.Routes.Settings, scenarioName);

    internal static async Task OpenSettingsRouteAsync(
        IPage page,
        string route,
        [CallerMemberName] string scenarioName = "")
    {
        await BrowserRouteDriver.OpenPageAsync(
            page,
            route,
            UiTestIds.Settings.Page,
            $"{scenarioName}-{UiTestIds.Settings.Page}");
        await WaitForSettingsReadyAsync(page, route);
    }

    internal static async Task OpenGoLiveRouteAsync(
        IPage page,
        string route,
        [CallerMemberName] string scenarioName = "")
    {
        await BrowserRouteDriver.OpenPageAsync(
            page,
            route,
            UiTestIds.GoLive.Page,
            $"{scenarioName}-{UiTestIds.GoLive.Page}");
        await WaitForGoLiveReadyAsync(page, route);
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
}
