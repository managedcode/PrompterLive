using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterStylesheetFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture){
    private const string HasTeleprompterStylesheetScript = """
        teleprompterHref => {
            const normalizedTargetPath = teleprompterHref.startsWith('/') ? teleprompterHref : `/${teleprompterHref}`;
            return Array.from(document.styleSheets)
            .map(sheet => sheet.href ?? "")
            .some(href => {
                if (!href) {
                    return false;
                }

                return new URL(href).pathname === normalizedTargetPath;
            });
        }
        """;

    [Test]
    public Task TeleprompterStylesheet_IsRegisteredBeforeTeleprompterRouteEntry() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var hasTeleprompterStylesheet = await page.EvaluateAsync<bool>(
                HasTeleprompterStylesheetScript,
                DesignStylesheetPaths.Teleprompter);

            await Assert.That(hasTeleprompterStylesheet).IsTrue();

            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
        });
}