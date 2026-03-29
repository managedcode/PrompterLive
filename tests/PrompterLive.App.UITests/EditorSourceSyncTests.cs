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
            await page.GotoAsync("/editor?id=quantum-computing");
            await Expect(page.GetByTestId("editor-source-input"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            await page.GetByTestId("editor-source-input").EvaluateAsync(
                """
                element => {
                    const nextValue = element.value
                        .replace(/^## \[[^\n]+\]/m, "## [Launch Angle|305WPM|focused|1:00-2:00]")
                        .replace(/^### \[[^\n]+\]/m, "### [Signal Block|305WPM|professional]");

                    element.focus();
                    element.value = nextValue;
                    element.setSelectionRange(0, 0);
                    element.dispatchEvent(new Event("input", { bubbles: true }));
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """);

            await Expect(page.Locator("[data-nav='seg-0']")).ToContainTextAsync("Launch Angle");
            await Expect(page.Locator("[data-nav='seg-0']")).ToContainTextAsync("Focused");
            await Expect(page.Locator("[data-nav='blk-0-0']")).ToContainTextAsync("Signal Block");
            await Expect(page.Locator("[data-nav='blk-0-0']")).ToContainTextAsync("305WPM");
            await Expect(page.GetByTestId("editor-source-highlight"))
                .ToContainTextAsync("## [Launch Angle|305WPM|focused|1:00-2:00]");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
