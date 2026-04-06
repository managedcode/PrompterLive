using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services.Editor;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

public sealed class EditorLargeDraftPerformanceTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_LargeDraftPasteKeepsFollowupTypingResponsive()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            var draft = EditorLargeDraftPerformanceTestData.BuildLargeDraft();
            var expectedLength =
                EditorLargeDraftPerformanceTestData.GetVisibleDraftLength(draft) +
                EditorLargeDraftPerformanceTestData.FollowupTypingText.Length;

            await page.GotoAsync(BrowserTestConstants.Routes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await page.EvaluateAsync(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-testid="${args.overlayTestId}"]`);
                    const harness = window[args.harnessGlobalName];
                    if (!input || !overlay || !harness) {
                        throw new Error("Unable to initialize the large draft performance probe.");
                    }

                    const samples = [];
                    const longTasks = [];
                    const observer = new PerformanceObserver(list => {
                        for (const entry of list.getEntries()) {
                            longTasks.push(entry.duration);
                        }
                    });
                    observer.observe({ type: "longtask" });

                    input.addEventListener(args.proxyChangedEventName, () => {
                        const started = performance.now();
                        requestAnimationFrame(() => {
                            samples.push({
                                latency: performance.now() - started,
                                renderedLength: Number.parseInt(overlay.dataset.renderedLength ?? "-1", 10)
                            });
                        });
                    }, { passive: true });

                    harness.setText(args.stageTestId, args.draftText);
                    harness.focus(args.stageTestId);
                    harness.setSelection(args.stageTestId, input.value.length, input.value.length, true);

                    window.__editorLargeDraftProbe = { longTasks, observer, samples };
                }
                """,
                new
                {
                    harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                    inputTestId = UiTestIds.Editor.SourceInput,
                    overlayTestId = UiTestIds.Editor.SourceHighlight,
                    proxyChangedEventName = EditorMonacoRuntimeContract.EditorProxyChangedEventName,
                    stageTestId = UiTestIds.Editor.SourceStage,
                    draftText = draft
                });

            await page.Keyboard.TypeAsync(EditorLargeDraftPerformanceTestData.FollowupTypingText, new() { Delay = 0 });
            await page.WaitForTimeoutAsync(EditorLargeDraftPerformanceTestData.ObservationDelayMs);

            var result = await page.EvaluateAsync<LargeDraftProbeResult>(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-testid="${args.overlayTestId}"]`);
                    const probe = window.__editorLargeDraftProbe;
                    if (!input || !overlay || !probe) {
                        throw new Error("Large draft performance probe result is unavailable.");
                    }

                    probe.observer.disconnect();
                    return {
                        finalInputLength: input.value.length,
                        finalRenderedLength: Number.parseInt(overlay.dataset.renderedLength ?? "-1", 10),
                        pasteMaxLongTaskMs: probe.longTasks.length ? Math.max(...probe.longTasks) : 0,
                        typingLatencyMs: probe.samples[probe.samples.length - 1]?.latency ?? -1,
                        typingSampleCount: probe.samples.length
                    };
                }
                """,
                new
                {
                    inputTestId = UiTestIds.Editor.SourceInput,
                    overlayTestId = UiTestIds.Editor.SourceHighlight
                });

            Assert.Equal(expectedLength, result.FinalInputLength);
            Assert.Equal(expectedLength, result.FinalRenderedLength);
            Assert.True(result.TypingSampleCount >= 2);
            Assert.True(
                result.PasteMaxLongTaskMs >= 0 &&
                result.PasteMaxLongTaskMs <= EditorLargeDraftPerformanceTestData.MaxPasteLongTaskMs,
                $"Large draft paste long-task budget exceeded. PasteMaxLongTaskMs: {result.PasteMaxLongTaskMs}; MaxPasteLongTaskMs: {EditorLargeDraftPerformanceTestData.MaxPasteLongTaskMs}; TypingLatencyMs: {result.TypingLatencyMs}; TypingSampleCount: {result.TypingSampleCount}; FinalInputLength: {result.FinalInputLength}; FinalRenderedLength: {result.FinalRenderedLength}.");
            Assert.True(
                result.TypingLatencyMs >= 0 &&
                result.TypingLatencyMs <= EditorLargeDraftPerformanceTestData.MaxTypingLatencyMs,
                $"Large draft typing latency exceeded the acceptance budget. TypingLatencyMs: {result.TypingLatencyMs}; MaxTypingLatencyMs: {EditorLargeDraftPerformanceTestData.MaxTypingLatencyMs}; PasteMaxLongTaskMs: {result.PasteMaxLongTaskMs}; TypingSampleCount: {result.TypingSampleCount}; FinalInputLength: {result.FinalInputLength}; FinalRenderedLength: {result.FinalRenderedLength}.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task EditorScreen_LargeDraftSegmentNavigationMovesCaretAndTargetEpisodeIntoView()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            var targetSegmentNumber = EditorLargeDraftPerformanceTestData.NavigationTargetSegmentIndex;
            var targetSegmentHeader = EditorLargeDraftPerformanceTestData.GetSegmentHeader(targetSegmentNumber);
            var targetSegmentLabel = EditorLargeDraftPerformanceTestData.GetSegmentLabel(targetSegmentNumber);
            var targetSegment = page.GetByTestId(UiTestIds.Editor.SegmentNavigation(targetSegmentNumber - 1));

            await page.GotoAsync(BrowserTestConstants.Routes.EditorLargeDraft);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await Expect(targetSegment).ToContainTextAsync(targetSegmentLabel);
            await targetSegment.ScrollIntoViewIfNeededAsync();
            await targetSegment.ClickAsync();
            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    const harness = window[args.harnessGlobalName];
                    const state = harness?.getState(args.stageTestId);
                    if (!input || !state?.visibleRange || !state?.selection) {
                        return false;
                    }

                    const expectedStart = input.value.indexOf(args.targetHeader);
                    return expectedStart >= 0 &&
                        state.selection.start === expectedStart &&
                        state.selection.line >= state.visibleRange.startLineNumber &&
                        state.selection.line <= state.visibleRange.endLineNumber;
                }
                """,
                new
                {
                    harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                    inputTestId = UiTestIds.Editor.SourceInput,
                    stageTestId = UiTestIds.Editor.SourceStage,
                    targetHeader = targetSegmentHeader
                },
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var result = await page.EvaluateAsync<LargeDraftNavigationResult>(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    const harness = window[args.harnessGlobalName];
                    if (!input || !harness) {
                        throw new Error("Large draft navigation probe result is unavailable.");
                    }

                    const state = harness.getState(args.stageTestId);

                    return {
                        selectionStart: input.selectionStart ?? -1,
                        expectedStart: input.value.indexOf(args.targetHeader),
                        scrollTop: state?.scrollTop ?? -1,
                        selectionLine: state?.selection?.line ?? -1,
                        targetVisible: Boolean(state?.visibleRange) &&
                            state.selection.line >= state.visibleRange.startLineNumber &&
                            state.selection.line <= state.visibleRange.endLineNumber,
                        visibleRangeEndLine: state?.visibleRange?.endLineNumber ?? -1,
                        visibleRangeStartLine: state?.visibleRange?.startLineNumber ?? -1
                    };
                }
                """,
                new
                {
                    harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                    inputTestId = UiTestIds.Editor.SourceInput,
                    stageTestId = UiTestIds.Editor.SourceStage,
                    targetHeader = targetSegmentHeader
                });

            Assert.Equal(result.ExpectedStart, result.SelectionStart);
            Assert.True(result.ScrollTop > 0);
            Assert.True(
                result.TargetVisible,
                $"Expected the target segment header to stay visible after navigation, but the Monaco visible range did not include the selected line. ScrollTop={result.ScrollTop}; SelectionStart={result.SelectionStart}; ExpectedStart={result.ExpectedStart}; SelectionLine={result.SelectionLine}; VisibleStart={result.VisibleRangeStartLine}; VisibleEnd={result.VisibleRangeEndLine}.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private sealed class LargeDraftProbeResult
    {
        public int FinalInputLength { get; set; }

        public int FinalRenderedLength { get; set; }

        public double PasteMaxLongTaskMs { get; set; }

        public double TypingLatencyMs { get; set; }

        public int TypingSampleCount { get; set; }
    }

    private sealed class LargeDraftNavigationResult
    {
        public int ExpectedStart { get; set; }

        public int SelectionStart { get; set; }

        public int SelectionLine { get; set; }

        public double ScrollTop { get; set; }

        public bool TargetVisible { get; set; }

        public int VisibleRangeEndLine { get; set; }

        public int VisibleRangeStartLine { get; set; }
    }
}
