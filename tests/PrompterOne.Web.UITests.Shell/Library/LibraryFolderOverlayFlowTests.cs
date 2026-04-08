using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LibraryFolderOverlayFlowTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
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
            await Assert.That(await nameInput.InputValueAsync()).IsEqualTo(string.Empty);
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

            await Assert.That(overlayStyles[1]).DoesNotContain("linear-gradient");
            await Assert.That(overlayStyles[2]).Contains("blur");

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
