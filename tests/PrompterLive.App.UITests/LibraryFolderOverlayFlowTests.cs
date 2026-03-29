using PrompterLive.Shared.Contracts;
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
            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Library.FolderCreateStart).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();

            var nameInput = page.GetByTestId(UiTestIds.Library.NewFolderName);
            Assert.Equal(string.Empty, await nameInput.InputValueAsync());
            await nameInput.ClickAsync();
            await nameInput.PressSequentiallyAsync(BrowserTestConstants.Folders.RoadshowsName);
            await Expect(nameInput).ToHaveValueAsync(BrowserTestConstants.Folders.RoadshowsName);

            var overlayStyles = await page.GetByTestId(UiTestIds.Library.NewFolderOverlay).EvaluateAsync<string[]>(
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

            await page.GetByTestId(UiTestIds.Library.NewFolderParent).SelectOptionAsync(BrowserTestConstants.Folders.PresentationsId);
            await page.GetByTestId(UiTestIds.Library.NewFolderSubmit).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);

            await page.ReloadAsync();

            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
