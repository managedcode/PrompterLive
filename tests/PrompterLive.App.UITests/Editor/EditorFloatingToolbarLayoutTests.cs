using Microsoft.Playwright;
using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

public sealed class EditorFloatingToolbarLayoutTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string SegmentLineSelector = ".ed-src-line-segment";
    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_FloatingToolbarKeepsFullHeightWhenSelectionIsActive()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
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
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
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
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            var sourceInput = page.GetByTestId(UiTestIds.Editor.SourceInput);
            await Expect(sourceInput).ToBeVisibleAsync();

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

            var before = await GetRequiredBoundingBoxAsync(floatingBar);

            await page.GetByTestId(UiTestIds.Editor.FloatEmphasis).ClickAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.FloatingToolbarSettleDelayMs);

            var after = await GetRequiredBoundingBoxAsync(floatingBar);

            Assert.InRange(
                Math.Abs(after.X - before.X),
                0,
                BrowserTestConstants.Editor.FloatingBarPinnedMaxDriftPx);
            Assert.InRange(
                Math.Abs(after.Y - before.Y),
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
                    x: rect.x,
                    y: rect.y,
                    width: rect.width,
                    height: rect.height
                };
            }
            """);

    private readonly record struct SelectionGeometry(double SelectionTop);
}
