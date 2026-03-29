using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class EditorSourceSyncTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_DirectSourceHeaderEditsRefreshStructureTree()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
                """
                (element, values) => {
                    const nextValue = element.value
                        .replace(/^## \[[^\n]+\]/m, values.segmentHeader)
                        .replace(/^### \[[^\n]+\]/m, values.blockHeader);

                    element.focus();
                    element.value = nextValue;
                    element.setSelectionRange(0, 0);
                    element.dispatchEvent(new Event("input", { bubbles: true }));
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """,
                new
                {
                    segmentHeader = BrowserTestConstants.Editor.SegmentRewrite,
                    blockHeader = BrowserTestConstants.Editor.BlockRewrite
                });

            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync("Launch Angle");
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync("Focused");
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync("Signal Block");
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync("305WPM");
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight))
                .ToContainTextAsync(BrowserTestConstants.Editor.SegmentRewrite);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
