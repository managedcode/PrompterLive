using System.Text.RegularExpressions;
using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class EditorInteractionTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_ShowsFloatingBarAndAppliesFormattingToSelectedSourceText()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
                """
                element => {
                    const text = element.value;
                    const target = "welcome";
                    const start = text.indexOf(target);
                    element.focus();
                    element.setSelectionRange(start, start + target.length);
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FloatEmphasis).ClickAsync();

            var value = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();
            Assert.Contains(BrowserTestConstants.Editor.EmphasisFragment, value, StringComparison.Ordinal);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight)).ToContainTextAsync(BrowserTestConstants.Editor.EmphasisFragment);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_UndoAndRedoWorkFromToolbarAndKeyboard()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToBeVisibleAsync();

            var initialValue = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
                """
                element => {
                    const addition = "\n[pause:2s]";
                    element.focus();
                    element.setSelectionRange(element.value.length, element.value.length);
                    element.value = element.value + addition;
                    element.dispatchEvent(new Event("input", { bubbles: true }));
                }
                """);

            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(BrowserTestConstants.Regexes.EndsWithPause);

            await page.GetByTestId(UiTestIds.Editor.Undo).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(initialValue);

            await page.GetByTestId(UiTestIds.Editor.Redo).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(BrowserTestConstants.Regexes.EndsWithPause);

            await page.GetByTestId(UiTestIds.Editor.SourceInput).ClickAsync();
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Undo);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(initialValue);

            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Redo);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(BrowserTestConstants.Regexes.EndsWithPause);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_FloatingToolbarShowsAiAndPersistsSelectionFormatting()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
                """
                element => {
                    const text = element.value;
                    const target = "transformative moment";
                    const start = text.indexOf(target);
                    element.focus();
                    element.setSelectionRange(start, start + target.length);
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingAi)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.FloatingSlow).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.SlowFragment)));

            await page.ReloadAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.SlowFragment)));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_DoesNotRenderLegacyStructureInspectorPanel()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.ActiveSegmentName)).ToHaveCountAsync(0);
            await Expect(page.GetByTestId(UiTestIds.Editor.ActiveBlockName)).ToHaveCountAsync(0);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync("Introduction");
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync("Overview Block");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_HidesFrontMatterFromVisibleEditorBody()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToBeVisibleAsync();

            var visibleSource = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();
            Assert.DoesNotContain("---", visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain("title:", visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain("author:", visibleSource, StringComparison.Ordinal);
            Assert.Contains("## [Intro|140WPM|warm]", visibleSource, StringComparison.Ordinal);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_ClickableMenusAndAiPanelApplyCommands()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
                """
                element => {
                    const text = element.value;
                    const target = "welcome";
                    const start = text.indexOf(target);
                    element.focus();
                    element.setSelectionRange(start, start + target.length);
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """);

            await page.GetByTestId(UiTestIds.Editor.FormatTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuFormat)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.FormatHighlight).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.HighlightFragment)));

            await page.GetByTestId(UiTestIds.Editor.PauseTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuPause)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.PauseTwoSeconds).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(new Regex(Regex.Escape(BrowserTestConstants.Editor.PauseFragment)));

            await page.GetByTestId(UiTestIds.Editor.InsertTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuInsert)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.InsertBlock).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(
                new Regex(@"### \[Block Name\|140WPM\]"));

            var valueBeforeAi = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();
            await page.GetByTestId(UiTestIds.Editor.Ai).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.AiPanel)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.AiActionSimplify).ClickAsync();

            var valueAfterAi = await page.GetByTestId(UiTestIds.Editor.SourceInput).InputValueAsync();
            Assert.NotEqual(valueBeforeAi, valueAfterAi);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_FullToolbarSurfaceSupportsExtendedCommands()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
                """
                element => {
                    const text = element.value;
                    const target = "welcome";
                    const start = text.indexOf(target);
                    element.focus();
                    element.setSelectionRange(start, start + target.length);
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """);

            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuColor)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.ColorGreen).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.GreenFragment)));

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
                """
                element => {
                    const text = element.value;
                    const target = "[green]welcome[/green]";
                    const start = text.indexOf(target);
                    element.focus();
                    element.setSelectionRange(start, start + target.length);
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """);

            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();
            await page.GetByTestId(UiTestIds.Editor.ColorClear).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).Not.ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.GreenFragment)));

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
                """
                element => {
                    const text = element.value;
                    const target = "transformative moment";
                    const start = text.indexOf(target);
                    element.focus();
                    element.setSelectionRange(start, start + target.length);
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                    element.dispatchEvent(new Event("keyup", { bubbles: true }));
                }
                """);

            await page.GetByTestId(UiTestIds.Editor.EmotionTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuEmotion)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.EmotionProfessional).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.ProfessionalFragment)));

            await page.GetByTestId(UiTestIds.Editor.SpeedTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuSpeed)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.SpeedCustomWpm).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.CustomWpmToken)));

            await page.GetByTestId(UiTestIds.Editor.InsertTrigger).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.MenuInsert)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Editor.InsertPronunciation).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(
                new Regex(Regex.Escape(BrowserTestConstants.Editor.PronunciationToken)));
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
