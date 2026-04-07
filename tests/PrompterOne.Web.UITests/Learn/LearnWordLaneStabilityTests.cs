using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LearnWordLaneStabilityTests(StandaloneAppFixture fixture)
{
    private const string LongProbeWord = "hype";
    private const string ShortProbeWord = "It";
    private const int StabilityProbeStepLimit = 120;
    private const double MaxOrpCenterDriftPx = 2;
    private const double MaxVisibleContextGapDriftPx = 2;
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
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

            await Assert.That(Math.Abs(longWordLane.OrpCenterPx - shortWordLane.OrpCenterPx)).IsBetween(0, MaxOrpCenterDriftPx);
            await Assert.That(Math.Abs(longWordLane.LeftVisibleGapPx - shortWordLane.LeftVisibleGapPx)).IsBetween(0, MaxVisibleContextGapDriftPx);
            await Assert.That(Math.Abs(longWordLane.RightVisibleGapPx - shortWordLane.RightVisibleGapPx)).IsBetween(0, MaxVisibleContextGapDriftPx);
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
                const word = document.querySelector(`[data-test="${ids.word}"]`);
                const orp = document.querySelector(`[data-test="${ids.orp}"]`);
                const leftRail = document.querySelector(`[data-test="${ids.left}"]`);
                const rightRail = document.querySelector(`[data-test="${ids.right}"]`);
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
                orp = UiTestIds.Learn.WordOrp,
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

        Assert.Fail("Unexpected execution path.");
    }

    private static async Task ExpectFocusWordAsync(IPage page, string expectedWord)
    {
        await Expect(page.GetByTestId(UiTestIds.Learn.Word))
            .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

        var actualWord = await ReadFocusWordAsync(page);
        // TODO: TUnit migration - xUnit Assert.Equal had additional argument(s) (ignoreCase: true) that could not be converted.
        await Assert.That(actualWord).IsEqualTo(expectedWord);
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
