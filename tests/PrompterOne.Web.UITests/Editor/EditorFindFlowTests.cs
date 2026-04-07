using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorFindFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private readonly record struct CssColor(double R, double G, double B, double A);

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

    [Test]
    public Task EditorScreen_FindBar_UsesStyledChrome() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.FindSurfaceScenario);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await page.GetByTestId(UiTestIds.Editor.FindToggle).ClickAsync();

            var findBar = page.GetByTestId(UiTestIds.Editor.FindBar);
            var inputShell = page.GetByTestId(UiTestIds.Editor.FindInputShell);
            var previousButton = page.GetByTestId(UiTestIds.Editor.FindPrevious);

            await Expect(findBar).ToBeVisibleAsync();
            await Expect(inputShell).ToBeVisibleAsync();
            await Expect(previousButton).ToBeVisibleAsync();

            var findBarBackgroundImage = await ReadCssValueAsync(findBar, "backgroundImage");
            var inputShellBackground = await ReadCssColorAsync(inputShell, "backgroundColor");
            var inputShellRadius = await ReadPxValueAsync(inputShell, "borderRadius");
            var previousButtonBackground = await ReadCssColorAsync(previousButton, "backgroundColor");
            var previousButtonRadius = await ReadPxValueAsync(previousButton, "borderRadius");

            await Assert.That(findBarBackgroundImage).IsNotEqualTo(BrowserTestConstants.EditorFlow.NoneValue);
            await Assert.That(inputShellBackground.A)
                .IsBetween(BrowserTestConstants.EditorFlow.MinimumFindShellBackgroundAlpha, double.MaxValue);
            await Assert.That(inputShellRadius)
                .IsBetween(BrowserTestConstants.EditorFlow.MinimumFindShellBorderRadiusPx, double.MaxValue);
            await Assert.That(previousButtonBackground.A)
                .IsBetween(0d, BrowserTestConstants.EditorFlow.MaximumFindButtonBackgroundAlpha);
            await Assert.That(previousButtonRadius)
                .IsBetween(BrowserTestConstants.EditorFlow.MinimumFindButtonBorderRadiusPx, double.MaxValue);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.FindSurfaceScenario,
                BrowserTestConstants.EditorFlow.FindSurfaceStep);
        });

    [Test]
    public Task EditorScreen_FindBar_KeepsFocusInSearchInputWhileTyping() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.FindFocusScenario);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            var input = page.GetByTestId(UiTestIds.Editor.FindInput);

            await page.GetByTestId(UiTestIds.Editor.FindToggle).ClickAsync();
            await Expect(input).ToBeVisibleAsync();

            await input.ClickAsync();
            await input.TypeAsync("i");
            await Expect(input).ToHaveValueAsync("i");
            await ExpectActiveElementDataTestAsync(page, UiTestIds.Editor.FindInput);

            await input.TypeAsync("n");
            await Expect(input).ToHaveValueAsync("in");
            await ExpectActiveElementDataTestAsync(page, UiTestIds.Editor.FindInput);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.FindFocusScenario,
                BrowserTestConstants.EditorFlow.FindFocusStep);
        });

    private static async Task<CssColor> ReadCssColorAsync(Microsoft.Playwright.ILocator locator, string propertyName) =>
        await locator.EvaluateAsync<CssColor>(
            """
            (element, propertyName) => {
                const value = getComputedStyle(element)[propertyName];
                const match = value.match(/rgba?\(([^)]+)\)/);
                if (!match) {
                    return { r: 0, g: 0, b: 0, a: 0 };
                }

                const parts = match[1].split(',').map(part => Number.parseFloat(part.trim()));
                return {
                    r: parts[0] ?? 0,
                    g: parts[1] ?? 0,
                    b: parts[2] ?? 0,
                    a: parts[3] ?? 1
                };
            }
            """,
            propertyName);

    private static Task<string> ReadCssValueAsync(Microsoft.Playwright.ILocator locator, string propertyName) =>
        locator.EvaluateAsync<string>(
            "(element, propertyName) => getComputedStyle(element)[propertyName]",
            propertyName);

    private static Task<double> ReadPxValueAsync(Microsoft.Playwright.ILocator locator, string propertyName) =>
        locator.EvaluateAsync<double>(
            """
            (element, propertyName) => Number.parseFloat(getComputedStyle(element)[propertyName] || "0")
            """,
            propertyName);

    private static async Task ExpectActiveElementDataTestAsync(Microsoft.Playwright.IPage page, string expectedTestId)
    {
        var activeElementTestId = await page.EvaluateAsync<string>(
            """
            attributeName => document.activeElement?.getAttribute(attributeName) ?? ""
            """,
            BrowserTestConstants.Html.DataTestAttribute);

        await Assert.That(activeElementTestId).IsEqualTo(expectedTestId);
    }
}
