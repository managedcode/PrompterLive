using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LibrarySearchFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture){
    [Test]
    public Task LibraryScreen_SearchMatchesFileNamesAndScriptContent() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Library);

            var searchInput = page.GetByTestId(UiTestIds.Header.LibrarySearch);
            var quantumCard = page.GetByTestId(BrowserTestConstants.Elements.QuantumCard);
            var demoCard = page.GetByTestId(BrowserTestConstants.Elements.DemoCard);

            await searchInput.FillAsync(BrowserTestConstants.Library.FileNameSearchQuery);
            await Expect(quantumCard).ToBeVisibleAsync();
            await Expect(demoCard).ToBeHiddenAsync();

            await searchInput.FillAsync(BrowserTestConstants.Library.ContentSearchQuery);
            await Expect(quantumCard).ToBeVisibleAsync();
            await Expect(demoCard).ToBeHiddenAsync();

            await searchInput.FillAsync(string.Empty);
            await Expect(demoCard).ToBeVisibleAsync();
        });
}