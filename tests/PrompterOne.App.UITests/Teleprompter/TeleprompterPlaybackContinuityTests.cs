using System.Globalization;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class TeleprompterPlaybackContinuityTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private readonly record struct ReaderTransitionSample(double OutgoingTop, double IncomingTop);

    [Fact]
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

    [Fact]
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

    [Fact]
    public Task Teleprompter_NextBlockTransitionKeepsReaderTextMovingUpwardOnLeadershipScript() =>
        RunPageAsync(async page =>
        {
            await OpenLeadershipTeleprompterAsync(page);
            var outgoingText = page.GetByTestId(UiTestIds.Teleprompter.CardText(0));
            var incomingText = page.GetByTestId(UiTestIds.Teleprompter.CardText(1));
            await Expect(outgoingText).ToBeVisibleAsync();

            var samples = new List<ReaderTransitionSample>
            {
                await CaptureReaderTransitionSampleAsync(outgoingText, incomingText)
            };

            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();

            for (var sampleIndex = 0; sampleIndex < BrowserTestConstants.Teleprompter.TransitionProbeSampleCount; sampleIndex++)
            {
                await page.WaitForTimeoutAsync(BrowserTestConstants.Teleprompter.TransitionProbeIntervalMs);
                samples.Add(await CaptureReaderTransitionSampleAsync(outgoingText, incomingText));
            }

            AssertMovesUpWithoutReversal(samples.Select(sample => sample.OutgoingTop).ToArray(), "Outgoing leadership block");
            AssertMovesUpWithoutReversal(samples.Select(sample => sample.IncomingTop).ToArray(), "Incoming leadership block");
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}"))
                .ToHaveTextAsync(BrowserTestConstants.Regexes.ReaderSecondBlockIndicator);
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

    private static void AssertMovesUpWithoutReversal(IReadOnlyList<double> positions, string label)
    {
        Assert.NotEmpty(positions);

        var minimumPosition = positions.Min();
        var totalTravel = positions[0] - minimumPosition;
        Assert.True(
            totalTravel >= BrowserTestConstants.Teleprompter.TransitionMinimumTravelPx,
            $"{label} did not travel upward enough. Samples: {FormatPositions(positions)}");

        for (var index = 1; index < positions.Count; index++)
        {
            Assert.True(
                positions[index] <= positions[index - 1] + BrowserTestConstants.Teleprompter.TransitionReversalTolerancePx,
                $"{label} moved back down. Samples: {FormatPositions(positions)}");
        }
    }

    private static string FormatPositions(IReadOnlyList<double> positions) =>
        string.Join(", ", positions.Select(position => position.ToString("0.##", CultureInfo.InvariantCulture)));
}
