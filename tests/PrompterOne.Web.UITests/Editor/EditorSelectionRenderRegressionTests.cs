using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[Collection(EditorAuthoringCollection.Name)]
public sealed class EditorSelectionRenderRegressionTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_SetTextThenSelectStillRendersFloatingBar()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await EditorMonacoDriver.SetTextAsync(page, BrowserTestConstants.Editor.TypedScript);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.TypedSelectionTarget);

            await Expect(page.GetByTestId(UiTestIds.Editor.FloatingBar))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_BackwardSelection_KeepsGrowingAcrossRepeatedArrowLeftInput()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await EditorMonacoDriver.SetTextAsync(page, BrowserTestConstants.Editor.TypedScript);
            await EditorMonacoDriver.SetCaretAtTextEndAsync(page, BrowserTestConstants.Editor.ReverseSelectionTarget);

            await page.Keyboard.DownAsync(BrowserTestConstants.Keyboard.Shift);

            try
            {
                for (var index = 0; index < BrowserTestConstants.Editor.ReverseSelectionCharacterCount; index++)
                {
                    await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.ArrowLeft);
                }
            }
            finally
            {
                await page.Keyboard.UpAsync(BrowserTestConstants.Keyboard.Shift);
            }

            var state = await EditorMonacoDriver.GetStateAsync(page);
            var selectedText = ReadSelectedText(state);

            Assert.Equal(BrowserTestConstants.Editor.ReverseSelectionExpectedText, selectedText);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_BackwardSelection_CanExtendAcrossLineBreaks()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await EditorMonacoDriver.SetTextAsync(page, BrowserTestConstants.Editor.TypedMultilineScript);
            await EditorMonacoDriver.SetCaretAtTextEndAsync(page, BrowserTestConstants.Editor.ReverseMultilineSelectionTarget);

            await page.Keyboard.DownAsync(BrowserTestConstants.Keyboard.Shift);

            try
            {
                for (var index = 0; index < BrowserTestConstants.Editor.ReverseMultilineSelectionCharacterCount; index++)
                {
                    await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.ArrowLeft);
                }
            }
            finally
            {
                await page.Keyboard.UpAsync(BrowserTestConstants.Keyboard.Shift);
            }

            var state = await EditorMonacoDriver.GetStateAsync(page);
            var selectedText = ReadSelectedText(state);

            Assert.True(
                selectedText.Length >= BrowserTestConstants.Editor.ReverseMultilineSelectionCharacterCount,
                $"Expected backward selection to keep growing across lines, but only selected {selectedText.Length} characters.");
            Assert.Contains(BrowserTestConstants.Editor.LineFeed, selectedText, StringComparison.Ordinal);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static string ReadSelectedText(EditorMonacoState state)
    {
        var start = Math.Min(state.Selection.Start, state.Selection.End);
        var end = Math.Max(state.Selection.Start, state.Selection.End);
        return state.Text[start..end];
    }
}
