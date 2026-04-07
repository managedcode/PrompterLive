using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

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
            UiTestIds.Teleprompter.CameraBackground,
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await Expect(cameraToggle)
            .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.InactiveStateValue);
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
            UiTestIds.Teleprompter.CameraBackground,
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        await Expect(cameraToggle)
            .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.ActiveStateValue);
    }

    private static async Task<bool> IsEnabledAsync(IPage page, ILocator cameraToggle)
    {
        var hasStream = await page.EvaluateAsync<bool>(
            BrowserTestConstants.Media.ElementHasVideoStreamScript,
            UiTestIds.Teleprompter.CameraBackground);

        if (hasStream)
        {
            return true;
        }

        var state = await cameraToggle.GetAttributeAsync(BrowserTestConstants.State.ActiveAttribute);
        return string.Equals(state, BrowserTestConstants.Teleprompter.ActiveStateValue, StringComparison.Ordinal);
    }
}
