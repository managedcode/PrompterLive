using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorFindFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture){
    [Test]
    public Task EditorScreen_FindBarSelectsMatches_AndShowsNoResultState() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await page.GetByTestId(UiTestIds.Editor.FindToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FindBar)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.FindInput).FillAsync(BrowserTestConstants.Editor.FindQuery);
            await Expect(page.GetByTestId(UiTestIds.Editor.FindResult))
                .ToHaveTextAsync(BrowserTestConstants.Editor.FindSingleMatchSummary);

            var state = await EditorMonacoDriver.GetStateAsync(page);
            var selectedText = state.Text.Substring(
                state.Selection.Start,
                state.Selection.End - state.Selection.Start);
            // TODO: TUnit migration - xUnit Assert.Equal had additional argument(s) (ignoreCase: true) that could not be converted.
            await Assert.That(selectedText).IsEqualTo(BrowserTestConstants.Editor.FindQuery);

            await page.GetByTestId(UiTestIds.Editor.FindInput).FillAsync(BrowserTestConstants.Editor.FindMissingQuery);
            await Expect(page.GetByTestId(UiTestIds.Editor.FindResult))
                .ToHaveTextAsync(BrowserTestConstants.Editor.FindNoMatches);
            await Expect(page.GetByTestId(UiTestIds.Editor.FindNext)).ToBeDisabledAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FindPrevious)).ToBeDisabledAsync();
        });
}