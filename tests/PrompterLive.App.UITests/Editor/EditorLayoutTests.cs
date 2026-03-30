using Microsoft.Playwright;
using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

public sealed class EditorLayoutTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_MetadataRailStaysDockedToRightOfMainPanel()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);

            var mainPanel = page.GetByTestId(UiTestIds.Editor.MainPanel);
            var metadataRail = page.GetByTestId(UiTestIds.Editor.MetadataRail);

            await Expect(mainPanel).ToBeVisibleAsync();
            await Expect(metadataRail).ToBeVisibleAsync();

            var mainBounds = await GetRequiredBoundingBoxAsync(mainPanel);
            var railBounds = await GetRequiredBoundingBoxAsync(metadataRail);
            var dockGap = railBounds.X - (mainBounds.X + mainBounds.Width);
            var bottomEdgeDrift = Math.Abs((railBounds.Y + railBounds.Height) - (mainBounds.Y + mainBounds.Height));

            Assert.InRange(
                Math.Abs(dockGap - BrowserTestConstants.Editor.MetadataRailDockGapPx),
                0,
                BrowserTestConstants.Editor.MetadataRailDockTolerancePx);
            Assert.InRange(
                Math.Abs(railBounds.Y - mainBounds.Y),
                0,
                BrowserTestConstants.Editor.MetadataRailDockTolerancePx);
            Assert.InRange(
                bottomEdgeDrift,
                0,
                BrowserTestConstants.Editor.MetadataRailDockTolerancePx);
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
