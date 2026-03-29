using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class EditorTypingTests(StandaloneAppFixture fixture)
{
    private const int TypingDelayMs = 8;
    private const string TypedScript = """
        ## [Typed Intro|175WPM|focused|0:05-0:20]
        ### [Typed Block|165WPM|professional]
        This is a typed TPS script. / [highlight]Every word[/highlight] stays in sync. //
        """;

    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_TypingScriptTextUpdatesStructureAndPersistsAfterReload()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/editor?id=quantum-computing");
            await Expect(page.GetByTestId("editor-source-input"))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });

            await page.GetByTestId("editor-source-input").ClickAsync();
            await page.Keyboard.PressAsync("Meta+A");
            await page.Keyboard.PressAsync("Backspace");
            await page.Keyboard.TypeAsync(TypedScript, new() { Delay = TypingDelayMs });

            await Expect(page.GetByTestId("editor-source-input")).ToHaveValueAsync(TypedScript);
            await Expect(page.GetByTestId("editor-active-segment-name")).ToHaveValueAsync("Typed Intro");
            await Expect(page.GetByTestId("editor-active-segment-wpm")).ToHaveValueAsync("175");
            await Expect(page.GetByTestId("editor-active-segment-emotion")).ToHaveValueAsync("Focused");
            await Expect(page.GetByTestId("editor-active-segment-timing")).ToHaveValueAsync("0:05-0:20");
            await Expect(page.GetByTestId("editor-active-block-name")).ToHaveValueAsync("Typed Block");
            await Expect(page.GetByTestId("editor-active-block-wpm")).ToHaveValueAsync("165");
            await Expect(page.GetByTestId("editor-active-block-emotion")).ToHaveValueAsync("Professional");
            await Expect(page.GetByTestId("editor-source-highlight"))
                .ToContainTextAsync("[highlight]Every word[/highlight]");

            await page.ReloadAsync();
            await Expect(page.GetByTestId("editor-source-input")).ToHaveValueAsync(TypedScript);
            await Expect(page.GetByTestId("editor-active-segment-name")).ToHaveValueAsync("Typed Intro");
            await Expect(page.GetByTestId("editor-active-block-name")).ToHaveValueAsync("Typed Block");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
