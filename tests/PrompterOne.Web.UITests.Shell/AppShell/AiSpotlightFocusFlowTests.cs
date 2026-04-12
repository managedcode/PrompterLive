using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class AiSpotlightFocusFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    [Test]
    public Task ShellHeader_AiSpotlightFocusesPrompt_WhenOpenedFromLibrary() =>
        RunPageAsync(async page =>
        {
            await ShellRouteDriver.OpenLibraryAsync(page);

            await page.GetByTestId(UiTestIds.Header.AiSpotlight).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.AiSpotlight.Overlay)).ToBeVisibleAsync();
            await ExpectPromptInputFocusAsync(page);
        });

    private static async Task ExpectPromptInputFocusAsync(Microsoft.Playwright.IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            ({ attributeName, testId }) => document.activeElement?.getAttribute(attributeName) === testId
            """,
            new
            {
                attributeName = BrowserTestConstants.Html.DataTestAttribute,
                testId = UiTestIds.AiSpotlight.PromptInput
            },
            new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
    }
}
