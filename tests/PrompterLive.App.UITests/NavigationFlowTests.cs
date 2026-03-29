using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class NavigationFlowTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task ScreenNavigation_UsesSpaRoutingWithoutReloadingBrowserContext()
    {
        const string nonce = "spa-nav-stable";
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.EvaluateAsync("value => window.__prompterSpaNonce = value", nonce);

            await page.GetByTestId(UiTestIds.Header.EditorLearn).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LearnQuantum));
            await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync();
            Assert.Equal(nonce, await page.EvaluateAsync<string>("() => window.__prompterSpaNonce"));

            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await page.EvaluateAsync("value => window.__prompterSpaNonce = value", nonce);

            await page.GetByTestId(UiTestIds.Header.EditorRead).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.TeleprompterQuantum));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
            Assert.Equal(nonce, await page.EvaluateAsync<string>("() => window.__prompterSpaNonce"));

            await page.GetByTestId(UiTestIds.Teleprompter.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.EditorQuantum));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            Assert.Equal(nonce, await page.EvaluateAsync<string>("() => window.__prompterSpaNonce"));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
