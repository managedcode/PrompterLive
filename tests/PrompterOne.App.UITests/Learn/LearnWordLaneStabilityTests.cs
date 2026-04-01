using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class LearnWordLaneStabilityTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string LongProbeWord = "hype";
    private const string ShortProbeWord = "It";
    private const int StabilityProbeStepLimit = 120;
    private const double MaxShellCenterDriftPx = 2;
    private const double MaxLeftRailEdgeDriftPx = 2;
    private const double MaxRightRailEdgeDriftPx = 2;
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task LearnScreen_QuantumWordLengthChanges_DoNotShiftTheRsvpLane()
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
                Math.Abs(longWordLane.ShellCenterPx - shortWordLane.ShellCenterPx),
                0,
                MaxShellCenterDriftPx);
            Assert.InRange(
                Math.Abs(longWordLane.LeftRailRightPx - shortWordLane.LeftRailRightPx),
                0,
                MaxLeftRailEdgeDriftPx);
            Assert.InRange(
                Math.Abs(longWordLane.RightRailLeftPx - shortWordLane.RightRailLeftPx),
                0,
                MaxRightRailEdgeDriftPx);
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
                const shell = document.querySelector(`[data-testid="${ids.shell}"]`);
                const leftRail = document.querySelector(`[data-testid="${ids.left}"]`);
                const rightRail = document.querySelector(`[data-testid="${ids.right}"]`);
                if (!shell || !leftRail || !rightRail) {
                    return { shellCenterPx: -999, leftRailRightPx: -999, rightRailLeftPx: -999 };
                }

                const shellRect = shell.getBoundingClientRect();
                const leftRailRect = leftRail.getBoundingClientRect();
                const rightRailRect = rightRail.getBoundingClientRect();

                return {
                    shellCenterPx: shellRect.left + (shellRect.width / 2),
                    leftRailRightPx: leftRailRect.right,
                    rightRailLeftPx: rightRailRect.left
                };
            }
            """,
            new
            {
                shell = UiTestIds.Learn.WordShell,
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
        double ShellCenterPx,
        double LeftRailRightPx,
        double RightRailLeftPx);
}
