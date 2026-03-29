using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class EditorTypingTests(StandaloneAppFixture fixture)
{
    private const string TypedScript = """
        ## [Typed Intro|175WPM|focused|0:05-0:20]
        ### [Typed Block|165WPM|professional]
        This is a typed TPS script. / [highlight]Every word[/highlight] stays in sync. //
        """;

    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_RapidTypingUpdatesStructureAndPersistsAfterReload()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/editor?id=quantum-computing");
            await Expect(page.GetByTestId("editor-source-input"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Expect(page.GetByText("ACTIVE SEGMENT")).ToHaveCountAsync(0);
            await Expect(page.GetByText("ACTIVE BLOCK")).ToHaveCountAsync(0);

            await page.GetByTestId("editor-source-input").ClickAsync();
            await page.Keyboard.PressAsync("Meta+A");
            await page.Keyboard.PressAsync("Backspace");
            await page.Keyboard.TypeAsync(TypedScript);

            await Expect(page.GetByTestId("editor-source-input")).ToHaveValueAsync(TypedScript);
            await Expect(page.Locator("[data-nav='seg-0']")).ToContainTextAsync("Typed Intro");
            await Expect(page.Locator("[data-nav='blk-0-0']")).ToContainTextAsync("Typed Block");
            await Expect(page.GetByTestId("editor-source-highlight"))
                .ToContainTextAsync("[highlight]Every word[/highlight]");

            await page.WaitForTimeoutAsync(800);
            await page.ReloadAsync();
            await Expect(page.GetByTestId("editor-source-input")).ToHaveValueAsync(TypedScript);
            await Expect(page.Locator("[data-nav='seg-0']")).ToContainTextAsync("Typed Intro");
            await Expect(page.Locator("[data-nav='blk-0-0']")).ToContainTextAsync("Typed Block");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
