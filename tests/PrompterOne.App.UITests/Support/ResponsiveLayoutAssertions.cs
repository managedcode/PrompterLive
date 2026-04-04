using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

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
        await page.GotoAsync(route);

        await Expect(page.GetByTestId(pageTestId))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

        await UiScenarioArtifacts.CapturePageAsync(
            page,
            BuildScenarioName(routeName, viewport),
            BrowserTestConstants.ResponsiveLayout.InitialStep);

        foreach (var controlTestId in controlTestIds)
        {
            await AssertVisibleWithinViewportAsync(page.GetByTestId(controlTestId), controlTestId, routeName, viewport);
        }
    }

    private static string BuildScenarioName(string routeName, ResponsiveViewport viewport) =>
        string.Join(
            BrowserTestConstants.ScenarioArtifacts.Separator,
            BrowserTestConstants.ResponsiveLayout.ScenarioPrefix,
            routeName,
            viewport.Name);

    internal static async Task AssertVisibleWithinViewportAsync(
        ILocator locator,
        string controlTestId,
        string routeName,
        ResponsiveViewport viewport)
    {
        await Expect(locator)
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);

        var bounds = box!;
        var tolerance = BrowserTestConstants.ResponsiveLayout.ViewportEdgeTolerancePx;

        Assert.True(
            bounds.X >= -tolerance && bounds.X <= viewport.Width,
            $"Element '{controlTestId}' left edge {bounds.X} was outside viewport width {viewport.Width} on {routeName} for {viewport.Name}.");
        Assert.True(
            bounds.Y >= -tolerance && bounds.Y <= viewport.Height,
            $"Element '{controlTestId}' top edge {bounds.Y} was outside viewport height {viewport.Height} on {routeName} for {viewport.Name}.");
        Assert.True(
            bounds.X + bounds.Width <= viewport.Width + tolerance,
            $"Element '{controlTestId}' right edge {bounds.X + bounds.Width} exceeded viewport width {viewport.Width} on {routeName} for {viewport.Name}.");
        Assert.True(
            bounds.Y + bounds.Height <= viewport.Height + tolerance,
            $"Element '{controlTestId}' bottom edge {bounds.Y + bounds.Height} exceeded viewport height {viewport.Height} on {routeName} for {viewport.Name}.");
    }
}
