using PrompterOne.Shared.Contracts;

namespace PrompterOne.App.UITests;

public sealed class EditorHugeDraftPerformanceTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_HugeDraftLoadedFromSeedKeepsFollowupTypingResponsive()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            var hugeDraft = EditorLargeDraftPerformanceTestData.BuildHugeDraft();
            var expectedLength =
                EditorLargeDraftPerformanceTestData.GetVisibleDraftLength(hugeDraft) +
                EditorLargeDraftPerformanceTestData.FollowupTypingText.Length;
            var sourceInput = page.GetByTestId(UiTestIds.Editor.SourceInput);

            await page.GotoAsync(BrowserTestConstants.Routes.EditorHugeDraft);
            var editorReady = false;
            var deadline = DateTimeOffset.UtcNow.AddMilliseconds(EditorLargeDraftPerformanceTestData.HugeDraftReadyTimeoutMs);
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (await sourceInput.IsVisibleAsync())
                {
                    editorReady = true;
                    break;
                }

                await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.DiagnosticPollDelayMs);
            }

            if (!editorReady)
            {
                var debug = await page.EvaluateAsync<HugeDraftDebugInfo>(
                    """
                    (args) => ({
                        location: window.location.href,
                        diagnosticsBannerVisible: Boolean(document.querySelector(`[data-testid="${args.diagnosticsBannerTestId}"]`)),
                        diagnosticsFatalVisible: Boolean(document.querySelector(`[data-testid="${args.diagnosticsFatalTestId}"]`)),
                        routeNotFoundVisible: Boolean((document.body?.innerText ?? '').includes(args.routeNotFoundText)),
                        sourceInputCount: document.querySelectorAll(`[data-testid="${args.sourceInputTestId}"]`).length,
                        bodySnippet: (document.body?.innerText ?? '').slice(0, 500)
                    })
                    """,
                    new
                    {
                        diagnosticsBannerTestId = UiTestIds.Diagnostics.Banner,
                        diagnosticsFatalTestId = UiTestIds.Diagnostics.Fatal,
                        routeNotFoundText = "Route not found",
                        sourceInputTestId = UiTestIds.Editor.SourceInput
                    });

                Assert.True(
                    editorReady,
                    $"Huge draft editor did not appear. Url: {debug.Location}; SourceInputs: {debug.SourceInputCount}; Banner: {debug.DiagnosticsBannerVisible}; Fatal: {debug.DiagnosticsFatalVisible}; RouteNotFound: {debug.RouteNotFoundVisible}; Body: {debug.BodySnippet}");
            }

            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    return (input?.value.length ?? 0) >= args.minimumVisibleLength;
                }
                """,
                new
                {
                    inputTestId = UiTestIds.Editor.SourceInput,
                    minimumVisibleLength = EditorLargeDraftPerformanceTestData.HugeDraftMinimumLength / 2
                },
                new() { Timeout = EditorLargeDraftPerformanceTestData.HugeDraftReadyTimeoutMs });

            await page.EvaluateAsync(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-testid="${args.overlayTestId}"]`);
                    if (!input || !overlay) {
                        throw new Error("Unable to initialize the huge draft performance probe.");
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

                    input.focus();
                    input.setSelectionRange(input.value.length, input.value.length);
                    window.__editorHugeDraftProbe = { longTasks, observer, samples };
                }
                """,
                new
                {
                    inputTestId = UiTestIds.Editor.SourceInput,
                    overlayTestId = UiTestIds.Editor.SourceHighlight
                });

            await page.Keyboard.TypeAsync(EditorLargeDraftPerformanceTestData.FollowupTypingText, new() { Delay = 0 });
            await page.WaitForTimeoutAsync(EditorLargeDraftPerformanceTestData.ObservationDelayMs);

            var result = await page.EvaluateAsync<HugeDraftProbeResult>(
                """
                (args) => {
                    const input = document.querySelector(`[data-testid="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-testid="${args.overlayTestId}"]`);
                    const probe = window.__editorHugeDraftProbe;
                    if (!input || !overlay || !probe) {
                        throw new Error("Huge draft performance probe result is unavailable.");
                    }

                    probe.observer.disconnect();
                    return {
                        finalInputLength: input.value.length,
                        finalRenderedLength: Number.parseInt(overlay.dataset.renderedLength ?? "-1", 10),
                        followupMaxLongTaskMs: probe.longTasks.length ? Math.max(...probe.longTasks) : 0,
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
                result.FollowupMaxLongTaskMs >= 0 &&
                result.FollowupMaxLongTaskMs <= EditorLargeDraftPerformanceTestData.MaxHugeFollowupLongTaskMs,
                $"Huge draft follow-up long task exceeded the acceptance budget. FollowupMaxLongTaskMs: {result.FollowupMaxLongTaskMs}; MaxHugeFollowupLongTaskMs: {EditorLargeDraftPerformanceTestData.MaxHugeFollowupLongTaskMs}; TypingLatencyMs: {result.TypingLatencyMs}; TypingSampleCount: {result.TypingSampleCount}; FinalInputLength: {result.FinalInputLength}; FinalRenderedLength: {result.FinalRenderedLength}.");
            Assert.True(
                result.TypingLatencyMs >= 0 &&
                result.TypingLatencyMs <= EditorLargeDraftPerformanceTestData.MaxHugeTypingLatencyMs,
                $"Huge draft typing latency exceeded the acceptance budget. TypingLatencyMs: {result.TypingLatencyMs}; MaxHugeTypingLatencyMs: {EditorLargeDraftPerformanceTestData.MaxHugeTypingLatencyMs}; FollowupMaxLongTaskMs: {result.FollowupMaxLongTaskMs}; TypingSampleCount: {result.TypingSampleCount}; FinalInputLength: {result.FinalInputLength}; FinalRenderedLength: {result.FinalRenderedLength}.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private sealed class HugeDraftProbeResult
    {
        public int FinalInputLength { get; set; }

        public int FinalRenderedLength { get; set; }

        public double FollowupMaxLongTaskMs { get; set; }

        public double TypingLatencyMs { get; set; }

        public int TypingSampleCount { get; set; }
    }

    private sealed class HugeDraftDebugInfo
    {
        public string BodySnippet { get; set; } = string.Empty;

        public bool DiagnosticsBannerVisible { get; set; }

        public bool DiagnosticsFatalVisible { get; set; }

        public string Location { get; set; } = string.Empty;

        public bool RouteNotFoundVisible { get; set; }

        public int SourceInputCount { get; set; }
    }
}
