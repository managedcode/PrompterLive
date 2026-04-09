using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services.Editor;

namespace PrompterOne.Web.UITests;

[System.Obsolete]

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
[NotInParallel]
public sealed class EditorHugeDraftPerformanceTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_HugeDraftLoadedFromSeedKeepsFollowupTypingResponsive()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            var hugeDraft = EditorLargeDraftPerformanceTestData.BuildHugeDraft();
            var expectedLength =
                EditorLargeDraftPerformanceTestData.GetVisibleDraftLength(hugeDraft) +
                EditorLargeDraftPerformanceTestData.FollowupTypingText.Length;

            await page.GotoAsync(BrowserTestConstants.Routes.EditorHugeDraft);
            var editorReady = false;
            var deadline = DateTimeOffset.UtcNow.AddMilliseconds(EditorLargeDraftPerformanceTestData.HugeDraftReadyTimeoutMs);
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (await EditorMonacoDriver.SourceStage(page).IsVisibleAsync())
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
                        diagnosticsBannerVisible: Boolean(document.querySelector(`[data-test="${args.diagnosticsBannerTestId}"]`)),
                        diagnosticsFatalVisible: Boolean(document.querySelector(`[data-test="${args.diagnosticsFatalTestId}"]`)),
                        routeNotFoundVisible: Boolean((document.body?.innerText ?? '').includes(args.routeNotFoundText)),
                        sourceInputCount: document.querySelectorAll(`[data-test="${args.sourceInputTestId}"]`).length,
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

                await Assert.That(editorReady).IsTrue().Because($"Huge draft editor did not appear. Url: {debug.Location}; SourceInputs: {debug.SourceInputCount}; Banner: {debug.DiagnosticsBannerVisible}; Fatal: {debug.DiagnosticsFatalVisible}; RouteNotFound: {debug.RouteNotFoundVisible}; Body: {debug.BodySnippet}");
            }

            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const input = document.querySelector(`[data-test="${args.inputTestId}"]`);
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
                    const input = document.querySelector(`[data-test="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-test="${args.overlayTestId}"]`);
                    if (!input || !overlay) {
                        throw new Error("Unable to initialize the huge draft performance probe.");
                    }

                    const samples = [];
                    const longTasks = [];
                    const errors = [];
                    const pendingSamples = [];
                    const observer = new PerformanceObserver(list => {
                        for (const entry of list.getEntries()) {
                            longTasks.push({
                                duration: entry.duration,
                                endTime: entry.startTime + entry.duration,
                                startTime: entry.startTime
                            });
                        }
                    });

                    observer.observe({ type: "longtask" });

                    const probe = {
                        captureStart: performance.now(),
                        errors,
                        eventCount: 0,
                        lastExpectedLength: -1,
                        longTasks,
                        observer,
                        overlayObserver: null,
                        pendingSampleCount: 0,
                        pendingSamples,
                        samples
                    };

                    const readRenderedLength = () =>
                        Number.parseInt(overlay.dataset.renderedLength ?? "-1", 10);

                    const completeVisibleSamples = renderedLength => {
                        while (pendingSamples.length > 0 && renderedLength >= pendingSamples[0].expectedLength) {
                            const pendingSample = pendingSamples.shift();
                            const completedAt = performance.now();
                            samples.push({
                                completedAt,
                                latency: completedAt - pendingSample.started,
                                renderedLength,
                                started: pendingSample.started
                            });
                        }

                        probe.pendingSampleCount = pendingSamples.length;
                    };

                    const overlayObserver = new MutationObserver(() => {
                        try {
                            completeVisibleSamples(readRenderedLength());
                        }
                        catch (error) {
                            errors.push(String(error?.stack ?? error));
                        }
                    });

                    overlayObserver.observe(overlay, {
                        attributes: true,
                        childList: true,
                        characterData: true,
                        subtree: true
                    });

                    probe.overlayObserver = overlayObserver;

                    input.addEventListener(args.proxyChangedEventName, event => {
                        probe.eventCount += 1;
                        const expectedLength = Number.isFinite(event?.detail?.textLength)
                            ? event.detail.textLength
                            : input.value.length;
                        probe.lastExpectedLength = expectedLength;
                        pendingSamples.push({
                            expectedLength,
                            started: performance.now()
                        });
                        probe.pendingSampleCount = pendingSamples.length;

                        try {
                            completeVisibleSamples(readRenderedLength());
                        }
                        catch (error) {
                            errors.push(String(error?.stack ?? error));
                        }
                    }, { passive: true });

                    window.__editorHugeDraftProbe = probe;
                }
                """,
                new
                {
                    inputTestId = UiTestIds.Editor.SourceInput,
                    overlayTestId = UiTestIds.Editor.SourceHighlight,
                    proxyChangedEventName = EditorMonacoRuntimeContract.EditorProxyChangedEventName
                });

            await EditorMonacoDriver.FocusAsync(page);
            await EditorMonacoDriver.SetCaretAtEndAsync(page);
            await page.EvaluateAsync(
                """
                () => {
                    const probe = window.__editorHugeDraftProbe;
                    if (!probe) {
                        throw new Error("Huge draft performance probe was not initialized before reset.");
                    }

                    probe.captureStart = performance.now();
                    probe.errors.length = 0;
                    probe.eventCount = 0;
                    probe.lastExpectedLength = -1;
                    probe.longTasks.length = 0;
                    probe.pendingSampleCount = 0;
                    probe.pendingSamples.length = 0;
                    probe.samples.length = 0;
                }
                """);
            await page.Keyboard.TypeAsync(EditorLargeDraftPerformanceTestData.FollowupTypingText, new() { Delay = 0 });
            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const overlay = document.querySelector(`[data-test="${args.overlayTestId}"]`);
                    const probe = window.__editorHugeDraftProbe;
                    if (!overlay || !probe) {
                        return false;
                    }

                    const renderedLength = Number.parseInt(overlay.dataset.renderedLength ?? "-1", 10);
                    return renderedLength >= args.expectedLength &&
                        probe.pendingSampleCount === 0 &&
                        probe.samples.length >= args.expectedSampleCount;
                }
                """,
                new
                {
                    expectedLength,
                    expectedSampleCount = EditorLargeDraftPerformanceTestData.FollowupTypingText.Length,
                    overlayTestId = UiTestIds.Editor.SourceHighlight
                },
                new() { Timeout = EditorLargeDraftPerformanceTestData.ObservationDelayMs });

            var result = await page.EvaluateAsync<HugeDraftProbeResult>(
                """
                (args) => {
                    const input = document.querySelector(`[data-test="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-test="${args.overlayTestId}"]`);
                    const probe = window.__editorHugeDraftProbe;
                    if (!input || !overlay || !probe) {
                        throw new Error("Huge draft performance probe result is unavailable.");
                    }

                    probe.observer.disconnect();
                    probe.overlayObserver?.disconnect?.();

                    const samples = probe.samples ?? [];
                    const captureStart = Number.isFinite(probe.captureStart)
                        ? probe.captureStart
                        : 0;
                    const responseWindowStart = samples.length
                        ? Math.min(...samples.map(sample => sample.started))
                        : -1;
                    const responseWindowEnd = samples.length
                        ? Math.max(...samples.map(sample => sample.completedAt))
                        : -1;
                    const relevantWindowStart = responseWindowStart >= 0
                        ? Math.max(captureStart, responseWindowStart)
                        : captureStart;
                    const observedLongTasks = (probe.longTasks ?? [])
                        .filter(entry => entry.endTime >= captureStart);
                    const responseWindowLongTasks = responseWindowEnd >= 0
                        ? observedLongTasks.filter(entry =>
                            entry.endTime >= relevantWindowStart &&
                            entry.startTime <= responseWindowEnd)
                        : [];

                    return {
                        finalInputLength: input.value.length,
                        finalRenderedLength: Number.parseInt(overlay.dataset.renderedLength ?? "-1", 10),
                        eventCount: probe.eventCount ?? -1,
                        errors: probe.errors ?? [],
                        followupMaxLongTaskMs: responseWindowLongTasks.length
                            ? Math.max(...responseWindowLongTasks.map(entry => entry.duration))
                            : 0,
                        lastExpectedLength: probe.lastExpectedLength ?? -1,
                        maxObservedLongTaskMs: observedLongTasks.length
                            ? Math.max(...observedLongTasks.map(entry => entry.duration))
                            : 0,
                        pendingSampleCount: probe.pendingSampleCount ?? -1,
                        responseWindowDurationMs: responseWindowEnd >= 0
                            ? Math.max(0, responseWindowEnd - relevantWindowStart)
                            : -1,
                        typingLatencyMs: samples[samples.length - 1]?.latency ?? -1,
                        typingSampleCount: samples.length
                    };
                }
                """,
                new
                {
                    inputTestId = UiTestIds.Editor.SourceInput,
                    overlayTestId = UiTestIds.Editor.SourceHighlight
                });

            await Assert.That(result.FinalInputLength).IsEqualTo(expectedLength);
            await Assert.That(result.FinalRenderedLength).IsEqualTo(expectedLength);
            await Assert.That(result.EventCount >= EditorLargeDraftPerformanceTestData.FollowupTypingText.Length).IsTrue().Because($"Expected Monaco proxy change events for the huge draft follow-up typing, but observed {result.EventCount}. Pending samples: {result.PendingSampleCount}; LastExpectedLength: {result.LastExpectedLength}; FinalInputLength: {result.FinalInputLength}; FinalRenderedLength: {result.FinalRenderedLength}.");
            await Assert.That(result.TypingSampleCount >= EditorLargeDraftPerformanceTestData.FollowupTypingText.Length).IsTrue().Because($"Expected the huge draft follow-up typing probe to capture visible overlay samples for each character, but observed {result.TypingSampleCount} sample(s) after {result.EventCount} proxy change event(s). Pending samples: {result.PendingSampleCount}; LastExpectedLength: {result.LastExpectedLength}; Errors: {string.Join(" || ", result.Errors)}.");
            await Assert.That(result.PendingSampleCount).IsEqualTo(0);
            await Assert.That(result.FollowupMaxLongTaskMs >= 0 &&
                result.FollowupMaxLongTaskMs <= EditorLargeDraftPerformanceTestData.MaxHugeFollowupLongTaskMs).IsTrue().Because($"Huge draft follow-up long task exceeded the acceptance budget. FollowupMaxLongTaskMs: {result.FollowupMaxLongTaskMs}; MaxHugeFollowupLongTaskMs: {EditorLargeDraftPerformanceTestData.MaxHugeFollowupLongTaskMs}; MaxObservedLongTaskMs: {result.MaxObservedLongTaskMs}; ResponseWindowDurationMs: {result.ResponseWindowDurationMs}; TypingLatencyMs: {result.TypingLatencyMs}; TypingSampleCount: {result.TypingSampleCount}; EventCount: {result.EventCount}; PendingSampleCount: {result.PendingSampleCount}; FinalInputLength: {result.FinalInputLength}; FinalRenderedLength: {result.FinalRenderedLength}; Errors: {string.Join(" || ", result.Errors)}.");
            await Assert.That(result.TypingLatencyMs >= 0 &&
                result.TypingLatencyMs <= EditorLargeDraftPerformanceTestData.MaxHugeTypingLatencyMs).IsTrue().Because($"Huge draft typing latency exceeded the acceptance budget. TypingLatencyMs: {result.TypingLatencyMs}; MaxHugeTypingLatencyMs: {EditorLargeDraftPerformanceTestData.MaxHugeTypingLatencyMs}; FollowupMaxLongTaskMs: {result.FollowupMaxLongTaskMs}; MaxObservedLongTaskMs: {result.MaxObservedLongTaskMs}; ResponseWindowDurationMs: {result.ResponseWindowDurationMs}; TypingSampleCount: {result.TypingSampleCount}; EventCount: {result.EventCount}; PendingSampleCount: {result.PendingSampleCount}; FinalInputLength: {result.FinalInputLength}; FinalRenderedLength: {result.FinalRenderedLength}; Errors: {string.Join(" || ", result.Errors)}.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private sealed class HugeDraftProbeResult
    {
        public int EventCount { get; set; }

        public string[] Errors { get; set; } = [];

        public int FinalInputLength { get; set; }

        public int FinalRenderedLength { get; set; }

        public double FollowupMaxLongTaskMs { get; set; }

        public int LastExpectedLength { get; set; }

        public double MaxObservedLongTaskMs { get; set; }

        public int PendingSampleCount { get; set; }

        public double ResponseWindowDurationMs { get; set; }

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
