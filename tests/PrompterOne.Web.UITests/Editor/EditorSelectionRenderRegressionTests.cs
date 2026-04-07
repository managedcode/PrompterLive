using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
[NotInParallel(UiTestParallelization.EditorAuthoringConstraintKey)]
public sealed class EditorSelectionRenderRegressionTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
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

    [Test]
    public async Task EditorScreen_BackwardSelection_SelectsExpectedTrailingCharactersFromWordEnd()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await EditorMonacoDriver.SetTextAsync(page, BrowserTestConstants.Editor.TypedScript);
            await EditorMonacoDriver.SetBackwardSelectionFromTextEndAsync(
                page,
                BrowserTestConstants.Editor.ReverseSelectionTarget,
                BrowserTestConstants.Editor.ReverseSelectionCharacterCount);

            var state = await EditorMonacoDriver.GetStateAsync(page);
            var selectedText = ReadSelectedText(state);

            await Assert.That(selectedText).IsEqualTo(BrowserTestConstants.Editor.ReverseSelectionExpectedText);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_BackwardSelection_CanExtendAcrossLineBreaks()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await EditorMonacoDriver.SetTextAsync(page, BrowserTestConstants.Editor.TypedMultilineScript);
            await EditorMonacoDriver.SetBackwardSelectionFromTextEndAsync(
                page,
                BrowserTestConstants.Editor.ReverseMultilineSelectionTarget,
                BrowserTestConstants.Editor.ReverseMultilineSelectionCharacterCount);

            var state = await EditorMonacoDriver.GetStateAsync(page);
            var selectedText = ReadSelectedText(state);

            await Assert.That(selectedText.Length >= BrowserTestConstants.Editor.ReverseMultilineSelectionCharacterCount).IsTrue().Because($"Expected backward selection to keep growing across lines, but only selected {selectedText.Length} characters.");
            await Assert.That(selectedText).Contains(BrowserTestConstants.Editor.LineFeed);
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
