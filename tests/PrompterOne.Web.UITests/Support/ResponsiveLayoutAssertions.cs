using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

internal static class ResponsiveLayoutAssertions
{
    public static async Task AssertRouteControlsVisibleAsync(
        IPage page,
        string routeName,
        string route,
        ResponsiveViewport viewport,
        string pageTestId,
        params string[] controlTestIds)
    {
        await page.SetViewportSizeAsync(viewport.Width, viewport.Height);
        await BrowserRouteDriver.OpenPageAsync(
            page,
            route,
            pageTestId,
            $"{routeName}-{viewport.Name}");

        await WaitForRouteReadyAsync(page, route, pageTestId);

        foreach (var controlTestId in controlTestIds)
        {
            await AssertVisibleWithinViewportAsync(page, page.GetByTestId(controlTestId), controlTestId, routeName, viewport);
        }

        await UiScenarioArtifacts.CapturePageAsync(
            page,
            BuildScenarioName(routeName, viewport),
            BrowserTestConstants.ResponsiveLayout.InitialStep);
    }

    private static string BuildScenarioName(string routeName, ResponsiveViewport viewport) =>
        string.Join(
            BrowserTestConstants.ScenarioArtifacts.Separator,
            BrowserTestConstants.ResponsiveLayout.ScenarioPrefix,
            routeName,
            viewport.Name);

    private static Task WaitForRouteReadyAsync(
        IPage page,
        string route,
        string pageTestId) =>
        pageTestId switch
        {
            UiTestIds.Learn.Page => PlaybackRouteDriver.WaitForLearnReadyAsync(page, route),
            UiTestIds.Teleprompter.Page => PlaybackRouteDriver.WaitForTeleprompterReadyAsync(page, route),
            _ => Task.CompletedTask
        };

    internal static async Task AssertVisibleWithinViewportAsync(
        IPage page,
        ILocator locator,
        string controlTestId,
        string routeName,
        ResponsiveViewport viewport)
    {
        await Expect(locator)
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

        var bounds = await UiInteractionDriver.GetBoundingClientRectAsync(page, controlTestId);
        var tolerance = BrowserTestConstants.ResponsiveLayout.ViewportEdgeTolerancePx;

        await Assert.That(bounds.X >= -tolerance && bounds.X <= viewport.Width)
            .IsTrue()
            .Because($"Element '{controlTestId}' left edge {bounds.X} was outside viewport width {viewport.Width} on {routeName} for {viewport.Name}.");
        await Assert.That(bounds.Y >= -tolerance && bounds.Y <= viewport.Height)
            .IsTrue()
            .Because($"Element '{controlTestId}' top edge {bounds.Y} was outside viewport height {viewport.Height} on {routeName} for {viewport.Name}.");
        await Assert.That(bounds.X + bounds.Width <= viewport.Width + tolerance)
            .IsTrue()
            .Because($"Element '{controlTestId}' right edge {bounds.X + bounds.Width} exceeded viewport width {viewport.Width} on {routeName} for {viewport.Name}.");
        await Assert.That(bounds.Y + bounds.Height <= viewport.Height + tolerance)
            .IsTrue()
            .Because($"Element '{controlTestId}' bottom edge {bounds.Y + bounds.Height} exceeded viewport height {viewport.Height} on {routeName} for {viewport.Name}.");
    }
}
