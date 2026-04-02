using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class EditorFloatingToolbarLayoutTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string SegmentLineSelector = ".ed-src-line-segment";
    private readonly record struct LayoutBounds(double Y, double Height);
    private readonly record struct ToolbarAnchor(double Left, double Top);
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_FloatingToolbarKeepsFullHeightWhenSelectionIsActive()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            var sourceInput = await GotoEditorAndWaitForSourceAsync(page);

            await sourceInput.EvaluateAsync(
                """
                (element, args) => {
                    element.focus();
                    element.value = args.text;
                    element.dispatchEvent(new Event("input", { bubbles: true }));

                    const start = element.value.indexOf(args.target);
                    element.setSelectionRange(start, start + args.target.length);
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """,
                new
                {
                    text = BrowserTestConstants.Editor.TypedScript,
                    target = BrowserTestConstants.Editor.TypedSelectionTarget
                });

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
            var sourceInput = await GotoEditorAndWaitForSourceAsync(page);

            await sourceInput.EvaluateAsync(
                """
                (element, args) => {
                    element.focus();
                    element.value = args.text;
                    element.dispatchEvent(new Event("input", { bubbles: true }));

                    const start = element.value.indexOf(args.startToken);
                    const endTokenStart = element.value.indexOf(args.endToken, start);
                    const end = endTokenStart + args.endToken.length;
                    element.setSelectionRange(start, end);
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """,
                new
                {
                    text = BrowserTestConstants.Editor.TypedScript,
                    startToken = BrowserTestConstants.Editor.TypedMultilineSelectionStart,
                    endToken = BrowserTestConstants.Editor.TypedMultilineSelectionEnd
                });

            var geometry = await page.GetByTestId(UiTestIds.Editor.SourceHighlight).EvaluateAsync<SelectionGeometry>(
                """
                (element, selector) => {
                    const firstSegmentLine = element.querySelector(selector);
                    if (!firstSegmentLine) {
                        throw new Error("Unable to locate the first rendered segment line.");
                    }

                    const rect = firstSegmentLine.getBoundingClientRect();
                    return {
                        selectionTop: rect.top
                    };
                }
                """,
                SegmentLineSelector);

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
            var sourceInput = await GotoEditorAndWaitForSourceAsync(page);

            await sourceInput.EvaluateAsync(
                """
                (element, target) => {
                    element.focus();
                    const start = element.value.indexOf(target);
                    element.setSelectionRange(start, start);
                }
                """,
                BrowserTestConstants.Editor.ToolbarPinnedSelectionTarget);

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

    private static async Task<ILocator> GotoEditorAndWaitForSourceAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);

        var editorPage = page.GetByTestId(UiTestIds.Editor.Page);
        await Expect(editorPage).ToBeVisibleAsync(new()
        {
            Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
        });

        var sourceInput = page.GetByTestId(UiTestIds.Editor.SourceInput);
        await Expect(sourceInput).ToBeVisibleAsync(new()
        {
            Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
        });

        return sourceInput;
    }

    private readonly record struct SelectionGeometry(double SelectionTop);
}
