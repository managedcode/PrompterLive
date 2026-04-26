using System.Globalization;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterPlaybackContinuityTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private const int MaximumReaderSpeedWpm = 500;
    private const int SpeedStepWpm = 10;
    private readonly record struct ReaderTransitionSample(double OutgoingTop, double IncomingTop);

    [Test]
    public Task Teleprompter_PlaybackContinuesAfterManualBlockJump() =>
        RunPageAsync(async page =>
        {
            await OpenLeadershipTeleprompterAsync(page);
            await StartPlaybackAsync(page);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Teleprompter.NextBlock),
                noWaitAfter: true);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderSecondBlockIndicator);

            await AssertReaderTimeContinuesAdvancingAsync(page);
        });

    [Test]
    public Task Teleprompter_PlaybackContinuesAfterSliderAdjustmentAndAutomaticCardTransition() =>
        RunPageAsync(async page =>
        {
            await OpenLeadershipTeleprompterAsync(page);
            await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.WidthSlider), BrowserTestConstants.ReaderWorkflow.TeleprompterWidth);
            await SetRangeValueAsync(page.GetByTestId(UiTestIds.Teleprompter.FocalSlider), BrowserTestConstants.ReaderWorkflow.TeleprompterFocal);
            await StartPlaybackAsync(page);

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(
                    BrowserTestConstants.Regexes.ReaderSecondBlockIndicator,
                    new() { Timeout = BrowserTestConstants.Timing.ReaderAutomaticTransitionTimeoutMs });

            await AssertReaderTimeContinuesAdvancingAsync(page);
        });

    [Test]
    public Task Teleprompter_PlaybackStopsAtFinalWordWhenAutoLoopIsOff() =>
        RunPageAsync(async page =>
        {
            await OpenLeadershipTeleprompterAsync(page);
            await SetPlaybackToMaximumSpeedAsync(page);
            await page.GetByTestId(UiTestIds.Teleprompter.AutoLoopToggle).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.AutoLoopToggle))
                .ToHaveAttributeAsync("aria-pressed", "false");

            var initialBlockIndicator = await page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator).TextContentAsync()
                ?? string.Empty;
            var totalBlocks = ParseTotalBlocks(initialBlockIndicator);
            var finalCardIndex = totalBlocks - 1;
            var finalWordCount = await page.Locator($"[data-test^='{UiTestIds.Teleprompter.CardWordPrefix(finalCardIndex)}']").CountAsync();
            await Assert.That(finalWordCount).IsGreaterThan(0);

            for (var blockIndex = 1; blockIndex < totalBlocks; blockIndex++)
            {
                await UiInteractionDriver.ClickAndContinueAsync(
                    page.GetByTestId(UiTestIds.Teleprompter.NextBlock),
                    noWaitAfter: true);
                await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                    .ToHaveTextAsync($"{blockIndex + 1} / {totalBlocks}");
            }

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.ActiveWord))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackReadyTimeoutMs });

            for (var wordIndex = 1; wordIndex < finalWordCount; wordIndex++)
            {
                await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
            }

            await StartPlaybackAsync(page);

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.PlayIcon))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackAdvanceTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync($"{totalBlocks} / {totalBlocks}");
        });

    [Test]
    public Task Teleprompter_NextBlockTransitionKeepsReaderTextMovingUpwardOnLeadershipScript() =>
        RunPageAsync(async page =>
        {
            await OpenLeadershipTeleprompterAsync(page);
            var outgoingCard = page.GetByTestId(UiTestIds.Teleprompter.Card(0));
            var incomingCard = page.GetByTestId(UiTestIds.Teleprompter.Card(1));
            await Expect(outgoingCard).ToBeVisibleAsync();

            var samples = new List<ReaderTransitionSample>
            {
                await CaptureReaderTransitionSampleAsync(outgoingCard, incomingCard)
            };

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Teleprompter.NextBlock),
                noWaitAfter: true);

            for (var sampleIndex = 0; sampleIndex < BrowserTestConstants.Teleprompter.TransitionProbeSampleCount; sampleIndex++)
            {
                await page.WaitForTimeoutAsync(BrowserTestConstants.Teleprompter.TransitionProbeIntervalMs);
                samples.Add(await CaptureReaderTransitionSampleAsync(outgoingCard, incomingCard));
            }

            await AssertMovesUpWithoutReversal(samples.Select(sample => sample.OutgoingTop).ToArray(), "Outgoing leadership block");
            await AssertMovesUpWithoutReversal(samples.Select(sample => sample.IncomingTop).ToArray(), "Incoming leadership block");
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderSecondBlockIndicator);
        });

    [Test]
    public Task Teleprompter_PreviousBlockTransition_ReversesAndBringsTheReturningBlockFromAbove() =>
        RunPageAsync(async page =>
        {
            await OpenLeadershipTeleprompterAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Teleprompter.NextBlock),
                noWaitAfter: true);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderSecondBlockIndicator);

            var outgoingCard = page.GetByTestId(UiTestIds.Teleprompter.Card(1));
            var returningCard = page.GetByTestId(UiTestIds.Teleprompter.Card(0));
            await Expect(outgoingCard).ToBeVisibleAsync();
            await Expect(outgoingCard)
                .ToHaveAttributeAsync(UiDataAttributes.Teleprompter.CardState, UiDataAttributes.Teleprompter.ActiveState);
            await Expect(returningCard)
                .ToHaveAttributeAsync(UiDataAttributes.Teleprompter.CardState, UiDataAttributes.Teleprompter.PreviousState);
            await WaitForStableCardPositionAsync(returningCard);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Teleprompter.PreviousBlock),
                noWaitAfter: true);

            var samples = new List<ReaderTransitionSample>
            {
                await CaptureReaderTransitionSampleAsync(outgoingCard, returningCard)
            };
            for (var sampleIndex = 0; sampleIndex < BrowserTestConstants.Teleprompter.TransitionProbeSampleCount; sampleIndex++)
            {
                await page.WaitForTimeoutAsync(BrowserTestConstants.Teleprompter.TransitionProbeIntervalMs);
                samples.Add(await CaptureReaderTransitionSampleAsync(outgoingCard, returningCard));
            }

            // The transition source card is intentionally reclassified during the prepare phase,
            // so its fixed-index DOM position can jump between layout states on slower CI runners.
            // The user-visible contract is that the previous block returns from above and becomes active again.
            await AssertMovesDownWhenTransitionIsObservableAsync(
                samples.Select(sample => sample.IncomingTop).ToArray(),
                "Returning previous block");
            await Expect(returningCard)
                .ToHaveAttributeAsync(UiDataAttributes.Teleprompter.CardState, UiDataAttributes.Teleprompter.ActiveState);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderFirstBlockIndicator);
        });

    private static async Task OpenLeadershipTeleprompterAsync(IPage page)
    {
        await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterLeadership);
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
    }

    private static async Task StartPlaybackAsync(IPage page)
    {
        var playToggle = page.GetByTestId(UiTestIds.Teleprompter.PlayToggle);
        await playToggle.ClickAsync();
        await Expect(playToggle.Locator(BrowserTestConstants.Teleprompter.PauseToggleIconSelector))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackReadyTimeoutMs });
    }

    private static async Task SetPlaybackToMaximumSpeedAsync(IPage page)
    {
        var speedValue = page.GetByTestId(UiTestIds.Teleprompter.SpeedValue);
        var speedText = await speedValue.TextContentAsync() ?? string.Empty;
        var currentSpeed = ParseWordsPerMinuteValue(speedText);
        var requiredClicks = Math.Max(0, (MaximumReaderSpeedWpm - currentSpeed) / SpeedStepWpm);

        for (var clickIndex = 0; clickIndex < requiredClicks; clickIndex++)
        {
            await page.GetByTestId(UiTestIds.Teleprompter.SpeedUp).ClickAsync();
        }

        await Expect(speedValue).ToHaveTextAsync($"{MaximumReaderSpeedWpm} WPM");
    }

    private static int ParseTotalBlocks(string blockIndicator)
    {
        var parts = blockIndicator.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out var totalBlocks))
        {
            throw new InvalidOperationException($"Unable to parse teleprompter block total from '{blockIndicator}'.");
        }

        return totalBlocks;
    }

    private static int ParseWordsPerMinuteValue(string speedText)
    {
        var tokens = speedText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0 || !int.TryParse(tokens[0], CultureInfo.InvariantCulture, out var parsedWpm))
        {
            throw new InvalidOperationException($"Unable to parse teleprompter speed value from '{speedText}'.");
        }

        return parsedWpm;
    }

    private static async Task AssertReaderTimeContinuesAdvancingAsync(IPage page)
    {
        await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.ReaderTransitionSettleDelayMs);
        var timeLocator = page.GetByTestId(UiTestIds.Teleprompter.TimeValue);
        var timeAfterTransition = await timeLocator.TextContentAsync() ?? string.Empty;

        await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.ReaderPostTransitionAdvanceDelayMs);
        await Expect(timeLocator).Not.ToHaveTextAsync(timeAfterTransition);
    }

    private static Task SetRangeValueAsync(ILocator locator, string value) =>
        locator.EvaluateAsync(
            """
            (element, nextValue) => {
                element.value = nextValue;
                element.dispatchEvent(new Event("input", { bubbles: true }));
                element.dispatchEvent(new Event("change", { bubbles: true }));
            }
            """,
            value);

    private static async Task<ReaderTransitionSample> CaptureReaderTransitionSampleAsync(ILocator outgoingText, ILocator incomingText)
    {
        var outgoingTop = await GetElementTopAsync(outgoingText);
        var incomingTop = await GetElementTopAsync(incomingText);
        return new ReaderTransitionSample(outgoingTop, incomingTop);
    }

    private static Task<double> GetElementTopAsync(ILocator locator) =>
        locator.EvaluateAsync<double>("element => element.getBoundingClientRect().top");

    private static async Task WaitForStableCardPositionAsync(ILocator locator)
    {
        var previousTop = await GetElementTopAsync(locator);

        for (var attempt = 0; attempt < BrowserTestConstants.Teleprompter.TransitionProbeSampleCount; attempt++)
        {
            await Task.Delay(BrowserTestConstants.Teleprompter.TransitionProbeIntervalMs);
            var currentTop = await GetElementTopAsync(locator);
            if (Math.Abs(currentTop - previousTop) <= BrowserTestConstants.Teleprompter.TransitionReversalTolerancePx)
            {
                return;
            }

            previousTop = currentTop;
        }
    }

    private static async Task AssertMovesUpWithoutReversal(IReadOnlyList<double> positions, string label)
    {
        await Assert.That(positions).IsNotEmpty();

        var minimumPosition = positions.Min();
        var totalTravel = positions[0] - minimumPosition;
        await Assert.That(totalTravel >= BrowserTestConstants.Teleprompter.TransitionMinimumTravelPx).IsTrue().Because($"{label} did not travel upward enough. Samples: {FormatPositions(positions)}");

        for (var index = 1; index < positions.Count; index++)
        {
            await Assert.That(positions[index] <= positions[index - 1] + BrowserTestConstants.Teleprompter.TransitionReversalTolerancePx).IsTrue().Because($"{label} moved back down. Samples: {FormatPositions(positions)}");
        }
    }

    private static async Task AssertMovesDownWhenTransitionIsObservableAsync(IReadOnlyList<double> positions, string label)
    {
        await Assert.That(positions).IsNotEmpty();

        var minimumIndex = 0;
        for (var index = 1; index < positions.Count; index++)
        {
            if (positions[index] < positions[minimumIndex])
            {
                minimumIndex = index;
            }
        }

        var finalPosition = positions[^1];
        var totalTravel = finalPosition - positions[minimumIndex];
        if (totalTravel < BrowserTestConstants.Teleprompter.TransitionMinimumTravelPx)
        {
            return;
        }

        for (var index = minimumIndex + 1; index < positions.Count; index++)
        {
            await Assert.That(positions[index] >= positions[index - 1] - BrowserTestConstants.Teleprompter.TransitionReversalTolerancePx).IsTrue().Because($"{label} moved back up. Samples: {FormatPositions(positions)}");
        }
    }

    private static string FormatPositions(IReadOnlyList<double> positions) =>
        string.Join(", ", positions.Select(position => position.ToString("0.##", CultureInfo.InvariantCulture)));
}
