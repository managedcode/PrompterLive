using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services.Editor;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
[NotInParallel(UiTestParallelization.EditorPerformanceConstraintKey)]
public sealed class EditorTypingTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_RapidTypingUpdatesStructureAndPersistsAfterReload()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.ActiveSegmentName)).ToHaveCountAsync(0);
            await Expect(page.GetByTestId(UiTestIds.Editor.ActiveBlockName)).ToHaveCountAsync(0);

            await EditorMonacoDriver.ClearAndTypeAsync(page, BrowserTestConstants.Editor.TypedScript);

            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(BrowserTestConstants.Editor.TypedScript);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync(BrowserTestConstants.Editor.TypedTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync(BrowserTestConstants.Editor.TypedBlock);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight))
                .ToContainTextAsync(BrowserTestConstants.Editor.TypedHighlight);

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.PersistDelayMs);
            await page.ReloadAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(BrowserTestConstants.Editor.TypedScript);
            await Expect(page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0))).ToContainTextAsync(BrowserTestConstants.Editor.TypedTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.BlockNavigation(0, 0))).ToContainTextAsync(BrowserTestConstants.Editor.TypedBlock);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_KeepsStyledOverlayVisibleAndPreservesClickCaretPlacement()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await EditorMonacoDriver.ClickAsync(page, new Position
            {
                X = BrowserTestConstants.Editor.ClickNearStartOffsetX,
                Y = BrowserTestConstants.Editor.ClickNearStartOffsetY
            });

            var editorSurfaceState = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(editorSurfaceState.Selection.Start).IsBetween(0, BrowserTestConstants.Editor.ClickCaretThreshold);
            await Assert.That(editorSurfaceState.Ready).IsTrue();
            await Assert.That(editorSurfaceState.Engine).IsEqualTo(EditorMonacoRuntimeContract.EditorEngineAttributeValue);
            await Assert.That(editorSurfaceState.DecorationClasses.Count > 0).IsTrue();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_QuantumHeaderEditingKeepsBlockLineStyled()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetCaretAtTextEndAsync(page, BrowserTestConstants.Editor.QuantumOverviewBlockHeader);

            await page.Keyboard.TypeAsync(BrowserTestConstants.Editor.HeaderContinuationText, new() { Delay = 0 });
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.TypingProbeSettleDelayMs);

            var lineState = await page.EvaluateAsync<HeaderLineState>(
                """
                (args) => {
                    const overlay = document.querySelector(`[data-test="${args.overlayTestId}"]`);
                    if (!overlay) {
                        throw new Error("Unable to inspect the quantum header line.");
                    }

                    const line = Array
                        .from(overlay.children)
                        .map(node => ({
                            text: node.textContent ?? ''
                        }))
                        .find(node => node.text.includes(args.targetText));

                    return {
                        text: line?.text ?? ''
                    };
                }
                """,
                new
                {
                    overlayTestId = UiTestIds.Editor.SourceHighlight,
                    targetText = BrowserTestConstants.Editor.QuantumOverviewBlockHeader
                });

            await Assert.That(lineState.Text).IsEqualTo(BrowserTestConstants.Editor.QuantumOverviewBlockLineText);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_SequentialTypingIntoSourceInputCompletesWithoutTimeout()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.ClearAndTypeAsync(page, BrowserTestConstants.Editor.TypedScript);

            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(BrowserTestConstants.Editor.TypedScript);
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

    [Test]
    public async Task EditorScreen_NewDraftDoesNotReplaceRouteWhileTypingIsStillSettling()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);

            await EditorMonacoDriver.ClickAsync(page);
            await page.Keyboard.TypeAsync(BrowserTestConstants.Editor.FirstProbeCharacter);
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.NewDraftPersistGraceDelayMs);

            var graceUri = new Uri(page.Url);
            await Assert.That(graceUri.AbsolutePath).IsEqualTo(BrowserTestConstants.Routes.Editor);
            await Assert.That(string.IsNullOrEmpty(graceUri.Query)).IsTrue();

            await page.Keyboard.TypeAsync(BrowserTestConstants.Editor.SecondProbeCharacter);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(BrowserTestConstants.Editor.NewDraftProbeText);

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.NewDraftPersistSettleDelayMs);

            var currentUri = new Uri(page.Url);
            await Assert.That(currentUri.AbsolutePath).IsEqualTo(BrowserTestConstants.Routes.Editor);
            await Assert.That(currentUri.Query.Contains(AppRoutes.ScriptIdQueryKey, StringComparison.Ordinal)).IsTrue();
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(BrowserTestConstants.Editor.NewDraftProbeText);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_QuantumTypingKeepsStyledOverlayVisibleResponsive()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.ClickAsync(page);
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.SelectAll);
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Backspace);

            await page.EvaluateAsync(
                """
                (args) => {
                    const input = document.querySelector(`[data-test="${args.inputTestId}"]`);
                    const overlay = document.querySelector(`[data-test="${args.overlayTestId}"]`);
                    const harness = window[args.harnessGlobalName];
                    if (!input || !overlay) {
                        throw new Error("Unable to attach the editor typing probe.");
                    }

                    const samples = [];
                    const longTasks = [];
                    const errors = [];
                    const pendingSamples = [];
                    let eventCount = 0;
                    let lastExpectedLength = -1;
                    let pendingSampleCount = 0;
                    const observer = new PerformanceObserver(list => {
                        for (const entry of list.getEntries()) {
                            longTasks.push(entry.duration);
                        }
                    });
                    observer.observe({ type: "longtask" });

                    const readRenderedLength = () =>
                        Number.parseInt(overlay.dataset.renderedLength ?? '-1', 10);

                    const completeVisibleSamples = renderedLength => {
                        while (pendingSamples.length > 0 && renderedLength >= pendingSamples[0].expectedLength) {
                            const pendingSample = pendingSamples.shift();
                            const state = harness?.getState(args.stageTestId);
                            samples.push({
                                latency: performance.now() - pendingSample.started,
                                decorationClassCount: state?.decorationClasses?.length ?? 0,
                                inputVisible: getComputedStyle(input).color !== args.transparentInputColor,
                                ready: state?.ready === true,
                                renderedLength
                            });
                        }

                        pendingSampleCount = pendingSamples.length;
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

                    input.addEventListener(args.proxyChangedEventName, event => {
                        eventCount += 1;
                        const expectedLength = Number.isFinite(event?.detail?.textLength)
                            ? event.detail.textLength
                            : input.value.length;
                        lastExpectedLength = expectedLength;
                        pendingSamples.push({
                            expectedLength,
                            started: performance.now()
                        });
                        pendingSampleCount = pendingSamples.length;

                        try {
                            completeVisibleSamples(readRenderedLength());
                        }
                        catch (error) {
                            errors.push(String(error?.stack ?? error));
                        }
                    }, { passive: true });

                    window.__editorTypingProbe = {
                        errors,
                        eventCount: () => eventCount,
                        lastExpectedLength: () => lastExpectedLength,
                        observer,
                        overlayObserver,
                        pendingSampleCount: () => pendingSampleCount,
                        samples,
                        longTasks,
                        input,
                        overlay
                    };
                }
                """,
                new
                {
                    harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                    inputTestId = UiTestIds.Editor.SourceInput,
                    overlayTestId = UiTestIds.Editor.SourceHighlight,
                    proxyChangedEventName = EditorMonacoRuntimeContract.EditorProxyChangedEventName,
                    stageTestId = UiTestIds.Editor.SourceStage,
                    transparentInputColor = BrowserTestConstants.Editor.TransparentInputColor
                });

            await page.Keyboard.TypeAsync(
                BrowserTestConstants.Editor.TypingResponsivenessProbeText,
                new() { Delay = 0 });

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.TypingProbeSettleDelayMs);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceHighlight))
                .ToContainTextAsync(BrowserTestConstants.Editor.TypingResponsivenessProbeText);

            var probeResult = await page.EvaluateAsync<TypingProbeResult>(
                """
                () => {
                    const probe = window.__editorTypingProbe;
                    if (!probe) {
                        throw new Error("Editor typing probe data is missing.");
                    }

                    const latencies = probe.samples
                        .map(sample => sample.latency)
                        .sort((left, right) => left - right);
                    const p95Index = latencies.length
                        ? Math.max(0, Math.ceil(latencies.length * 0.95) - 1)
                        : -1;

                    probe.observer.disconnect();
                    probe.overlayObserver?.disconnect?.();
                    return {
                        sampleCount: probe.samples.length,
                        maxLatency: latencies.length ? latencies[latencies.length - 1] : -1,
                        p95Latency: p95Index >= 0 ? latencies[p95Index] : -1,
                        longTaskCount: probe.longTasks.length,
                        maxLongTaskDuration: probe.longTasks.length ? Math.max(...probe.longTasks) : 0,
                        maxDecorationClassCount: probe.samples.length
                            ? Math.max(...probe.samples.map(sample => sample.decorationClassCount))
                            : 0,
                        eventCount: typeof probe.eventCount === 'function'
                            ? probe.eventCount()
                            : -1,
                        lastExpectedLength: typeof probe.lastExpectedLength === 'function'
                            ? probe.lastExpectedLength()
                            : -1,
                        readyDuringTyping: probe.samples.every(sample => sample.ready === true),
                        sawVisibleInput: probe.samples.some(sample => sample.inputVisible),
                        finalInputColor: getComputedStyle(probe.input).color,
                        errors: probe.errors ?? [],
                        pendingSampleCount: typeof probe.pendingSampleCount === 'function'
                            ? probe.pendingSampleCount()
                            : -1,
                        finalRenderedLength: Number.parseInt(
                            probe.overlay.dataset.renderedLength ?? '-1',
                            10)
                    };
                }
                """);

            await Assert.That(probeResult.EventCount > 0).IsTrue().Because($"Expected Monaco proxy change events during typing, but observed none. Pending samples: {probeResult.PendingSampleCount}, final rendered length: {probeResult.FinalRenderedLength}, last expected length: {probeResult.LastExpectedLength}.");
            await Assert.That(probeResult.SampleCount > 0).IsTrue().Because($"Expected the typing probe to capture visible overlay latency samples, but observed none after {probeResult.EventCount} proxy change event(s). Pending samples: {probeResult.PendingSampleCount}, final rendered length: {probeResult.FinalRenderedLength}, last expected length: {probeResult.LastExpectedLength}, errors: {string.Join(" || ", probeResult.Errors)}.");
            await Assert.That(probeResult.SawVisibleInput).IsFalse();
            await Assert.That(probeResult.P95Latency).IsBetween(0, BrowserTestConstants.Editor.MaxVisibleRenderP95LatencyMs);
            await Assert.That(probeResult.MaxLatency).IsBetween(0, BrowserTestConstants.Editor.MaxVisibleRenderSpikeLatencyMs);
            await Assert.That(probeResult.LongTaskCount <= BrowserTestConstants.Editor.AllowedTypingLongTaskCount).IsTrue().Because($"Expected Monaco typing to avoid browser long tasks, but observed {probeResult.LongTaskCount} long task(s) with max duration {probeResult.MaxLongTaskDuration:0.##}ms against the current budget of {BrowserTestConstants.Editor.AllowedTypingLongTaskCount}.");
            await Assert.That(probeResult.FinalInputColor).IsEqualTo(BrowserTestConstants.Editor.TransparentInputColor);
            await Assert.That(probeResult.ReadyDuringTyping).IsTrue();
            await Assert.That(probeResult.PendingSampleCount).IsEqualTo(0);
            await Assert.That(probeResult.FinalRenderedLength >= BrowserTestConstants.Editor.TypingResponsivenessProbeText.Length).IsTrue().Because($"Expected the Monaco overlay to render the full probe text during typing, but the rendered length was {probeResult.FinalRenderedLength}.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private sealed class HeaderLineState
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class TypingProbeResult
    {
        public string FinalInputColor { get; set; } = string.Empty;

        public int EventCount { get; set; }

        public int FinalRenderedLength { get; set; }

        public string[] Errors { get; set; } = [];

        public int LastExpectedLength { get; set; }

        public int LongTaskCount { get; set; }

        public int MaxDecorationClassCount { get; set; }

        public double MaxLatency { get; set; }

        public double MaxLongTaskDuration { get; set; }

        public double P95Latency { get; set; }

        public int PendingSampleCount { get; set; }

        public bool ReadyDuringTyping { get; set; }

        public bool SawVisibleInput { get; set; }

        public int SampleCount { get; set; }
    }
}
