using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class LearnWordLaneStabilityTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string LongProbeWord = "hype";
    private const string ShortProbeWord = "It";
    private const int StabilityProbeStepLimit = 120;
    private const double MaxOrpCenterDriftPx = 2;
    private const double MaxVisibleContextGapDriftPx = 2;
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task LearnScreen_QuantumWordLengthChanges_KeepTheOrpAnchorAndVisibleContextGapsStable()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.SetViewportSizeAsync(
                BrowserTestConstants.Learn.QuantumViewportWidth,
                BrowserTestConstants.Learn.QuantumViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.LearnQuantum);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await StepUntilWordAsync(page, LongProbeWord, StabilityProbeStepLimit);
            var longWordLane = await MeasureRsvpLaneAsync(page);

            await page.GetByTestId(UiTestIds.Learn.StepForward).ClickAsync();
            await ExpectFocusWordAsync(page, ShortProbeWord);
            var shortWordLane = await MeasureRsvpLaneAsync(page);

            Assert.InRange(
                Math.Abs(longWordLane.OrpCenterPx - shortWordLane.OrpCenterPx),
                0,
                MaxOrpCenterDriftPx);
            Assert.InRange(
                Math.Abs(longWordLane.LeftVisibleGapPx - shortWordLane.LeftVisibleGapPx),
                0,
                MaxVisibleContextGapDriftPx);
            Assert.InRange(
                Math.Abs(longWordLane.RightVisibleGapPx - shortWordLane.RightVisibleGapPx),
                0,
                MaxVisibleContextGapDriftPx);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static Task<RsvpLaneMeasurement> MeasureRsvpLaneAsync(IPage page) =>
        page.EvaluateAsync<RsvpLaneMeasurement>(
            """
            ids => {
                const word = document.querySelector(`[data-testid="${ids.word}"]`);
                const orp = word?.querySelector('.orp');
                const leftRail = document.querySelector(`[data-testid="${ids.left}"]`);
                const rightRail = document.querySelector(`[data-testid="${ids.right}"]`);
                const leftWord = leftRail?.lastElementChild;
                const rightWord = rightRail?.firstElementChild;
                if (!word || !orp || !leftWord || !rightWord) {
                    return { orpCenterPx: -999, leftVisibleGapPx: -999, rightVisibleGapPx: -999 };
                }

                const wordRect = word.getBoundingClientRect();
                const orpRect = orp.getBoundingClientRect();
                const leftWordRect = leftWord.getBoundingClientRect();
                const rightWordRect = rightWord.getBoundingClientRect();

                return {
                    orpCenterPx: orpRect.left + (orpRect.width / 2),
                    leftVisibleGapPx: wordRect.left - leftWordRect.right,
                    rightVisibleGapPx: rightWordRect.left - wordRect.right
                };
            }
            """,
            new
            {
                word = UiTestIds.Learn.Word,
                left = UiTestIds.Learn.ContextLeft,
                right = UiTestIds.Learn.ContextRight
            });

    private static async Task StepUntilWordAsync(IPage page, string targetWord, int stepLimit)
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

        Assert.Fail($"Did not reach the Learn probe word '{targetWord}' within {stepLimit} steps.");
    }

    private static async Task ExpectFocusWordAsync(IPage page, string expectedWord)
    {
        await Expect(page.GetByTestId(UiTestIds.Learn.Word))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

        var actualWord = await ReadFocusWordAsync(page);
        Assert.Equal(expectedWord, actualWord, ignoreCase: true);
    }

    private static async Task<string> ReadFocusWordAsync(IPage page)
    {
        var rawWord = await page.GetByTestId(UiTestIds.Learn.Word).TextContentAsync();
        return string.Concat((rawWord ?? string.Empty).Where(character => !char.IsWhiteSpace(character)));
    }

    private readonly record struct RsvpLaneMeasurement(
        double OrpCenterPx,
        double LeftVisibleGapPx,
        double RightVisibleGapPx);
}
