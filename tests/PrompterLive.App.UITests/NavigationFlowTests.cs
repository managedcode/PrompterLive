using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class NavigationFlowTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task ScreenNavigation_UsesSpaRoutingWithoutReloadingBrowserContext()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/editor?id=quantum-computing");
            await Expect(page.GetByTestId("editor-page")).ToBeVisibleAsync(new() { Timeout = 15_000 });

            const string nonce = "spa-nav-stable";
            await page.EvaluateAsync("value => window.__prompterSpaNonce = value", nonce);

            await page.GetByRole(AriaRole.Button, new() { Name = "Learn" }).ClickAsync();
            await page.WaitForURLAsync("**/learn?id=quantum-computing");
            await Expect(page.GetByTestId("learn-page")).ToBeVisibleAsync();
            Assert.Equal(nonce, await page.EvaluateAsync<string>("() => window.__prompterSpaNonce"));

            await page.GotoAsync("/editor?id=quantum-computing");
            await Expect(page.GetByTestId("editor-page")).ToBeVisibleAsync();
            await page.EvaluateAsync("value => window.__prompterSpaNonce = value", nonce);

            await page.GetByRole(AriaRole.Button, new() { Name = "Read" }).ClickAsync();
            await page.WaitForURLAsync("**/teleprompter?id=quantum-computing");
            await Expect(page.GetByTestId("teleprompter-page")).ToBeVisibleAsync();
            Assert.Equal(nonce, await page.EvaluateAsync<string>("() => window.__prompterSpaNonce"));

            await page.GetByTestId("teleprompter-back").ClickAsync();
            await page.WaitForURLAsync("**/editor?id=quantum-computing");
            await Expect(page.GetByTestId("editor-page")).ToBeVisibleAsync();
            Assert.Equal(nonce, await page.EvaluateAsync<string>("() => window.__prompterSpaNonce"));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
