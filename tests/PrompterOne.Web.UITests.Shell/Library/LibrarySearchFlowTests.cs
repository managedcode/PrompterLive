using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LibrarySearchFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    [Test]
    public Task LibraryScreen_SearchMatchesFileNamesAndScriptContent() =>
        RunPageAsync(async page =>
        {
            await ShellRouteDriver.OpenLibraryAsync(page);

            var quantumCard = page.GetByTestId(BrowserTestConstants.Elements.QuantumCard);
            var demoCard = page.GetByTestId(BrowserTestConstants.Elements.DemoCard);

            await UiInteractionDriver.FillAndContinueAsync(
                page,
                UiTestIds.Header.LibrarySearch,
                BrowserTestConstants.Library.FileNameSearchQuery);
            await Expect(quantumCard).ToBeVisibleAsync();
            await Expect(demoCard).ToBeHiddenAsync();

            await UiInteractionDriver.FillAndContinueAsync(
                page,
                UiTestIds.Header.LibrarySearch,
                BrowserTestConstants.Library.ContentSearchQuery);
            await Expect(quantumCard).ToBeVisibleAsync();
            await Expect(demoCard).ToBeHiddenAsync();

            await UiInteractionDriver.FillAndContinueAsync(
                page,
                UiTestIds.Header.LibrarySearch,
                string.Empty);
            await Expect(demoCard).ToBeVisibleAsync();
        });
}
