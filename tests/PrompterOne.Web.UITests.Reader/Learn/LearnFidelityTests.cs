using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LearnFidelityTests(StandaloneAppFixture fixture)
{
    private const string LayoutReadyAttributeName = "data-rsvp-layout-ready";
    private const double MaxLayoutReadyOrpDeltaPx = 6;
    private const string LayoutReadyTrueValue = "true";
    private readonly record struct ContextGapMeasurement(double LeftGapPx, double RightGapPx);
    private readonly record struct ContextRailClipMeasurement(double LeftClipPx, double RightClipPx);
    private readonly record struct FocusWordSlackMeasurement(double SlackPx);
    private readonly record struct OrpDeltaMeasurement(double DeltaPx);
    private readonly record struct VisibleContextWordGapMeasurement(double LeftWordGapPx, double RightWordGapPx);

    [Test]
    public async Task LearnScreen_KeepsOrpLetterCenteredOnReferenceGuide()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnQuantum);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Learn.WordOrp)).ToBeVisibleAsync();
            var initialDelta = (await WaitForLearnLayoutReadyAsync(page)).DeltaPx;
            await Assert.That(initialDelta).IsBetween(0, 6);

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.PlayToggle));
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.LearnPlaybackDelayMs);
            var playbackDelta = (await WaitForLearnLayoutReadyAsync(page)).DeltaPx;
            await Assert.That(playbackDelta).IsBetween(0, 6);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LearnScreen_UsesPhraseTimelineForSecurityIncidentScript()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnSecurityIncident);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.StepForwardLarge));

            for (var index = 0; index < BrowserTestConstants.Learn.MidFlowStepSmall; index++)
            {
                await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.StepForward));
            }

            await ExpectFocusWordAsync(page, BrowserTestConstants.Learn.MidFlowWord);
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase))
                .Not.ToHaveTextAsync(BrowserTestConstants.Learn.EndOfScriptText);
            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase))
                .ToContainTextAsync(BrowserTestConstants.Learn.NextPhraseFragment);

            var leftContextCount = await CountContextWordsAsync(page, UiTestIds.Learn.ContextLeft);
            var rightContextCount = await CountContextWordsAsync(page, UiTestIds.Learn.ContextRight);

            await Assert.That(leftContextCount).IsEqualTo(BrowserTestConstants.Learn.ContextWordCount);
            await Assert.That(rightContextCount).IsEqualTo(BrowserTestConstants.Learn.ContextWordCount);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LearnScreen_DemoContextRails_ShowTwoWordsPerSideWithoutRightRailClipping()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.DemoViewportWidth,
                BrowserTestConstants.Learn.DemoViewportHeight);
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await WaitForLearnLayoutReadyAsync(page);

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.DemoContextLayoutProbeWord,
                BrowserTestConstants.Learn.DemoContextLayoutProbeStepLimit);

            var leftWords = await ReadContextWordsAsync(page, UiTestIds.Learn.ContextLeft);
            var rightWords = await ReadContextWordsAsync(page, UiTestIds.Learn.ContextRight);

            await Assert.That(leftWords).IsEquivalentTo([
                    BrowserTestConstants.Learn.DemoLeftContextFirstWord,
                    BrowserTestConstants.Learn.DemoLeftContextSecondWord
                ], CollectionOrdering.Matching);
            await Assert.That(rightWords).IsEquivalentTo([
                    BrowserTestConstants.Learn.DemoRightContextFirstWord,
                    BrowserTestConstants.Learn.DemoRightContextSecondWord
                ], CollectionOrdering.Matching);

            var clip = await MeasureContextRailClipAsync(page);

            await Assert.That(clip.LeftClipPx <= BrowserTestConstants.Learn.MaxRailClipPx).IsTrue().Because($"Expected the left Learn context rail to avoid clipping visible words, but it clipped by {clip.LeftClipPx:0.##}px.");
            await Assert.That(clip.RightClipPx <= BrowserTestConstants.Learn.MaxRailClipPx).IsTrue().Because($"Expected the right Learn context rail to avoid clipping visible words, but it clipped by {clip.RightClipPx:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LearnScreen_KeepsContextRailsSeparatedFromFocusWordOnLeadershipScript()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.OverlapViewportWidth,
                BrowserTestConstants.Learn.OverlapViewportHeight);
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnLeadership);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await WaitForLearnLayoutReadyAsync(page);

            await StepUntilWordAsync(page, BrowserTestConstants.Learn.OverlapProbeWord, BrowserTestConstants.Learn.OverlapProbeStepLimit);

            var gaps = await MeasureContextGapsAsync(page);

            await Assert.That(gaps.LeftGapPx >= 0).IsTrue().Because($"Expected the left context rail to stop before the focus word, but the overlap was {-gaps.LeftGapPx:0.##} px.");
            await Assert.That(gaps.RightGapPx >= 0).IsTrue().Because($"Expected the right context rail to start after the focus word, but the overlap was {-gaps.RightGapPx:0.##} px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LearnScreen_KeepsSecurityIncidentContextWordsCloseToFocusedWord()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.SecurityIncidentViewportWidth,
                BrowserTestConstants.Learn.SecurityIncidentViewportHeight);
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnSecurityIncident);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await WaitForLearnLayoutReadyAsync(page);

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.SecurityIncidentProbeWord,
                BrowserTestConstants.Learn.SecurityIncidentProbeStepLimit);

            var gaps = await MeasureVisibleContextWordGapsAsync(page);

            await Assert.That(gaps.LeftWordGapPx <= BrowserTestConstants.Learn.MaxVisibleContextWordGapPx).IsTrue().Because($"Expected the visible left context word gap to stay within {BrowserTestConstants.Learn.MaxVisibleContextWordGapPx}px, but it was {gaps.LeftWordGapPx:0.##}px.");
            await Assert.That(gaps.RightWordGapPx <= BrowserTestConstants.Learn.MaxVisibleContextWordGapPx).IsTrue().Because($"Expected the visible right context word gap to stay within {BrowserTestConstants.Learn.MaxVisibleContextWordGapPx}px, but it was {gaps.RightWordGapPx:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LearnScreen_KeepsQuantumContextWordsCloseToFocusedWord()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.QuantumViewportWidth,
                BrowserTestConstants.Learn.QuantumViewportHeight);
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnQuantum);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await WaitForLearnLayoutReadyAsync(page);

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.QuantumProbeWord,
                BrowserTestConstants.Learn.QuantumProbeStepLimit);

            var gaps = await MeasureVisibleContextWordGapsAsync(page);

            await Assert.That(gaps.LeftWordGapPx <= BrowserTestConstants.Learn.MaxQuantumVisibleContextWordGapPx).IsTrue().Because($"Expected the quantum left context word gap to stay within {BrowserTestConstants.Learn.MaxQuantumVisibleContextWordGapPx}px, but it was {gaps.LeftWordGapPx:0.##}px.");
            await Assert.That(gaps.RightWordGapPx <= BrowserTestConstants.Learn.MaxQuantumVisibleContextWordGapPx).IsTrue().Because($"Expected the quantum right context word gap to stay within {BrowserTestConstants.Learn.MaxQuantumVisibleContextWordGapPx}px, but it was {gaps.RightWordGapPx:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LearnScreen_LeadershipPreviewState_ShowsCurrentSentenceContextAndCloserContext()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.LeadershipViewportWidth,
                BrowserTestConstants.Learn.LeadershipViewportHeight);
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnLeadership);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await WaitForLearnLayoutReadyAsync(page);

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.LeadershipPreviewProbeWord,
                BrowserTestConstants.Learn.LeadershipPreviewProbeStepLimit);

            var gaps = await MeasureVisibleContextWordGapsAsync(page);

            await Assert.That(gaps.LeftWordGapPx <= BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx).IsTrue().Because($"Expected the leadership left context word gap to stay within {BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx}px, but it was {gaps.LeftWordGapPx:0.##}px.");
            await Assert.That(gaps.RightWordGapPx <= BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx).IsTrue().Because($"Expected the leadership right context word gap to stay within {BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx}px, but it was {gaps.RightWordGapPx:0.##}px.");

            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase))
                .ToContainTextAsync(BrowserTestConstants.Learn.LeadershipCurrentSentencePreviewText);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LearnScreen_LeadershipUncertainState_StaysSentenceLocalAndDropsPunctuation()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.LeadershipViewportWidth,
                BrowserTestConstants.Learn.LeadershipViewportHeight);
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnLeadership);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await WaitForLearnLayoutReadyAsync(page);

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.LeadershipCleanSentenceProbeWord,
                BrowserTestConstants.Learn.LeadershipCleanSentenceProbeStepLimit);

            var gaps = await MeasureVisibleContextWordGapsAsync(page);
            await Assert.That(gaps.LeftWordGapPx <= BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx).IsTrue().Because($"Expected the leadership left context word gap to stay within {BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx}px, but it was {gaps.LeftWordGapPx:0.##}px.");
            await Assert.That(gaps.RightWordGapPx <= BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx).IsTrue().Because($"Expected the leadership right context word gap to stay within {BrowserTestConstants.Learn.MaxLeadershipVisibleContextWordGapPx}px, but it was {gaps.RightWordGapPx:0.##}px.");

            var focusWordOverflowPx = await MeasureFocusWordOverflowAsync(page);
            await Assert.That(focusWordOverflowPx <= BrowserTestConstants.Learn.MaxFocusWordOverflowPx).IsTrue().Because($"Expected the Learn focus word to stay inside the visible RSVP lane, but it overflowed by {focusWordOverflowPx:0.##}px.");

            var leftWords = await ReadContextWordsAsync(page, UiTestIds.Learn.ContextLeft);
            var rightWords = await ReadContextWordsAsync(page, UiTestIds.Learn.ContextRight);

            await Assert.That(leftWords).IsEquivalentTo([BrowserTestConstants.Learn.LeadershipLeftContextWord], CollectionOrdering.Matching);
            await Assert.That(rightWords).IsEquivalentTo([
                    BrowserTestConstants.Learn.LeadershipRightContextFirstWord,
                    BrowserTestConstants.Learn.LeadershipRightContextSecondWord
                ], CollectionOrdering.Matching);

            await Expect(page.GetByTestId(UiTestIds.Learn.NextPhrase))
                .ToHaveTextAsync(BrowserTestConstants.Learn.LeadershipCleanSentencePreviewText);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LearnScreen_LongFocusWord_FitsWithoutClipping()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await WaitForLearnLayoutReadyAsync(page);

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.LongWordProbeWord,
                BrowserTestConstants.Learn.LongWordProbeStepLimit);

            var overflowPx = await MeasureFocusWordOverflowAsync(page);

            await Assert.That(overflowPx <= BrowserTestConstants.Learn.MaxFocusWordOverflowPx).IsTrue().Because($"Expected the Learn focus word to stay inside the visible RSVP lane, but it overflowed by {overflowPx:0.##}px.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task LearnScreen_FocusWord_UsesPackedHorizontalStackAroundOrp()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenLearnAsync(page, BrowserTestConstants.Routes.LearnDemo);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await WaitForLearnLayoutReadyAsync(page);

            await StepUntilWordAsync(
                page,
                BrowserTestConstants.Learn.DemoFocusStackProbeWord,
                BrowserTestConstants.Learn.DemoFocusStackProbeStepLimit);

            var slack = await MeasureFocusWordSlackAsync(page);

            await Assert.That(slack.SlackPx <= BrowserTestConstants.Learn.MaxFocusWordSlackPx).IsTrue().Because($"Expected the Learn focus word to use a packed horizontal stack, but it still had {slack.SlackPx:0.##}px of internal slack.");
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static Task<int> CountContextWordsAsync(Microsoft.Playwright.IPage page, string contextTestId) =>
        page.EvaluateAsync<int>(
            """
            targetTestId => {
                const element = document.querySelector(`[data-test="${targetTestId}"]`);
                return element ? element.children.length : -1;
            }
            """,
            contextTestId);

    private static Task<string[]> ReadContextWordsAsync(Microsoft.Playwright.IPage page, string contextTestId) =>
        page.EvaluateAsync<string[]>(
            """
            targetTestId => {
                const element = document.querySelector(`[data-test="${targetTestId}"]`);
                if (!element) {
                    return [];
                }

                return Array.from(element.children)
                    .map(child => child.textContent?.trim() ?? '')
                    .filter(text => text.length > 0);
            }
            """,
            contextTestId);

    private static async Task StepUntilWordAsync(Microsoft.Playwright.IPage page, string targetWord, int stepLimit)
    {
        for (var stepIndex = 0; stepIndex < stepLimit; stepIndex++)
        {
            var currentWord = await ReadFocusWordAsync(page);
            if (string.Equals(currentWord, targetWord, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Learn.StepForward));
            await WaitForLearnLayoutReadyAsync(page);
        }

        Assert.Fail("Unexpected execution path.");
    }

    private static async Task<OrpDeltaMeasurement> WaitForLearnLayoutReadyAsync(Microsoft.Playwright.IPage page)
    {
        var handle = await page.WaitForFunctionAsync(
            """
            args => {
                const display = document.querySelector(`[data-test="${args.displayTestId}"]`);
                const line = document.querySelector(`[data-test="${args.lineTestId}"]`);
                const orp = document.querySelector(`[data-test="${args.orpTestId}"]`);
                if (display?.getAttribute(args.layoutReadyAttributeName) !== args.layoutReadyValue || !line || !orp) {
                    return false;
                }

                const lineRect = line.getBoundingClientRect();
                const orpRect = orp.getBoundingClientRect();
                const delta = Math.abs((lineRect.left + (lineRect.width / 2)) - (orpRect.left + (orpRect.width / 2)));
                return delta <= args.maxOrpDeltaPx
                    ? { DeltaPx: delta }
                    : false;
            }
            """,
            new
            {
                displayTestId = UiTestIds.Learn.Display,
                layoutReadyAttributeName = LayoutReadyAttributeName,
                layoutReadyValue = LayoutReadyTrueValue,
                lineTestId = UiTestIds.Learn.OrpLine,
                maxOrpDeltaPx = MaxLayoutReadyOrpDeltaPx,
                orpTestId = UiTestIds.Learn.WordOrp
            });

        return await handle.JsonValueAsync<OrpDeltaMeasurement>();
    }

    private static async Task ExpectFocusWordAsync(Microsoft.Playwright.IPage page, string expectedWord)
    {
        await Expect(page.GetByTestId(UiTestIds.Learn.Word))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

        var actualWord = await ReadFocusWordAsync(page);
        // TODO: TUnit migration - xUnit Assert.Equal had additional argument(s) (ignoreCase: true) that could not be converted.
        await Assert.That(actualWord).IsEqualTo(expectedWord);
    }

    private static async Task<string> ReadFocusWordAsync(Microsoft.Playwright.IPage page)
    {
        var rawWord = await page.GetByTestId(UiTestIds.Learn.Word).TextContentAsync();
        return string.Concat((rawWord ?? string.Empty).Where(character => !char.IsWhiteSpace(character)));
    }

    private static Task<ContextGapMeasurement> MeasureContextGapsAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<ContextGapMeasurement>(
            """
            ids => {
                const left = document.querySelector(`[data-test="${ids.left}"]`);
                const focus = document.querySelector(`[data-test="${ids.focus}"]`);
                const right = document.querySelector(`[data-test="${ids.right}"]`);
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
            """,
            new
            {
                left = UiTestIds.Learn.ContextLeft,
                focus = UiTestIds.Learn.Word,
                right = UiTestIds.Learn.ContextRight
            });

    private static Task<VisibleContextWordGapMeasurement> MeasureVisibleContextWordGapsAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<VisibleContextWordGapMeasurement>(
            """
            ids => {
                const left = document.querySelector(`[data-test="${ids.left}"]`);
                const focus = document.querySelector(`[data-test="${ids.focus}"]`);
                const right = document.querySelector(`[data-test="${ids.right}"]`);
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
                const leftRail = document.querySelector(`[data-test="${ids.left}"]`);
                const rightRail = document.querySelector(`[data-test="${ids.right}"]`);
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
                const word = document.querySelector(`[data-test="${ids.word}"]`);
                const leading = document.querySelector(`[data-test="${ids.leading}"]`);
                const orp = document.querySelector(`[data-test="${ids.orp}"]`);
                const trailing = document.querySelector(`[data-test="${ids.trailing}"]`);
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
                leading = UiTestIds.Learn.WordLeading,
                orp = UiTestIds.Learn.WordOrp,
                trailing = UiTestIds.Learn.WordTrailing,
                word = UiTestIds.Learn.Word
            });

    private static Task<double> MeasureFocusWordOverflowAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<double>(
            """
            ids => {
                const display = document.querySelector(`[data-test="${ids.display}"]`);
                const word = document.querySelector(`[data-test="${ids.word}"]`);
                const leading = document.querySelector(`[data-test="${ids.leading}"]`);
                const trailing = document.querySelector(`[data-test="${ids.trailing}"]`);
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
                leading = UiTestIds.Learn.WordLeading,
                trailing = UiTestIds.Learn.WordTrailing,
                word = UiTestIds.Learn.Word
            });
}
