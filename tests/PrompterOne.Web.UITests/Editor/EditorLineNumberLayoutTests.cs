using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

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
            await Assert.That(gutterBounds.Width).IsBetween(BrowserTestConstants.Editor.GutterMinimumWidthPx,BrowserTestConstants.Editor.GutterMaximumWidthPx);
            await Assert.That(gutterBounds.X - stageBounds.X).IsBetween(0,stageBounds.Width);
            await Assert.That(state.Layout.ContentLeft >= BrowserTestConstants.Editor.MinimumContentLeftWithLineNumbersPx).IsTrue().Because($"Expected Monaco contentLeft to include the line-number gutter, but it was {state.Layout.ContentLeft:0.##}.");

            var lineNumberGap = await GetLineNumberTextGapAsync(stage);
            await Assert.That(lineNumberGap).IsBetween(BrowserTestConstants.Editor.MinimumLineNumberTextGapPx,BrowserTestConstants.Editor.MaximumLineNumberTextGapPx);
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

    private static async Task<double> GetLineNumberTextGapAsync(ILocator stage) =>
        await stage.EvaluateAsync<double>(
            """
            element => {
                const firstLineNumber = element.querySelector('.margin-view-overlays .line-numbers');
                const firstViewLine = Array.from(element.querySelectorAll('.view-lines .view-line'))
                    .find(line => (line.textContent ?? '').trim().length > 0);

                if (!(firstLineNumber instanceof HTMLElement) || !(firstViewLine instanceof HTMLElement)) {
                    return -1;
                }

                const numberRange = document.createRange();
                numberRange.selectNodeContents(firstLineNumber);
                const numberRect = numberRange.getBoundingClientRect();
                const lineRect = firstViewLine.getBoundingClientRect();
                return lineRect.left - numberRect.right;
            }
            """);

    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);
}