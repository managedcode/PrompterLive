using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
[NotInParallel(UiTestParallelization.EditorAuthoringConstraintKey)]
public sealed class EditorFloatingToolbarLayoutTests(StandaloneAppFixture fixture)
{
    private readonly record struct LayoutBounds(double Y, double Height);
    private readonly record struct ToolbarAnchor(double Left, double Top);
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_FloatingToolbarKeepsFullHeightWhenSelectionIsActive()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GotoEditorAndWaitForSourceAsync(page);
            await EditorMonacoDriver.SetTextAsync(page, BrowserTestConstants.Editor.TypedScript);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TypedSelectionTarget);

            var floatingBar = page.GetByTestId(UiTestIds.Editor.FloatingBar);
            await Expect(floatingBar).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);

            var bounds = await GetRequiredBoundingBoxAsync(floatingBar);
            await Assert.That(bounds.Height).IsBetween(BrowserTestConstants.Editor.FloatingBarMinHeightPx, double.MaxValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_FloatingToolbarStaysAboveMultiLineSelection()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GotoEditorAndWaitForSourceAsync(page);
            await EditorMonacoDriver.SetTextAsync(page, BrowserTestConstants.Editor.TypedMultilineScript);
            var state = await EditorMonacoDriver.GetStateAsync(page);
            var start = state.Text.IndexOf(BrowserTestConstants.Editor.TypedMultilineSelectionStart, StringComparison.Ordinal);
            var endTokenStart = state.Text.IndexOf(BrowserTestConstants.Editor.TypedMultilineSelectionEnd, start, StringComparison.Ordinal);
            await Assert.That(start >= 0).IsTrue();
            await Assert.That(endTokenStart >= 0).IsTrue();
            await EditorMonacoDriver.SetSelectionAsync(
                page,
                start,
                endTokenStart + BrowserTestConstants.Editor.TypedMultilineSelectionEnd.Length);

            var geometry = await page.GetByTestId(UiTestIds.Editor.SourceHighlight).EvaluateAsync<SelectionGeometry>(
                """
                (element, probeText) => {
                    const firstSelectedLine = Array
                        .from(element.children)
                        .find(node => (node.textContent ?? '').includes(probeText));

                    if (!firstSelectedLine) {
                        throw new Error(`Unable to locate the rendered source line that contains "${probeText}".`);
                    }

                    const rect = firstSelectedLine.getBoundingClientRect();
                    return {
                        selectionTop: rect.top
                    };
                }
                """,
                BrowserTestConstants.Editor.TypedMultilineSelectionProbeLine);

            var floatingBar = page.GetByTestId(UiTestIds.Editor.FloatingBar);
            await Expect(floatingBar).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);

            var bounds = await GetRequiredBoundingBoxAsync(floatingBar);
            await Assert.That(geometry.SelectionTop - (bounds.Y + bounds.Height)).IsBetween(BrowserTestConstants.Editor.FloatingBarMinGapAboveSelectionPx, double.MaxValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_FloatingToolbarStaysPinnedAfterFloatingFormatAction()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GotoEditorAndWaitForSourceAsync(page);
            await EditorMonacoDriver.SetCaretAtTextStartAsync(page, BrowserTestConstants.Editor.ToolbarPinnedSelectionTarget);
            await EditorMonacoDriver.FocusAsync(page);
            await EditorMonacoDriver.PressKeyRepeatedlyAsync(
                page,
                BrowserTestConstants.Keyboard.ShiftArrowRight,
                BrowserTestConstants.Editor.ToolbarPinnedSelectionCharacterCount);

            var floatingBar = page.GetByTestId(UiTestIds.Editor.FloatingBar);
            await Expect(floatingBar).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);

            var before = await GetRequiredAnchorAsync(floatingBar);

            await page.GetByTestId(UiTestIds.Editor.FloatEmphasis).ClickAsync();
            await Expect(floatingBar).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);

            var after = await GetRequiredAnchorAsync(floatingBar);

            await Assert.That(Math.Abs(after.Left - before.Left)).IsBetween(0, BrowserTestConstants.Editor.FloatingBarPinnedMaxDriftPx);
            await Assert.That(Math.Abs(after.Top - before.Top)).IsBetween(0, BrowserTestConstants.Editor.FloatingBarPinnedMaxDriftPx);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task<LayoutBounds> GetRequiredBoundingBoxAsync(ILocator locator) =>
        await locator.EvaluateAsync<LayoutBounds>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                return {
                    y: rect.y,
                    height: rect.height
                };
            }
            """);

    private static async Task<ToolbarAnchor> GetRequiredAnchorAsync(ILocator locator) =>
        await locator.EvaluateAsync<ToolbarAnchor>(
            """
            element => {
                const style = window.getComputedStyle(element);
                return {
                    left: Number.parseFloat(style.left),
                    top: Number.parseFloat(style.top)
                };
            }
            """);

    private static async Task GotoEditorAndWaitForSourceAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);

        var editorPage = page.GetByTestId(UiTestIds.Editor.Page);
        await Expect(editorPage).ToBeVisibleAsync(new()
        {
            Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
        });

        await EditorMonacoDriver.WaitUntilReadyAsync(page);
    }

    private readonly record struct SelectionGeometry(double SelectionTop);
}
