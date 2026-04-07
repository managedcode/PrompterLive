using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorLineNumberLayoutTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_MonacoLineNumbersRenderInsideVisibleGutter()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.LineNumbersScenario);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            var stage = page.GetByTestId(UiTestIds.Editor.SourceStage);
            var gutter = page.GetByTestId(UiTestIds.Editor.SourceGutter);
            var state = await EditorMonacoDriver.GetStateAsync(page);

            await Expect(gutter)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await UiScenarioArtifacts.CaptureLocatorAsync(
                gutter,
                BrowserTestConstants.EditorFlow.LineNumbersScenario,
                BrowserTestConstants.EditorFlow.LineNumbersStep);

            var stageBounds = await GetRequiredBoundingBoxAsync(stage);
            var gutterBounds = await GetRequiredBoundingBoxAsync(gutter);
            var gutterText = (await gutter.TextContentAsync())?.Trim() ?? string.Empty;

            await Assert.That(gutterText).Contains(BrowserTestConstants.Editor.GutterFirstLineNumberText);
            await Assert.That(gutterBounds.Width).IsBetween(BrowserTestConstants.Editor.GutterMinimumWidthPx, BrowserTestConstants.Editor.GutterMaximumWidthPx);
            await Assert.That(gutterBounds.X - stageBounds.X).IsBetween(0, stageBounds.Width);
            await Assert.That(state.Layout.ContentLeft >= BrowserTestConstants.Editor.MinimumContentLeftWithLineNumbersPx).IsTrue().Because($"Expected Monaco contentLeft to include the line-number gutter, but it was {state.Layout.ContentLeft:0.##}.");
            var contentLeftBoundary = stageBounds.X + state.Layout.ContentLeft;
            var lineNumberTextRight = await gutter.EvaluateAsync<double>(
                """
                element => {
                    const lineNumbers = Array.from(element.querySelectorAll('.line-numbers'));
                    if (lineNumbers.length === 0) {
                        return 0;
                    }

                    return Math.max(...lineNumbers.map(node => node.getBoundingClientRect().right));
                }
                """);
            var lineNumberGap = contentLeftBoundary - lineNumberTextRight;
            await Assert.That(lineNumberGap).IsBetween(BrowserTestConstants.Editor.MinimumLineNumberTextGapPx, BrowserTestConstants.Editor.MaximumLineNumberTextGapPx);
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

    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);
}
