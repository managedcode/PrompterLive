using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorMinimapLayoutTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_MonacoMinimapStaysVisibleInsideEditorStage()
    {
        await UiPageScenarioDriver.RunWithIsolatedPageRetryAsync(
            _fixture,
            async page =>
            {
                await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
                await EditorMonacoDriver.WaitUntilReadyAsync(page);

                var stage = page.GetByTestId(UiTestIds.Editor.SourceStage);
                var minimap = page.GetByTestId(UiTestIds.Editor.SourceMinimap);
                var state = await EditorMonacoDriver.GetStateAsync(page);
                var stageBounds = await GetRequiredBoundingBoxAsync(stage);
                await Expect(minimap)
                    .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

                var minimapBounds = await GetRequiredBoundingBoxAsync(minimap);
                var stageRight = stageBounds.X + stageBounds.Width;

                await Assert.That(state.Layout.MinimapWidth >= BrowserTestConstants.Editor.MinimapMinimumWidthPx).IsTrue().Because($"Expected Monaco minimap width to be at least {BrowserTestConstants.Editor.MinimapMinimumWidthPx}px, but was {state.Layout.MinimapWidth:0.##}px.");
                await Assert.That(minimapBounds.X - stageBounds.X).IsBetween(0, stageBounds.Width);
                await Assert.That(stageRight - (minimapBounds.X + minimapBounds.Width)).IsBetween(0, BrowserTestConstants.Editor.MinimapStageEdgeTolerancePx);
            },
            "Editor minimap layout coverage failed.");
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
