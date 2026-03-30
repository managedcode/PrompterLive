using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

public sealed class EditorTypingTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_RapidTypingUpdatesStructureAndPersistsAfterReload()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.ActiveSegmentName)).ToHaveCountAsync(0);
            await Expect(page.GetByTestId(UiTestIds.Editor.ActiveBlockName)).ToHaveCountAsync(0);

            await page.GetByTestId(UiTestIds.Editor.SourceInput).ClickAsync();
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.SelectAll);
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Backspace);
            await page.Keyboard.TypeAsync(BrowserTestConstants.Editor.TypedScript);

            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(BrowserTestConstants.Editor.TypedScript);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync(BrowserTestConstants.Editor.TypedTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync(BrowserTestConstants.Editor.TypedBlock);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight))
                .ToContainTextAsync(BrowserTestConstants.Editor.TypedHighlight);

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.PersistDelayMs);
            await page.ReloadAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(BrowserTestConstants.Editor.TypedScript);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync(BrowserTestConstants.Editor.TypedTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync(BrowserTestConstants.Editor.TypedBlock);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_KeepsStyledOverlayVisibleAndPreservesClickCaretPlacement()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Editor.SourceInput).ClickAsync(new()
            {
                Position = new()
                {
                    X = BrowserTestConstants.Editor.ClickNearStartOffsetX,
                    Y = BrowserTestConstants.Editor.ClickNearStartOffsetY
                }
            });

            var editorSurfaceState = await page.EvaluateAsync<EditorSurfaceState>(
                """
                () => {
                    const input = document.querySelector('[data-testid="editor-source-input"]');
                    const highlight = document.querySelector('[data-testid="editor-source-highlight"]');
                    const inputStyle = input ? getComputedStyle(input) : null;
                    const highlightStyle = highlight ? getComputedStyle(highlight) : null;
                    return {
                        selectionStart: input ? input.selectionStart ?? -1 : -1,
                        inputColor: inputStyle ? inputStyle.color : '',
                        highlightOpacity: highlightStyle ? highlightStyle.opacity : ''
                    };
                }
                """);

            Assert.NotNull(editorSurfaceState);
            Assert.InRange(editorSurfaceState!.SelectionStart, 0, BrowserTestConstants.Editor.ClickCaretThreshold);
            Assert.Equal(BrowserTestConstants.Editor.TransparentInputColor, editorSurfaceState.InputColor);
            Assert.Equal(BrowserTestConstants.Editor.VisibleOverlayOpacity, editorSurfaceState.HighlightOpacity);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_SequentialTypingIntoSourceInputCompletesWithoutTimeout()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            var sourceInput = page.GetByTestId(UiTestIds.Editor.SourceInput);

            await Expect(sourceInput)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            await sourceInput.ClickAsync();
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.SelectAll);
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Backspace);
            await sourceInput.PressSequentiallyAsync(BrowserTestConstants.Editor.TypedScript);

            await Expect(sourceInput).ToHaveValueAsync(BrowserTestConstants.Editor.TypedScript);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0)))
                .ToContainTextAsync(BrowserTestConstants.Editor.TypedTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0)))
                .ToContainTextAsync(BrowserTestConstants.Editor.TypedBlock);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_NewDraftDoesNotReplaceRouteWhileTypingIsStillSettling()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Editor);
            var sourceInput = page.GetByTestId(UiTestIds.Editor.SourceInput);

            await Expect(sourceInput)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            await sourceInput.ClickAsync();
            await page.Keyboard.TypeAsync(BrowserTestConstants.Editor.FirstProbeCharacter);
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.NewDraftPersistGraceDelayMs);

            var graceUri = new Uri(page.Url);
            Assert.Equal(BrowserTestConstants.Routes.Editor, graceUri.AbsolutePath);
            Assert.True(string.IsNullOrEmpty(graceUri.Query));

            await page.Keyboard.TypeAsync(BrowserTestConstants.Editor.SecondProbeCharacter);
            await Expect(sourceInput).ToHaveValueAsync(BrowserTestConstants.Editor.NewDraftProbeText);

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.NewDraftPersistSettleDelayMs);

            var currentUri = new Uri(page.Url);
            Assert.Equal(BrowserTestConstants.Routes.Editor, currentUri.AbsolutePath);
            Assert.True(currentUri.Query.Contains(AppRoutes.ScriptIdQueryKey, StringComparison.Ordinal));
            await Expect(sourceInput).ToHaveValueAsync(BrowserTestConstants.Editor.NewDraftProbeText);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private sealed class EditorSurfaceState
    {
        public string HighlightOpacity { get; set; } = string.Empty;

        public string InputColor { get; set; } = string.Empty;

        public int SelectionStart { get; set; }
    }
}
