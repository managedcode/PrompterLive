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

    [Fact]
    public async Task EditorScreen_PlainTypingKeepsVisibleOverlayResponsive()
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

            await page.EvaluateAsync(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-testid="${args.overlayTestId}"]`);
                    const stage = document.querySelector(`[data-testid="${args.stageTestId}"]`);
                    if (!input || !overlay || !stage) {
                        throw new Error("Unable to attach the editor typing probe.");
                    }

                    const samples = [];
                    const longTasks = [];
                    const observer = new PerformanceObserver(list => {
                        for (const entry of list.getEntries()) {
                            longTasks.push(entry.duration);
                        }
                    });
                    observer.observe({ type: "longtask" });

                    input.addEventListener("input", () => {
                        const started = performance.now();
                        requestAnimationFrame(() => {
                            samples.push({
                                latency: performance.now() - started,
                                typingModeActive: stage.classList.contains(args.typingStateClass),
                                inputVisible: getComputedStyle(input).color !== args.transparentInputColor
                            });
                        });
                    }, { passive: true });

                    window.__editorTypingProbe = {
                        observer,
                        samples,
                        longTasks,
                        stage,
                        input,
                        overlay
                    };
                }
                """,
                new
                {
                    inputTestId = UiTestIds.Editor.SourceInput,
                    overlayTestId = UiTestIds.Editor.SourceHighlight,
                    stageTestId = UiTestIds.Editor.SourceStage,
                    transparentInputColor = BrowserTestConstants.Editor.TransparentInputColor,
                    typingStateClass = BrowserTestConstants.Editor.SourceStageTypingClass
                });

            await page.Keyboard.TypeAsync(
                BrowserTestConstants.Editor.TypingResponsivenessProbeText,
                new() { Delay = 0 });

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.TypingProbeSettleDelayMs);
            await Expect(sourceInput).ToHaveValueAsync(BrowserTestConstants.Editor.TypingResponsivenessProbeText);

            var probeResult = await page.EvaluateAsync<TypingProbeResult>(
                """
                (args) => {
                    const probe = window.__editorTypingProbe;
                    if (!probe) {
                        throw new Error("Editor typing probe data is missing.");
                    }

                    probe.observer.disconnect();
                    return {
                        sampleCount: probe.samples.length,
                        maxLatency: probe.samples.length ? Math.max(...probe.samples.map(sample => sample.latency)) : -1,
                        longTaskCount: probe.longTasks.length,
                        maxLongTaskDuration: probe.longTasks.length ? Math.max(...probe.longTasks) : 0,
                        sawTypingMode: probe.samples.some(sample => sample.typingModeActive),
                        sawVisibleInput: probe.samples.some(sample => sample.inputVisible),
                        finalInputColor: getComputedStyle(probe.input).color,
                        finalOverlayOpacity: getComputedStyle(probe.overlay).opacity,
                        finalRenderedLength: Number.parseInt(
                            probe.overlay.dataset.renderedLength ?? '-1',
                            10),
                        finalTypingModeActive: probe.stage.classList.contains(args.typingStateClass)
                    };
                }
                """,
                new
                {
                    typingStateClass = BrowserTestConstants.Editor.SourceStageTypingClass
                });

            Assert.True(probeResult.SampleCount > 0);
            Assert.True(probeResult.SawTypingMode);
            Assert.True(probeResult.SawVisibleInput);
            Assert.InRange(
                probeResult.MaxLatency,
                0,
                BrowserTestConstants.Editor.MaxVisibleRenderLatencyMs);
            Assert.InRange(
                probeResult.LongTaskCount,
                0,
                BrowserTestConstants.Editor.MaxTypingLongTaskCount);
            Assert.False(probeResult.FinalTypingModeActive);
            Assert.Equal(BrowserTestConstants.Editor.TransparentInputColor, probeResult.FinalInputColor);
            Assert.Equal(BrowserTestConstants.Editor.VisibleOverlayOpacity, probeResult.FinalOverlayOpacity);
            Assert.Equal(BrowserTestConstants.Editor.TypingResponsivenessProbeText.Length, probeResult.FinalRenderedLength);
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

    private sealed class TypingProbeResult
    {
        public string FinalInputColor { get; set; } = string.Empty;

        public string FinalOverlayOpacity { get; set; } = string.Empty;

        public int FinalRenderedLength { get; set; }

        public bool FinalTypingModeActive { get; set; }

        public int LongTaskCount { get; set; }

        public double MaxLatency { get; set; }

        public double MaxLongTaskDuration { get; set; }

        public bool SawTypingMode { get; set; }

        public bool SawVisibleInput { get; set; }

        public int SampleCount { get; set; }
    }
}
