using System.Globalization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class LearnFidelityTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string LearnPlaybackProbeStartKey = "__prompterOneLearnFidelityPlaybackStartMs";
    private readonly record struct ContextGapMeasurement(double LeftGapPx, double RightGapPx);
    private readonly record struct ContextRailClipMeasurement(double LeftClipPx, double RightClipPx);
    private readonly record struct FocusWordSlackMeasurement(double SlackPx);
    private readonly record struct VisibleContextWordGapMeasurement(double LeftWordGapPx, double RightWordGapPx);

    [Fact]
    public async Task LearnScreen_KeepsOrpLetterCenteredOnReferenceGuide()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.LearnQuantum);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Learn.Word).Locator(".orp")).ToBeVisibleAsync();

            var initialDelta = await MeasureOrpDeltaAsync(page);
            Assert.InRange(initialDelta, 0, 6);

            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.LearnPlaybackDelayMs);

            var playbackDelta = await MeasureOrpDeltaAsync(page);
            Assert.InRange(playbackDelta, 0, 6);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_UsesPhraseTimelineForSecurityIncidentScript()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.LearnSecurityIncident);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Learn.StepForwardLarge).ClickAsync();

            for (var index = 0; index < BrowserTestConstants.Learn.MidFlowStepSmall; index++)
            {
                await page.GetByTestId(UiTestIds.Learn.StepForward).ClickAsync();
            }

            await ExpectFocusWordAsync(page, BrowserTestConstants.Learn.MidFlowWord);
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase))
                .Not.ToHaveTextAsync(BrowserTestConstants.Learn.EndOfScriptText);
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase))
                .ToContainTextAsync(BrowserTestConstants.Learn.NextPhraseFragment);

            var leftContextCount = await CountContextWordsAsync(page, UiDomIds.Learn.ContextLeft);
            var rightContextCount = await CountContextWordsAsync(page, UiDomIds.Learn.ContextRight);

            Assert.Equal(BrowserTestConstants.Learn.ContextWordCount, leftContextCount);
            Assert.Equal(BrowserTestConstants.Learn.ContextWordCount, rightContextCount);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_DemoContextRails_ShowTwoWordsPerSideWithoutRightRailClipping()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.DemoViewportWidth,
                BrowserTestConstants.Learn.DemoViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.DemoContextLayoutProbeWord,
                BrowserTestConstants.Learn.DemoContextLayoutProbeStepLimit);

            var leftWords = await ReadContextWordsAsync(page, UiDomIds.Learn.ContextLeft);
            var rightWords = await ReadContextWordsAsync(page, UiDomIds.Learn.ContextRight);

            Assert.Equal(
                [
                    BrowserTestConstants.Learn.DemoLeftContextFirstWord,
                    BrowserTestConstants.Learn.DemoLeftContextSecondWord
                ],
                leftWords);
            Assert.Equal(
                [
                    BrowserTestConstants.Learn.DemoRightContextFirstWord,
                    BrowserTestConstants.Learn.DemoRightContextSecondWord
                ],
                rightWords);

            var clip = await MeasureContextRailClipAsync(page);

            Assert.True(
                clip.LeftClipPx <= BrowserTestConstants.Learn.MaxRailClipPx,
                $"Expected the left Learn context rail to avoid clipping visible words, but it clipped by {clip.LeftClipPx:0.##}px.");
            Assert.True(
                clip.RightClipPx <= BrowserTestConstants.Learn.MaxRailClipPx,
                $"Expected the right Learn context rail to avoid clipping visible words, but it clipped by {clip.RightClipPx:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_KeepsContextRailsSeparatedFromFocusWordOnLeadershipScript()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.OverlapViewportWidth,
                BrowserTestConstants.Learn.OverlapViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.LearnLeadership);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await StepUntilWordAsync(page, BrowserTestConstants.Learn.OverlapProbeWord, BrowserTestConstants.Learn.OverlapProbeStepLimit);

            var gaps = await MeasureContextGapsAsync(page);

            Assert.True(
                gaps.LeftGapPx >= 0,
                $"Expected the left context rail to stop before the focus word, but the overlap was {-gaps.LeftGapPx:0.##} px.");
            Assert.True(
                gaps.RightGapPx >= 0,
                $"Expected the right context rail to start after the focus word, but the overlap was {-gaps.RightGapPx:0.##} px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_KeepsSecurityIncidentContextWordsCloseToFocusedWord()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.SecurityIncidentViewportWidth,
                BrowserTestConstants.Learn.SecurityIncidentViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.LearnSecurityIncident);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.SecurityIncidentProbeWord,
                BrowserTestConstants.Learn.SecurityIncidentProbeStepLimit);

            var gaps = await MeasureVisibleContextWordGapsAsync(page);

            Assert.True(
                gaps.LeftWordGapPx <= BrowserTestConstants.Learn.MaxVisibleContextWordGapPx,
                $"Expected the visible left context word gap to stay within {BrowserTestConstants.Learn.MaxVisibleContextWordGapPx}px, but it was {gaps.LeftWordGapPx:0.##}px.");
            Assert.True(
                gaps.RightWordGapPx <= BrowserTestConstants.Learn.MaxVisibleContextWordGapPx,
                $"Expected the visible right context word gap to stay within {BrowserTestConstants.Learn.MaxVisibleContextWordGapPx}px, but it was {gaps.RightWordGapPx:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_KeepsQuantumContextWordsCloseToFocusedWord()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.QuantumViewportWidth,
                BrowserTestConstants.Learn.QuantumViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.LearnQuantum);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.QuantumProbeWord,
                BrowserTestConstants.Learn.QuantumProbeStepLimit);

            var gaps = await MeasureVisibleContextWordGapsAsync(page);

            Assert.True(
                gaps.LeftWordGapPx <= BrowserTestConstants.Learn.MaxQuantumVisibleContextWordGapPx,
                $"Expected the quantum left context word gap to stay within {BrowserTestConstants.Learn.MaxQuantumVisibleContextWordGapPx}px, but it was {gaps.LeftWordGapPx:0.##}px.");
            Assert.True(
                gaps.RightWordGapPx <= BrowserTestConstants.Learn.MaxQuantumVisibleContextWordGapPx,
                $"Expected the quantum right context word gap to stay within {BrowserTestConstants.Learn.MaxQuantumVisibleContextWordGapPx}px, but it was {gaps.RightWordGapPx:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_LeadershipPreviewState_ShowsCurrentSentenceContextAndCloserContext()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.LeadershipViewportWidth,
                BrowserTestConstants.Learn.LeadershipViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.LearnLeadership);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.LeadershipPreviewProbeWord,
                BrowserTestConstants.Learn.LeadershipPreviewProbeStepLimit);

            var gaps = await MeasureVisibleContextWordGapsAsync(page);

            Assert.True(
                gaps.LeftWordGapPx <= BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx,
                $"Expected the leadership left context word gap to stay within {BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx}px, but it was {gaps.LeftWordGapPx:0.##}px.");
            Assert.True(
                gaps.RightWordGapPx <= BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx,
                $"Expected the leadership right context word gap to stay within {BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx}px, but it was {gaps.RightWordGapPx:0.##}px.");

            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase))
                .ToContainTextAsync(BrowserTestConstants.Learn.LeadershipCurrentSentencePreviewText);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_LeadershipUncertainState_StaysSentenceLocalAndDropsPunctuation()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.LeadershipViewportWidth,
                BrowserTestConstants.Learn.LeadershipViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.LearnLeadership);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.LeadershipCleanSentenceProbeWord,
                BrowserTestConstants.Learn.LeadershipCleanSentenceProbeStepLimit);

            var gaps = await MeasureVisibleContextWordGapsAsync(page);
            Assert.True(
                gaps.LeftWordGapPx <= BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx,
                $"Expected the leadership left context word gap to stay within {BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx}px, but it was {gaps.LeftWordGapPx:0.##}px.");
            Assert.True(
                gaps.RightWordGapPx <= BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx,
                $"Expected the leadership right context word gap to stay within {BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx}px, but it was {gaps.RightWordGapPx:0.##}px.");

            var focusWordOverflowPx = await MeasureFocusWordOverflowAsync(page);
            Assert.True(
                focusWordOverflowPx <= BrowserTestConstants.Learn.MaxFocusWordOverflowPx,
                $"Expected the Learn focus word to stay inside the visible RSVP lane, but it overflowed by {focusWordOverflowPx:0.##}px.");

            var leftWords = await ReadContextWordsAsync(page, UiDomIds.Learn.ContextLeft);
            var rightWords = await ReadContextWordsAsync(page, UiDomIds.Learn.ContextRight);

            Assert.Equal([BrowserTestConstants.Learn.LeadershipLeftContextWord], leftWords);
            Assert.Equal(
                [
                    BrowserTestConstants.Learn.LeadershipRightContextFirstWord,
                    BrowserTestConstants.Learn.LeadershipRightContextSecondWord
                ],
                rightWords);

            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase))
                .ToHaveTextAsync(BrowserTestConstants.Learn.LeadershipCleanSentencePreviewText);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_LongFocusWord_FitsWithoutClipping()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.LongWordProbeWord,
                BrowserTestConstants.Learn.LongWordProbeStepLimit);

            var overflowPx = await MeasureFocusWordOverflowAsync(page);

            Assert.True(
                overflowPx <= BrowserTestConstants.Learn.MaxFocusWordOverflowPx,
                $"Expected the Learn focus word to stay inside the visible RSVP lane, but it overflowed by {overflowPx:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_FocusWord_UsesPackedHorizontalStackAroundOrp()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.DemoFocusStackProbeWord,
                BrowserTestConstants.Learn.DemoFocusStackProbeStepLimit);

            var slack = await MeasureFocusWordSlackAsync(page);

            Assert.True(
                slack.SlackPx <= BrowserTestConstants.Learn.MaxFocusWordSlackPx,
                $"Expected the Learn focus word to use a packed horizontal stack, but it still had {slack.SlackPx:0.##}px of internal slack.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Fact]
    public async Task LearnScreen_WpmIncrease_AcceleratesPlaybackCadence()
    {
        var slowPlaybackDuration = await MeasureLearnPlaybackDurationAsync(
            async page =>
            {
                await DecreaseLearnSpeedAsync(page, BrowserTestConstants.ReaderTiming.LearnSlowWpmAdjustmentClicks);
                await Expect(page.Locator($"#{UiDomIds.Learn.Speed}"))
                    .ToHaveTextAsync(BrowserTestConstants.ReaderTiming.LearnSlowWpm.ToString(CultureInfo.InvariantCulture));
            });

        var fastPlaybackDuration = await MeasureLearnPlaybackDurationAsync(
            async page =>
            {
                await IncreaseLearnSpeedAsync(page, BrowserTestConstants.ReaderTiming.LearnFastWpmAdjustmentClicks);
                await Expect(page.Locator($"#{UiDomIds.Learn.Speed}"))
                    .ToHaveTextAsync(BrowserTestConstants.ReaderTiming.LearnFastWpm.ToString(CultureInfo.InvariantCulture));
            });

        Assert.True(
            fastPlaybackDuration <= slowPlaybackDuration - BrowserTestConstants.ReaderTiming.MinimumSpeedProbePlaybackDeltaMs,
            $"Expected higher WPM to finish faster. Slow mode took {slowPlaybackDuration} ms, faster mode took {fastPlaybackDuration} ms.");
    }

    private static Task<double> MeasureOrpDeltaAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<double>(
            """
            () => {
                const line = document.querySelector('[data-testid="learn-orp-line"]');
                const orp = document.querySelector('[data-testid="learn-word"] .orp');
                if (!line || !orp) {
                    return 999;
                }

                const lineRect = line.getBoundingClientRect();
                const orpRect = orp.getBoundingClientRect();
                const lineCenter = lineRect.left + (lineRect.width / 2);
                const orpCenter = orpRect.left + (orpRect.width / 2);
                return Math.abs(lineCenter - orpCenter);
            }
            """);

    private static Task<int> CountContextWordsAsync(Microsoft.Playwright.IPage page, string elementId) =>
        page.EvaluateAsync<int>(
            """
            targetId => {
                const element = document.getElementById(targetId);
                return element ? element.children.length : -1;
            }
            """,
            elementId);

    private static Task<string[]> ReadContextWordsAsync(Microsoft.Playwright.IPage page, string elementId) =>
        page.EvaluateAsync<string[]>(
            """
            targetId => {
                const element = document.getElementById(targetId);
                if (!element) {
                    return [];
                }

                return Array.from(element.children)
                    .map(child => child.textContent?.trim() ?? '')
                    .filter(text => text.length > 0);
            }
            """,
            elementId);

    private static async Task StepUntilWordAsync(Microsoft.Playwright.IPage page, string targetWord, int stepLimit)
    {
        for (var stepIndex = 0; stepIndex < stepLimit; stepIndex++)
        {
            var currentWord = await ReadFocusWordAsync(page);
            if (string.Equals(currentWord, targetWord, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await page.GetByTestId(UiTestIds.Learn.StepForward).ClickAsync();
        }

        Assert.Fail($"Did not reach the learn probe word '{targetWord}' within {stepLimit} steps.");
    }

    private static async Task ExpectFocusWordAsync(Microsoft.Playwright.IPage page, string expectedWord)
    {
        await Expect(page.GetByTestId(UiTestIds.Learn.Word))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

        var actualWord = await ReadFocusWordAsync(page);
        Assert.Equal(expectedWord, actualWord, ignoreCase: true);
    }

    private static async Task<string> ReadFocusWordAsync(Microsoft.Playwright.IPage page)
    {
        var rawWord = await page.GetByTestId(UiTestIds.Learn.Word).TextContentAsync();
        return string.Concat((rawWord ?? string.Empty).Where(character => !char.IsWhiteSpace(character)));
    }

    private static async Task IncreaseLearnSpeedAsync(Microsoft.Playwright.IPage page, int clickCount)
    {
        for (var clickIndex = 0; clickIndex < clickCount; clickIndex++)
        {
            await page.GetByTestId(UiTestIds.Learn.SpeedUp).ClickAsync();
        }
    }

    private static async Task DecreaseLearnSpeedAsync(Microsoft.Playwright.IPage page, int clickCount)
    {
        for (var clickIndex = 0; clickIndex < clickCount; clickIndex++)
        {
            await page.GetByTestId(UiTestIds.Learn.SpeedDown).ClickAsync();
        }
    }

    private async Task<int> MeasureLearnPlaybackDurationAsync(Func<Microsoft.Playwright.IPage, Task> configurePageAsync)
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.LearnReaderTiming);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await configurePageAsync(page);

            return await MeasurePlaybackDurationToWordAsync(
                page,
                BrowserTestConstants.ReaderTiming.WordCount);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task<int> MeasurePlaybackDurationToWordAsync(Microsoft.Playwright.IPage page, int targetWordNumber)
    {
        await page.EvaluateAsync(
            """
            probe => {
                window[probe.key] = performance.now();
            }
            """,
            new
            {
                key = LearnPlaybackProbeStartKey
            });
        await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();
        await page.WaitForFunctionAsync(
            """
            probe => {
                const element = document.querySelector(`[data-testid="${probe.progressLabelTestId}"]`);
                const text = element?.textContent ?? '';
                const match = /Word\s+(\d+)/.exec(text);
                return match !== null && Number(match[1]) >= probe.targetWordNumber;
            }
            """,
            new
            {
                progressLabelTestId = UiTestIds.Learn.ProgressLabel,
                targetWordNumber
            },
            new()
            {
                Timeout = BrowserTestConstants.ReaderTiming.SampleCaptureTimeoutMs
            });

        return await page.EvaluateAsync<int>(
            """
            probe => Math.round(performance.now() - (window[probe.key] ?? performance.now()))
            """,
            new
            {
                key = LearnPlaybackProbeStartKey
            });
    }

    private static Task<ContextGapMeasurement> MeasureContextGapsAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<ContextGapMeasurement>(
            """
            () => {
                const left = document.querySelector('[data-testid="learn-context-left"]');
                const focus = document.querySelector('[data-testid="learn-word"]');
                const right = document.querySelector('[data-testid="learn-context-right"]');
                if (!left || !focus || !right) {
                    return { leftGapPx: -999, rightGapPx: -999 };
                }

                const leftRect = left.getBoundingClientRect();
                const focusRect = focus.getBoundingClientRect();
                const rightRect = right.getBoundingClientRect();

                return {
                    leftGapPx: focusRect.left - leftRect.right,
                    rightGapPx: rightRect.left - focusRect.right
                };
            }
            """);

    private static Task<VisibleContextWordGapMeasurement> MeasureVisibleContextWordGapsAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<VisibleContextWordGapMeasurement>(
            """
            ids => {
                const left = document.querySelector(`[data-testid="${ids.left}"]`);
                const focus = document.querySelector(`[data-testid="${ids.focus}"]`);
                const right = document.querySelector(`[data-testid="${ids.right}"]`);
                const leftWord = left?.lastElementChild;
                const rightWord = right?.firstElementChild;
                if (!leftWord || !focus || !rightWord) {
                    return { leftWordGapPx: 999, rightWordGapPx: 999 };
                }

                const leftWordRect = leftWord.getBoundingClientRect();
                const focusRect = focus.getBoundingClientRect();
                const rightWordRect = rightWord.getBoundingClientRect();

                return {
                    leftWordGapPx: focusRect.left - leftWordRect.right,
                    rightWordGapPx: rightWordRect.left - focusRect.right
                };
            }
            """,
            new
            {
                left = UiTestIds.Learn.ContextLeft,
                focus = UiTestIds.Learn.Word,
                right = UiTestIds.Learn.ContextRight
            });

    private static Task<ContextRailClipMeasurement> MeasureContextRailClipAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<ContextRailClipMeasurement>(
            """
            ids => {
                const leftRail = document.querySelector(`[data-testid="${ids.left}"]`);
                const rightRail = document.querySelector(`[data-testid="${ids.right}"]`);
                if (!leftRail || !rightRail) {
                    return { leftClipPx: 999, rightClipPx: 999 };
                }

                const leftOverflowVisible = getComputedStyle(leftRail).overflowX === 'visible';
                const rightOverflowVisible = getComputedStyle(rightRail).overflowX === 'visible';
                const leftWord = leftRail.firstElementChild;
                const rightWord = rightRail.lastElementChild;
                const leftRailRect = leftRail.getBoundingClientRect();
                const rightRailRect = rightRail.getBoundingClientRect();
                const leftWordRect = leftWord?.getBoundingClientRect();
                const rightWordRect = rightWord?.getBoundingClientRect();

                return {
                    leftClipPx: leftOverflowVisible || !leftWordRect
                        ? 0
                        : Math.max(leftRailRect.left - leftWordRect.left, 0),
                    rightClipPx: rightOverflowVisible || !rightWordRect
                        ? 0
                        : Math.max(rightWordRect.right - rightRailRect.right, 0)
                };
            }
            """,
            new
            {
                left = UiTestIds.Learn.ContextLeft,
                right = UiTestIds.Learn.ContextRight
            });

    private static Task<FocusWordSlackMeasurement> MeasureFocusWordSlackAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<FocusWordSlackMeasurement>(
            """
            ids => {
                const word = document.querySelector(`[data-testid="${ids.word}"]`);
                const leading = word?.querySelector('.rsvp-focus-leading');
                const orp = word?.querySelector('.rsvp-focus-orp');
                const trailing = word?.querySelector('.rsvp-focus-trailing');
                if (!leading || !orp || !trailing) {
                    return { slackPx: 999 };
                }

                const wordRect = word.getBoundingClientRect();
                const leadingRect = leading.getBoundingClientRect();
                const orpRect = orp.getBoundingClientRect();
                const trailingRect = trailing.getBoundingClientRect();
                const occupiedWidth = leadingRect.width + orpRect.width + trailingRect.width;
                const slackPx = Math.max(wordRect.width - occupiedWidth, 0);

                return { slackPx };
            }
            """,
            new
            {
                word = UiTestIds.Learn.Word
            });

    private static Task<double> MeasureFocusWordOverflowAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<double>(
            """
            ids => {
                const display = document.querySelector(`[data-testid="${ids.display}"]`);
                const word = document.querySelector(`[data-testid="${ids.word}"]`);
                const leading = word?.querySelector('.rsvp-focus-leading');
                const trailing = word?.querySelector('.rsvp-focus-trailing');
                if (!display || !word || !leading || !trailing) {
                    return 999;
                }

                const displayRect = display.getBoundingClientRect();
                const leadingRect = leading.getBoundingClientRect();
                const trailingRect = trailing.getBoundingClientRect();
                const leftOverflow = Math.max(displayRect.left - leadingRect.left, 0);
                const rightOverflow = Math.max(trailingRect.right - displayRect.right, 0);
                return Math.max(leftOverflow, rightOverflow);
            }
            """,
            new
            {
                display = UiTestIds.Learn.Display,
                word = UiTestIds.Learn.Word
            });
}
