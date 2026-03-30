using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

public sealed class TeleprompterFidelityTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    [Fact]
    public async Task TeleprompterScreen_UsesSingleFullBleedBackgroundCameraLayer()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await Expect(page.Locator(BrowserTestConstants.Elements.CameraOverlaySelector)).ToHaveCountAsync(0);
            await EnsureCameraLayerIsActiveAsync(page);

            var isFullBleed = await page.EvaluateAsync<bool>(
                $$"""
                () => {
                    const camera = document.querySelector('[data-testid="teleprompter-camera-layer-primary"]');
                    const shell = document.querySelector('{{BrowserTestConstants.Elements.TeleprompterShellSelector}}');
                    if (!(camera instanceof HTMLElement) || !(shell instanceof HTMLElement)) {
                        return false;
                    }

                    const cameraRect = camera.getBoundingClientRect();
                    const shellRect = shell.getBoundingClientRect();
                    return cameraRect.width >= shellRect.width * 0.95 && cameraRect.height >= shellRect.height * 0.95;
                }
                """);

            Assert.True(isFullBleed);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task EnsureCameraLayerIsActiveAsync(Microsoft.Playwright.IPage page)
    {
        var cameraLayer = page.GetByTestId(UiTestIds.Teleprompter.CameraBackground);
        await TeleprompterCameraDriver.EnsureEnabledAsync(page);
        await Expect(cameraLayer).ToHaveClassAsync(BrowserTestConstants.Regexes.ActiveClass);
    }
}
