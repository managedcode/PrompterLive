using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorStatusBarFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private readonly record struct CssColor(double R, double G, double B, double A);
    private readonly record struct StatusBarLayoutProbe(
        double BaseWpmTop,
        double CursorTop,
        double DurationTop,
        double ProfileTop,
        double SegmentsTop,
        double StatusBarHeight,
        double WordsTop);

    [Test]
    public Task EditorScreen_StatusBar_UsesCompactSingleLineStrip() =>
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
            await Expect(versionChip).ToHaveCountAsync(0);

            await Expect(cursorChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusLineLabel);
            await Expect(cursorChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusColumnLabel);
            await Expect(profileChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusProfileLabel);
            await Expect(baseWpmChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusBaseWpmLabel);
            await Expect(segmentsChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusSegmentsLabel);
            await Expect(wordsChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusWordsLabel);
            await Expect(durationChip).ToContainTextAsync(BrowserTestConstants.EditorFlow.StatusDurationLabel);

            var initialCursorText = await cursorChip.InnerTextAsync();
            var initialLayout = await ReadLayoutAsync(page);

            var statusBarBackground = await ReadCssColorAsync(statusBar, "backgroundColor");
            var profileChipBackground = await ReadCssColorAsync(profileChip, "backgroundColor");
            var profileChipRadius = await ReadPxValueAsync(profileChip, "borderRadius");

            await Assert.That(statusBarBackground.A)
                .IsBetween(BrowserTestConstants.EditorFlow.MinimumStatusBarBackgroundAlpha, double.MaxValue);
            await Assert.That(profileChipBackground.A)
                .IsBetween(0d, BrowserTestConstants.EditorFlow.MaximumStatusItemBackgroundAlpha);
            await Assert.That(profileChipRadius)
                .IsBetween(0d, BrowserTestConstants.EditorFlow.MaximumStatusItemBorderRadiusPx);
            await Assert.That(initialLayout.StatusBarHeight)
                .IsBetween(0d, BrowserTestConstants.EditorFlow.MaximumStatusBarHeightPx);

            await EditorMonacoDriver.SetCaretAtEndAsync(page);

            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const element = document.querySelector(`[data-test="${args.testId}"]`);
                    return (element?.textContent ?? "").trim() !== args.initialText;
                }
                """,
                new
                {
                    initialText = initialCursorText.Trim(),
                    testId = UiTestIds.Editor.StatusCursor
                });

            var updatedLayout = await ReadLayoutAsync(page);

            await Assert.That(Math.Abs(updatedLayout.StatusBarHeight - initialLayout.StatusBarHeight))
                .IsBetween(0d, BrowserTestConstants.EditorFlow.MaximumStatusBarHeightDeltaPx);
            await Assert.That(MaxTopDelta(updatedLayout))
                .IsBetween(0d, BrowserTestConstants.EditorFlow.MaximumStatusItemTopDeltaPx);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.StatusBarScenario,
                BrowserTestConstants.EditorFlow.StatusBarStep);
        });

    private static double MaxTopDelta(StatusBarLayoutProbe probe)
    {
        var tops = new[]
        {
            probe.BaseWpmTop,
            probe.CursorTop,
            probe.DurationTop,
            probe.ProfileTop,
            probe.SegmentsTop,
            probe.WordsTop
        };

        return tops.Max() - tops.Min();
    }

    private static Task<StatusBarLayoutProbe> ReadLayoutAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<StatusBarLayoutProbe>(
            """
            (args) => {
                const readTop = testId => document.querySelector(`[data-test="${testId}"]`)?.getBoundingClientRect().top ?? 0;
                const statusBar = document.querySelector(`[data-test="${args.statusBar}"]`);

                return {
                    baseWpmTop: readTop(args.baseWpm),
                    cursorTop: readTop(args.cursor),
                    durationTop: readTop(args.duration),
                    profileTop: readTop(args.profile),
                    segmentsTop: readTop(args.segments),
                    statusBarHeight: statusBar?.getBoundingClientRect().height ?? 0,
                    wordsTop: readTop(args.words)
                };
            }
            """,
            new
            {
                baseWpm = UiTestIds.Editor.StatusBaseWpm,
                cursor = UiTestIds.Editor.StatusCursor,
                duration = UiTestIds.Editor.StatusDuration,
                profile = UiTestIds.Editor.StatusProfile,
                segments = UiTestIds.Editor.StatusSegments,
                statusBar = UiTestIds.Editor.StatusBar,
                words = UiTestIds.Editor.StatusWords
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

    private static Task<double> ReadPxValueAsync(Microsoft.Playwright.ILocator locator, string propertyName) =>
        locator.EvaluateAsync<double>(
            """
            (element, propertyName) => Number.parseFloat(getComputedStyle(element)[propertyName] || "0")
            """,
            propertyName);
}
