using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorStatusBarFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private readonly record struct CssColor(double R, double G, double B, double A);

    [Test]
    public Task EditorScreen_StatusBar_UsesSegmentedReadableChrome() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.StatusBarScenario);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            var statusBar = page.GetByTestId(UiTestIds.Editor.StatusBar);
            var cursorChip = page.GetByTestId(UiTestIds.Editor.StatusCursor);
            var profileChip = page.GetByTestId(UiTestIds.Editor.StatusProfile);
            var baseWpmChip = page.GetByTestId(UiTestIds.Editor.StatusBaseWpm);
            var segmentsChip = page.GetByTestId(UiTestIds.Editor.StatusSegments);
            var wordsChip = page.GetByTestId(UiTestIds.Editor.StatusWords);
            var durationChip = page.GetByTestId(UiTestIds.Editor.StatusDuration);
            var versionChip = page.GetByTestId(UiTestIds.Editor.StatusVersion);

            await Expect(statusBar).ToBeVisibleAsync();
            await Expect(cursorChip).ToBeVisibleAsync();
            await Expect(profileChip).ToBeVisibleAsync();
            await Expect(baseWpmChip).ToBeVisibleAsync();
            await Expect(segmentsChip).ToBeVisibleAsync();
            await Expect(wordsChip).ToBeVisibleAsync();
            await Expect(durationChip).ToBeVisibleAsync();
            await Expect(versionChip).ToBeVisibleAsync();

            await Expect(cursorChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusLineLabel);
            await Expect(cursorChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusColumnLabel);
            await Expect(profileChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusProfileLabel);
            await Expect(baseWpmChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusBaseWpmLabel);
            await Expect(segmentsChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusSegmentsLabel);
            await Expect(wordsChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusWordsLabel);
            await Expect(durationChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusDurationLabel);
            await Expect(versionChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusVersionLabel);

            var statusBarBackgroundImage = await ReadCssValueAsync(statusBar, "backgroundImage");
            var profileChipBackground = await ReadCssColorAsync(profileChip, "backgroundColor");
            var profileChipRadius = await ReadPxValueAsync(profileChip, "borderRadius");

            await Assert.That(statusBarBackgroundImage).IsNotEqualTo(BrowserTestConstants.EditorFlow.NoneValue);
            await Assert.That(profileChipBackground.A)
                .IsBetween(BrowserTestConstants.EditorFlow.MinimumStatusChipBackgroundAlpha, double.MaxValue);
            await Assert.That(profileChipRadius)
                .IsBetween(BrowserTestConstants.EditorFlow.MinimumStatusChipBorderRadiusPx, double.MaxValue);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.StatusBarScenario,
                BrowserTestConstants.EditorFlow.StatusBarStep);
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
}
