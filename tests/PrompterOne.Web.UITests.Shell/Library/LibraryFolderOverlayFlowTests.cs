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
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ShellRouteDriver.OpenLibraryAsync(page);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.FolderCreateStart),
                noWaitAfter: true);
            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeVisibleAsync();

            var nameInput = page.GetByTestId(UiTestIds.Library.NewFolderName);
            await Assert.That(await nameInput.InputValueAsync()).IsEqualTo(string.Empty);
            await UiInteractionDriver.ClickAndContinueAsync(nameInput, noWaitAfter: true);
            await nameInput.FillAsync(BrowserTestConstants.Folders.RoadshowsName);
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

            var parentSelect = page.GetByTestId(UiTestIds.Library.NewFolderParent);
            var selectedParentId = await parentSelect.EvaluateAsync<string>(
                """
                (element, value) => {
                    if (!(element instanceof HTMLSelectElement)) {
                        throw new Error("Expected the new folder parent field to be a select element.");
                    }

                    element.value = value;
                    element.dispatchEvent(new Event("input", { bubbles: true }));
                    element.dispatchEvent(new Event("change", { bubbles: true }));
                    return element.value;
                }
                """,
                BrowserTestConstants.Folders.PresentationsId);
            await Assert.That(selectedParentId).IsEqualTo(BrowserTestConstants.Folders.PresentationsId);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.NewFolderSubmit),
                noWaitAfter: true);

            await Expect(page.GetByTestId(UiTestIds.Library.NewFolderOverlay)).ToBeHiddenAsync();
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);

            await BrowserRouteDriver.ReloadPageAsync(
                page,
                BrowserTestConstants.Routes.Library,
                UiTestIds.Library.Page,
                "library-folder-overlay-reload");
            await Expect(page.GetByTestId(BrowserTestConstants.Elements.RoadshowsFolder)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.LibraryBreadcrumbCurrent)).ToHaveTextAsync(BrowserTestConstants.Folders.RoadshowsName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
