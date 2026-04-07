using System.Globalization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LearnStartupAlignmentTests(StandaloneAppFixture fixture)
{
    private const string HiddenOpacity = "0";
    private const string HiddenVisibility = "hidden";
    private const string LayoutReadyAttributeName = "data-rsvp-layout-ready";
    private const string LayoutReadyFalseValue = "false";
    private const string LayoutReadyTrueValue = "true";
    private const double MaxReadyOrpDeltaPx = 6;
    private const int RequiredStableReadySamples = 2;
    private const string StartupWord = "Good";
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task LearnScreen_DemoStartup_HidesFocusRowUntilOrpLayoutIsReady()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.AddInitScriptAsync(BuildStartupTraceScript());
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.DemoViewportWidth,
                BrowserTestConstants.Learn.DemoViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await page.WaitForFunctionAsync(
                """
                expectedWord => Array.isArray(window.__learnStartupTrace) &&
                    window.__learnStartupTrace.some(sample => sample.text === expectedWord)
                """,
                StartupWord);

            var trace = await page.EvaluateAsync<LearnStartupTraceSample[]>(
                "() => window.__learnStartupTrace ?? []");
            var startupWordSamples = trace
                .Where(sample => string.Equals(sample.Text, StartupWord, StringComparison.Ordinal))
                .ToArray();

            await Assert.That(trace).IsNotEmpty();
            await Assert.That(startupWordSamples).IsNotEmpty();

            await Assert.That(trace).Contains(sample =>
                    string.Equals(sample.LayoutReady, LayoutReadyFalseValue, StringComparison.Ordinal) &&
                    string.Equals(sample.RowOpacity, HiddenOpacity, StringComparison.Ordinal) &&
                    string.Equals(sample.RowVisibility, HiddenVisibility, StringComparison.Ordinal));

            var readyStartupWordSample = await WaitForStableLearnLayoutAsync(page);
            await Assert.That(readyStartupWordSample.LayoutReady).IsEqualTo(LayoutReadyTrueValue);
            await Assert.That(readyStartupWordSample.OrpDeltaPx).IsBetween(0, MaxReadyOrpDeltaPx);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static string BuildStartupTraceScript()
    {
        return $$"""
            (() => {
                const layoutReadyAttributeName = {{ToJsString(LayoutReadyAttributeName)}};
                const learnDisplayTestId = {{ToJsString(UiTestIds.Learn.Display)}};
                const learnLineTestId = {{ToJsString(UiTestIds.Learn.OrpLine)}};
                const learnWordTestId = {{ToJsString(UiTestIds.Learn.Word)}};
                const learnWordOrpTestId = {{ToJsString(UiTestIds.Learn.WordOrp)}};
                const learnFocusRowTestId = {{ToJsString(UiTestIds.Learn.FocusRow)}};
                const maxSamples = 16;
                const maxAnimationFramePasses = maxSamples;

                window.__learnStartupTrace = [];

                const readEffectiveOpacity = element => {
                    let effectiveOpacity = 1;
                    let current = element;

                    while (current instanceof HTMLElement) {
                        const currentOpacity = Number.parseFloat(getComputedStyle(current).opacity || '1');
                        effectiveOpacity *= Number.isFinite(currentOpacity) ? currentOpacity : 1;
                        current = current.parentElement;
                    }

                    return effectiveOpacity <= 0
                        ? '0'
                        : String(Math.round(effectiveOpacity * 1000) / 1000);
                };

                const capture = () => {
                    if (window.__learnStartupTrace.length >= maxSamples) {
                        return;
                    }

                    const display = document.querySelector(`[data-test="${learnDisplayTestId}"]`);
                    const row = document.querySelector(`[data-test="${learnFocusRowTestId}"]`);
                    const line = document.querySelector(`[data-test="${learnLineTestId}"]`);
                    const word = document.querySelector(`[data-test="${learnWordTestId}"]`);
                    const orp = document.querySelector(`[data-test="${learnWordOrpTestId}"]`);
                    if (!display || !row) {
                        return;
                    }

                    const rowStyles = getComputedStyle(row);
                    const lineRect = line?.getBoundingClientRect();
                    const orpRect = orp?.getBoundingClientRect();

                    window.__learnStartupTrace.push({
                        layoutReady: display.getAttribute(layoutReadyAttributeName),
                        orpDeltaPx: lineRect && orpRect
                            ? Math.abs((lineRect.left + (lineRect.width / 2)) - (orpRect.left + (orpRect.width / 2)))
                            : -1,
                        rowOpacity: readEffectiveOpacity(row),
                        rowVisibility: rowStyles.visibility,
                        text: word?.textContent?.replace(/\s+/g, '') ?? ''
                    });
                };

                let animationFramePasses = 0;
                const captureOnAnimationFrame = () => {
                    animationFramePasses += 1;
                    capture();

                    if (window.__learnStartupTrace.length >= maxSamples || animationFramePasses >= maxAnimationFramePasses) {
                        return;
                    }

                    window.requestAnimationFrame(captureOnAnimationFrame);
                };

                new MutationObserver(capture).observe(document, {
                    attributeFilter: ['style', layoutReadyAttributeName],
                    attributes: true,
                    characterData: true,
                    childList: true,
                    subtree: true
                });

                capture();
                window.requestAnimationFrame(captureOnAnimationFrame);
            })();
            """;
    }

    private static string ToJsString(string value) => $"'{value.Replace("\\", "\\\\").Replace("'", "\\'")}'";

    private static async Task<LearnStartupTraceSample> WaitForStableLearnLayoutAsync(Microsoft.Playwright.IPage page)
    {
        LearnStartupTraceSample? lastSample = null;
        var stableReadySamples = 0;
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var currentSample = await ReadCurrentLearnLayoutAsync(page);
            lastSample = currentSample;

            var isStableReady =
                string.Equals(currentSample.LayoutReady, LayoutReadyTrueValue, StringComparison.Ordinal) &&
                !string.Equals(currentSample.RowOpacity, HiddenOpacity, StringComparison.Ordinal) &&
                !string.Equals(currentSample.RowVisibility, HiddenVisibility, StringComparison.Ordinal) &&
                currentSample.OrpDeltaPx >= 0 &&
                currentSample.OrpDeltaPx <= MaxReadyOrpDeltaPx;

            stableReadySamples = isStableReady
                ? stableReadySamples + 1
                : 0;

            if (stableReadySamples >= RequiredStableReadySamples)
            {
                return currentSample;
            }

            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.DiagnosticPollDelayMs);
        }

        throw new InvalidOperationException(
            $"Learn layout did not stabilize. LayoutReady: {lastSample?.LayoutReady ?? "<null>"}; RowOpacity: {lastSample?.RowOpacity ?? "<null>"}; RowVisibility: {lastSample?.RowVisibility ?? "<null>"}; OrpDeltaPx: {(lastSample is null ? "<null>" : lastSample.OrpDeltaPx.ToString(CultureInfo.InvariantCulture))}; Text: {lastSample?.Text ?? "<null>"}.");
    }

    private static Task<LearnStartupTraceSample> ReadCurrentLearnLayoutAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<LearnStartupTraceSample>(
            """
            args => {
                const display = document.querySelector(`[data-test="${args.displayTestId}"]`);
                const row = document.querySelector(`[data-test="${args.focusRowTestId}"]`);
                const line = document.querySelector(`[data-test="${args.lineTestId}"]`);
                const word = document.querySelector(`[data-test="${args.wordTestId}"]`);
                const orp = document.querySelector(`[data-test="${args.orpTestId}"]`);
                const rowStyles = row ? getComputedStyle(row) : null;
                const lineRect = line?.getBoundingClientRect();
                const orpRect = orp?.getBoundingClientRect();

                return {
                    layoutReady: display?.getAttribute(args.layoutReadyAttributeName) ?? '',
                    orpDeltaPx: lineRect && orpRect
                        ? Math.abs((lineRect.left + (lineRect.width / 2)) - (orpRect.left + (orpRect.width / 2)))
                        : 999,
                    rowOpacity: rowStyles?.opacity ?? '',
                    rowVisibility: rowStyles?.visibility ?? '',
                    text: word?.textContent?.replace(/\\s+/g, '') ?? ''
                };
            }
            """,
            new
            {
                displayTestId = UiTestIds.Learn.Display,
                focusRowTestId = UiTestIds.Learn.FocusRow,
                layoutReadyAttributeName = LayoutReadyAttributeName,
                lineTestId = UiTestIds.Learn.OrpLine,
                orpTestId = UiTestIds.Learn.WordOrp,
                wordTestId = UiTestIds.Learn.Word
            });

    private sealed class LearnStartupTraceSample
    {
        public string? LayoutReady { get; set; }

        public double OrpDeltaPx { get; set; }

        public string RowOpacity { get; set; } = string.Empty;

        public string RowVisibility { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;
    }
}
