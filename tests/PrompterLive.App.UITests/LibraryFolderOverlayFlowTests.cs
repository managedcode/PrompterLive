using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class LibraryFolderOverlayFlowTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task FolderOverlay_IsTranslucent_AndCreationAcceptsTypedInput()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/library");
            await Expect(page.GetByTestId("library-page")).ToBeVisibleAsync();

            await page.GetByTestId("library-folder-create-start").ClickAsync();
            await Expect(page.GetByTestId("library-new-folder-overlay")).ToBeVisibleAsync();

            var nameInput = page.GetByTestId("library-new-folder-name");
            Assert.Equal(string.Empty, await nameInput.InputValueAsync());
            await nameInput.ClickAsync();
            await nameInput.PressSequentiallyAsync("Roadshows");
            await Expect(nameInput).ToHaveValueAsync("Roadshows");

            var overlayStyles = await page.GetByTestId("library-new-folder-overlay").EvaluateAsync<string[]>(
                @"element => {
                    const style = getComputedStyle(element);
                    return [
                        style.backgroundColor,
                        style.backgroundImage,
                        style.backdropFilter || style.webkitBackdropFilter || ''
                    ];
                }");

            Assert.DoesNotContain("linear-gradient", overlayStyles[1], StringComparison.OrdinalIgnoreCase);
            Assert.Contains("blur", overlayStyles[2], StringComparison.OrdinalIgnoreCase);

            await page.GetByTestId("library-new-folder-parent").SelectOptionAsync("presentations");
            await page.GetByTestId("library-new-folder-submit").ClickAsync();

            await Expect(page.GetByTestId("library-new-folder-overlay")).ToBeHiddenAsync();
            await Expect(page.GetByTestId("library-folder-roadshows")).ToBeVisibleAsync();
            await Expect(page.Locator(".bc-current")).ToHaveTextAsync("Roadshows");

            await page.ReloadAsync();

            await Expect(page.GetByTestId("library-folder-roadshows")).ToBeVisibleAsync();
            await Expect(page.Locator(".bc-current")).ToHaveTextAsync("Roadshows");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
