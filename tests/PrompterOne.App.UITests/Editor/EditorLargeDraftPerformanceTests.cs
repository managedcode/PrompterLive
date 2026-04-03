using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

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
            var sourceInput = page.GetByTestId(UiTestIds.Editor.SourceInput);

            await page.GotoAsync(BrowserTestConstants.Routes.Editor);
            await Expect(sourceInput)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await sourceInput.ClickAsync();

            await page.EvaluateAsync(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-testid="${args.overlayTestId}"]`);
                    if (!input || !overlay) {
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

                    input.addEventListener("input", () => {
                        const started = performance.now();
                        requestAnimationFrame(() => {
                            samples.push({
                                latency: performance.now() - started,
                                renderedLength: Number.parseInt(overlay.dataset.renderedLength ?? "-1", 10)
                            });
                        });
                    }, { passive: true });

                    input.value = args.draftText;
                    input.dispatchEvent(new Event("input", { bubbles: true }));
                    input.focus();
                    input.setSelectionRange(input.value.length, input.value.length);

                    window.__editorLargeDraftProbe = { longTasks, observer, samples };
                }
                """,
                new
                {
                    inputTestId = UiTestIds.Editor.SourceInput,
                    overlayTestId = UiTestIds.Editor.SourceHighlight,
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
            Assert.InRange(
                result.PasteMaxLongTaskMs,
                0,
                EditorLargeDraftPerformanceTestData.MaxPasteLongTaskMs);
            Assert.InRange(
                result.TypingLatencyMs,
                0,
                EditorLargeDraftPerformanceTestData.MaxTypingLatencyMs);
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
            var sourceInput = page.GetByTestId(UiTestIds.Editor.SourceInput);
            var targetSegment = page.GetByTestId(UiTestIds.Editor.SegmentNavigation(targetSegmentNumber - 1));

            await page.GotoAsync(BrowserTestConstants.Routes.EditorLargeDraft);
            await Expect(sourceInput)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            await Expect(targetSegment).ToContainTextAsync(targetSegmentLabel);
            await targetSegment.ScrollIntoViewIfNeededAsync();
            await targetSegment.ClickAsync();

            var result = await page.EvaluateAsync<LargeDraftNavigationResult>(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-testid="${args.overlayTestId}"]`);
                    const scrollHost = document.querySelector(`[data-testid="${args.scrollHostTestId}"]`);
                    if (!input || !overlay || !scrollHost) {
                        throw new Error("Large draft navigation probe result is unavailable.");
                    }

                    const targetLine = Array
                        .from(overlay.children)
                        .find(node => (node.textContent ?? '').includes(args.targetHeader));

                    const hostRect = scrollHost.getBoundingClientRect();
                    const targetRect = targetLine?.getBoundingClientRect();

                    return {
                        selectionStart: input.selectionStart ?? -1,
                        expectedStart: input.value.indexOf(args.targetHeader),
                        scrollTop: input.scrollTop,
                        targetVisible: Boolean(targetRect) &&
                            targetRect.top >= hostRect.top &&
                            targetRect.bottom <= hostRect.bottom
                    };
                }
                """,
                new
                {
                    inputTestId = UiTestIds.Editor.SourceInput,
                    overlayTestId = UiTestIds.Editor.SourceHighlight,
                    scrollHostTestId = UiTestIds.Editor.SourceScrollHost,
                    targetHeader = targetSegmentHeader
                });

            Assert.Equal(result.ExpectedStart, result.SelectionStart);
            Assert.True(result.ScrollTop > 0);
            Assert.True(result.TargetVisible);
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

        public double ScrollTop { get; set; }

        public bool TargetVisible { get; set; }
    }
}
