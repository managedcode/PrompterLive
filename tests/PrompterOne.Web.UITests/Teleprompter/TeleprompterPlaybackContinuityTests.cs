using System.Globalization;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterPlaybackContinuityTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture){
    private readonly record struct ReaderTransitionSample(double OutgoingTop, double IncomingTop);

    [Test]
    public Task Teleprompter_PlaybackContinuesAfterManualBlockJump() =>
        RunPageAsync(async page =>
        {
            await OpenLeadershipTeleprompterAsync(page);
            await StartPlaybackAsync(page);

            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}"))
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

            await Expect(page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}"))
                .ToHaveTextAsync(
                    BrowserTestConstants.Regexes.ReaderSecondBlockIndicator,
                    new() { Timeout = BrowserTestConstants.Timing.ReaderAutomaticTransitionTimeoutMs });

            await AssertReaderTimeContinuesAdvancingAsync(page);
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

            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();

            for (var sampleIndex = 0; sampleIndex < BrowserTestConstants.Teleprompter.TransitionProbeSampleCount; sampleIndex++)
            {
                await page.WaitForTimeoutAsync(BrowserTestConstants.Teleprompter.TransitionProbeIntervalMs);
                samples.Add(await CaptureReaderTransitionSampleAsync(outgoingCard, incomingCard));
            }

            await AssertMovesUpWithoutReversal(samples.Select(sample => sample.OutgoingTop).ToArray(), "Outgoing leadership block");
            await AssertMovesUpWithoutReversal(samples.Select(sample => sample.IncomingTop).ToArray(), "Incoming leadership block");
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}"))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderSecondBlockIndicator);
        });

    [Test]
    public Task Teleprompter_PreviousBlockTransition_ReversesAndBringsTheReturningBlockFromAbove() =>
        RunPageAsync(async page =>
        {
            await OpenLeadershipTeleprompterAsync(page);
            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}"))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderSecondBlockIndicator);
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.ReaderTransitionSettleDelayMs);

            var outgoingCard = page.GetByTestId(UiTestIds.Teleprompter.Card(1));
            var returningCard = page.GetByTestId(UiTestIds.Teleprompter.Card(0));
            await Expect(outgoingCard).ToBeVisibleAsync();

            var samples = new List<ReaderTransitionSample>
            {
                await CaptureReaderTransitionSampleAsync(outgoingCard, returningCard)
            };

            await page.GetByTestId(UiTestIds.Teleprompter.PreviousBlock).ClickAsync();

            for (var sampleIndex = 0; sampleIndex < BrowserTestConstants.Teleprompter.TransitionProbeSampleCount; sampleIndex++)
            {
                await page.WaitForTimeoutAsync(BrowserTestConstants.Teleprompter.TransitionProbeIntervalMs);
                samples.Add(await CaptureReaderTransitionSampleAsync(outgoingCard, returningCard));
            }

            await AssertMovesDownWithoutReversal(samples.Select(sample => sample.OutgoingTop).ToArray(), "Outgoing current block");
            await AssertMovesDownWithoutReversal(samples.Select(sample => sample.IncomingTop).ToArray(), "Returning previous block");
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}"))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderFirstBlockIndicator);
        });

    private static async Task OpenLeadershipTeleprompterAsync(IPage page)
    {
        await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterLeadership);
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

    private static async Task AssertReaderTimeContinuesAdvancingAsync(IPage page)
    {
        await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.ReaderTransitionSettleDelayMs);
        var timeLocator = page.Locator($"#{UiDomIds.Teleprompter.Time}");
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

    private static async Task AssertMovesDownWithoutReversal(IReadOnlyList<double> positions, string label)
    {
        await Assert.That(positions).IsNotEmpty();

        var maximumPosition = positions.Max();
        var totalTravel = maximumPosition - positions[0];
        await Assert.That(totalTravel >= BrowserTestConstants.Teleprompter.TransitionMinimumTravelPx).IsTrue().Because($"{label} did not travel downward enough. Samples: {FormatPositions(positions)}");

        for (var index = 1; index < positions.Count; index++)
        {
            await Assert.That(positions[index] >= positions[index - 1] - BrowserTestConstants.Teleprompter.TransitionReversalTolerancePx).IsTrue().Because($"{label} moved back up. Samples: {FormatPositions(positions)}");
        }
    }

    private static string FormatPositions(IReadOnlyList<double> positions) =>
        string.Join(", ", positions.Select(position => position.ToString("0.##", CultureInfo.InvariantCulture)));
}
