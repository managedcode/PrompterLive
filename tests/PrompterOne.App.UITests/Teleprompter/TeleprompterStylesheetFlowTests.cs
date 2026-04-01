using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class TeleprompterStylesheetFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private const string HasTeleprompterStylesheetScript = """
        teleprompterHref => Array.from(document.styleSheets)
            .map(sheet => sheet.href ?? "")
            .some(href => {
                if (!href) {
                    return false;
                }

                return new URL(href).pathname === teleprompterHref;
            })
        """;

    [Fact]
    public Task TeleprompterStylesheet_IsRegisteredBeforeTeleprompterRouteEntry() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var hasTeleprompterStylesheet = await page.EvaluateAsync<bool>(
                HasTeleprompterStylesheetScript,
                DesignStylesheetPaths.Teleprompter);

            Assert.True(hasTeleprompterStylesheet);

            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        });
}
