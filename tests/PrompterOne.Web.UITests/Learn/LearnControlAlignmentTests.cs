using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class LearnControlAlignmentTests(StandaloneAppFixture fixture)
{
    private const double MaxCenterlineDeltaPx = 4;
    private const double MaxTransportSymmetryDeltaPx = 4;
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task LearnScreen_QuantumControls_StayCenteredAndSymmetric()
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

            var alignment = await MeasureControlAlignmentAsync(page);

            await Assert.That(Math.Abs(alignment.SpeedCenterPx - alignment.PlayCenterPx)).IsBetween(0,MaxCenterlineDeltaPx);
            await Assert.That(Math.Abs(alignment.PlayCenterPx - alignment.ProgressCenterPx)).IsBetween(0,MaxCenterlineDeltaPx);
            await Assert.That(Math.Abs(alignment.BackLargeOffsetPx - alignment.ForwardLargeOffsetPx)).IsBetween(0,MaxTransportSymmetryDeltaPx);
            await Assert.That(Math.Abs(alignment.BackSmallOffsetPx - alignment.ForwardSmallOffsetPx)).IsBetween(0,MaxTransportSymmetryDeltaPx);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task<ControlAlignmentMeasurement> MeasureControlAlignmentAsync(IPage page)
    {
        var speedDownBounds = await GetRequiredBoundingBoxAsync(page.GetByTestId(UiTestIds.Learn.SpeedDown));
        var speedUpBounds = await GetRequiredBoundingBoxAsync(page.GetByTestId(UiTestIds.Learn.SpeedUp));
        var playBounds = await GetRequiredBoundingBoxAsync(page.GetByTestId(UiTestIds.Learn.PlayToggle));
        var progressBounds = await GetRequiredBoundingBoxAsync(page.GetByTestId(UiTestIds.Learn.ProgressLabel));
        var backLargeBounds = await GetRequiredBoundingBoxAsync(page.GetByTestId(UiTestIds.Learn.StepBackwardLarge));
        var backSmallBounds = await GetRequiredBoundingBoxAsync(page.GetByTestId(UiTestIds.Learn.StepBackward));
        var forwardSmallBounds = await GetRequiredBoundingBoxAsync(page.GetByTestId(UiTestIds.Learn.StepForward));
        var forwardLargeBounds = await GetRequiredBoundingBoxAsync(page.GetByTestId(UiTestIds.Learn.StepForwardLarge));

        var speedCenter = (GetCenterX(speedDownBounds) + GetCenterX(speedUpBounds)) / 2;
        var playCenter = GetCenterX(playBounds);

        return new ControlAlignmentMeasurement(
            speedCenter,
            playCenter,
            GetCenterX(progressBounds),
            playCenter - GetCenterX(backLargeBounds),
            playCenter - GetCenterX(backSmallBounds),
            GetCenterX(forwardSmallBounds) - playCenter,
            GetCenterX(forwardLargeBounds) - playCenter);
    }

    private static double GetCenterX(LayoutBounds bounds) => bounds.X + (bounds.Width / 2);

    private static async Task<LayoutBounds> GetRequiredBoundingBoxAsync(ILocator locator) =>
        await locator.EvaluateAsync<LayoutBounds>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                return {
                    x: rect.x,
                    y: rect.y,
                    width: rect.width,
                    height: rect.height
                };
            }
            """);

    private readonly record struct ControlAlignmentMeasurement(
        double SpeedCenterPx,
        double PlayCenterPx,
        double ProgressCenterPx,
        double BackLargeOffsetPx,
        double BackSmallOffsetPx,
        double ForwardSmallOffsetPx,
        double ForwardLargeOffsetPx);

    private readonly record struct LayoutBounds(double X, double Y, double Width, double Height);
}