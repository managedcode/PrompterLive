using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[Collection(EditorAuthoringCollection.Name)]
public sealed class EditorFloatingToolbarLayoutTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly record struct LayoutBounds(double Y, double Height);
    private readonly record struct ToolbarAnchor(double Left, double Top);
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
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
            Assert.InRange(
                bounds.Height,
                BrowserTestConstants.Editor.FloatingBarMinHeightPx,
                double.MaxValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
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
            Assert.True(start >= 0);
            Assert.True(endTokenStart >= 0);
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
            Assert.InRange(
                geometry.SelectionTop - (bounds.Y + bounds.Height),
                BrowserTestConstants.Editor.FloatingBarMinGapAboveSelectionPx,
                double.MaxValue);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_FloatingToolbarStaysPinnedAfterFloatingFormatAction()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await GotoEditorAndWaitForSourceAsync(page);
            await EditorMonacoDriver.SetCaretAtTextStartAsync(page, BrowserTestConstants.Editor.ToolbarPinnedSelectionTarget);

            await page.Keyboard.DownAsync(BrowserTestConstants.Keyboard.Shift);

            try
            {
                for (var index = 0; index < BrowserTestConstants.Editor.ToolbarPinnedSelectionCharacterCount; index++)
                {
                    await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.ArrowRight);
                }
            }
            finally
            {
                await page.Keyboard.UpAsync(BrowserTestConstants.Keyboard.Shift);
            }

            var floatingBar = page.GetByTestId(UiTestIds.Editor.FloatingBar);
            await Expect(floatingBar).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);

            var before = await GetRequiredAnchorAsync(floatingBar);

            await page.GetByTestId(UiTestIds.Editor.FloatEmphasis).ClickAsync();
            await Expect(floatingBar).ToBeVisibleAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);

            var after = await GetRequiredAnchorAsync(floatingBar);

            Assert.InRange(
                Math.Abs(after.Left - before.Left),
                0,
                BrowserTestConstants.Editor.FloatingBarPinnedMaxDriftPx);
            Assert.InRange(
                Math.Abs(after.Top - before.Top),
                0,
                BrowserTestConstants.Editor.FloatingBarPinnedMaxDriftPx);
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
