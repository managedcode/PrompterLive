using Microsoft.Playwright;
using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

internal static class TeleprompterCameraDriver
{
    public static async Task EnsureDisabledAsync(IPage page)
    {
        var cameraToggle = page.GetByTestId(UiTestIds.Teleprompter.CameraToggle);
        await cameraToggle.ScrollIntoViewIfNeededAsync();

        if (await IsEnabledAsync(page, cameraToggle))
        {
            await cameraToggle.ClickAsync();
        }

        await page.WaitForFunctionAsync(
            BrowserTestConstants.Media.ElementHasNoStreamScript,
            UiDomIds.Teleprompter.Camera,
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await Expect(cameraToggle).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
    }

    public static async Task EnsureEnabledAsync(IPage page)
    {
        var cameraToggle = page.GetByTestId(UiTestIds.Teleprompter.CameraToggle);
        await cameraToggle.ScrollIntoViewIfNeededAsync();

        if (!await IsEnabledAsync(page, cameraToggle))
        {
            await cameraToggle.ClickAsync();
        }

        await page.WaitForFunctionAsync(
            BrowserTestConstants.Media.ElementHasVideoStreamScript,
            UiDomIds.Teleprompter.Camera,
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await Expect(cameraToggle).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
    }

    private static async Task<bool> IsEnabledAsync(IPage page, ILocator cameraToggle)
    {
        var hasStream = await page.EvaluateAsync<bool>(
            BrowserTestConstants.Media.ElementHasVideoStreamScript,
            UiDomIds.Teleprompter.Camera);

        if (hasStream)
        {
            return true;
        }

        return await HasClassAsync(cameraToggle, BrowserTestConstants.Css.ActiveClass);
    }

    private static async Task<bool> HasClassAsync(ILocator locator, string className) =>
        (await locator.GetAttributeAsync(BrowserTestConstants.Html.ClassAttribute) ?? string.Empty)
        .Split(BrowserTestConstants.Html.ClassSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Contains(className, StringComparer.Ordinal);
}
