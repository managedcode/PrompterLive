using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class LearnStartupAlignmentTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string HiddenOpacity = "0";
    private const string HiddenVisibility = "hidden";
    private const string LayoutReadyAttributeName = "data-rsvp-layout-ready";
    private const string LayoutReadyFalseValue = "false";
    private const string LayoutReadyTrueValue = "true";
    private const double MaxReadyOrpDeltaPx = 6;
    private const string StartupWord = "Good";
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
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

            Assert.NotEmpty(startupWordSamples);

            var firstStartupWordSample = startupWordSamples[0];
            Assert.Equal(LayoutReadyFalseValue, firstStartupWordSample.LayoutReady);
            Assert.Equal(HiddenOpacity, firstStartupWordSample.RowOpacity);
            Assert.Equal(HiddenVisibility, firstStartupWordSample.RowVisibility);

            await WaitForLearnLayoutReadyAsync(page);
            var readyStartupWordSample = await ReadCurrentLearnLayoutAsync(page);
            Assert.Equal(LayoutReadyTrueValue, readyStartupWordSample.LayoutReady);
            Assert.InRange(readyStartupWordSample.OrpDeltaPx, 0, MaxReadyOrpDeltaPx);
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
                const maxSamples = 16;
                const maxAnimationFramePasses = maxSamples;

                window.__learnStartupTrace = [];

                const capture = () => {
                    if (window.__learnStartupTrace.length >= maxSamples) {
                        return;
                    }

                    const display = document.querySelector(`[data-testid="${learnDisplayTestId}"]`);
                    const row = display?.querySelector('.rsvp-h-row');
                    const line = document.querySelector(`[data-testid="${learnLineTestId}"]`);
                    const word = document.querySelector(`[data-testid="${learnWordTestId}"]`);
                    const orp = word?.querySelector('.orp');
                    if (!display || !row || !line || !word || !orp) {
                        return;
                    }

                    const rowStyles = getComputedStyle(row);
                    const lineRect = line.getBoundingClientRect();
                    const orpRect = orp.getBoundingClientRect();

                    window.__learnStartupTrace.push({
                        layoutReady: display.getAttribute(layoutReadyAttributeName),
                        orpDeltaPx: Math.abs((lineRect.left + (lineRect.width / 2)) - (orpRect.left + (orpRect.width / 2))),
                        rowOpacity: rowStyles.opacity,
                        rowVisibility: rowStyles.visibility,
                        text: word.textContent.replace(/\s+/g, '')
                    });
                };

                let animationFramePasses = 0;
                const captureOnAnimationFrame = () => {
                    capture();

                    if (window.__learnStartupTrace.length >= maxSamples || animationFramePasses >= maxAnimationFramePasses) {
                        return;
                    }

                    animationFramePasses += 1;
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

    private static Task WaitForLearnLayoutReadyAsync(Microsoft.Playwright.IPage page) =>
        page.WaitForFunctionAsync(
            """
            args => {
                const display = document.querySelector(`[data-testid="${args.displayTestId}"]`);
                return display?.getAttribute(args.layoutReadyAttributeName) === args.layoutReadyValue;
            }
            """,
            new
            {
                displayTestId = UiTestIds.Learn.Display,
                layoutReadyAttributeName = LayoutReadyAttributeName,
                layoutReadyValue = LayoutReadyTrueValue
            });

    private static Task<LearnStartupTraceSample> ReadCurrentLearnLayoutAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<LearnStartupTraceSample>(
            """
            args => {
                const display = document.querySelector(`[data-testid="${args.displayTestId}"]`);
                const row = display?.querySelector('.rsvp-h-row');
                const line = document.querySelector(`[data-testid="${args.lineTestId}"]`);
                const word = document.querySelector(`[data-testid="${args.wordTestId}"]`);
                const orp = word?.querySelector('.orp');
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
                layoutReadyAttributeName = LayoutReadyAttributeName,
                lineTestId = UiTestIds.Learn.OrpLine,
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
