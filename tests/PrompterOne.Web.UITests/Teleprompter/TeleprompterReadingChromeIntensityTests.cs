using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterReadingChromeIntensityTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    private readonly record struct CssColor(double R, double G, double B, double A);
    private readonly record struct GradientColors(CssColor Start, CssColor End);

    [Test]
    public Task TeleprompterSecurityIncident_ActivePlayback_KeepsProgressAndControlsSubdued() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.TeleprompterFlow.SecurityIncidentChromeScenarioName);

            await page.SetViewportSizeAsync(
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentViewportWidth,
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentViewportHeight);
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterSecurityIncident);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs
            });

            var progressShell = page.GetByTestId(UiTestIds.Teleprompter.Progress);
            var progressLabel = page.GetByTestId(UiTestIds.Teleprompter.ProgressLabel);
            var progressFill = page.GetByTestId(UiTestIds.Teleprompter.ProgressSegmentFill(0));
            var controls = page.GetByTestId(UiTestIds.Teleprompter.Controls);
            var playToggle = page.GetByTestId(UiTestIds.Teleprompter.PlayToggle);

            await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
            await playToggle.ClickAsync();
            await Expect(progressShell).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.Teleprompter.ActiveStateValue);
            await Expect(controls).ToHaveAttributeAsync(
                BrowserTestConstants.State.ActiveAttribute,
                BrowserTestConstants.Teleprompter.ActiveStateValue);
            await ClearChromeHoverAsync(page);
            await page.WaitForTimeoutAsync(BrowserTestConstants.TeleprompterFlow.ReadingChromeSettleDelayMs);

            var progressLabelColor = await ReadCssColorAsync(progressLabel, BrowserTestConstants.TeleprompterFlow.ColorProperty);
            var playToggleColor = await ReadCssColorAsync(playToggle, BrowserTestConstants.TeleprompterFlow.ColorProperty);
            var playToggleBackground = await ReadCssColorAsync(playToggle, BrowserTestConstants.TeleprompterFlow.BackgroundColorProperty);
            var progressFillColors = await ReadGradientColorsAsync(progressFill);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentChromeScenarioName,
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentChromePageStep);
            await UiScenarioArtifacts.CaptureLocatorAsync(
                progressShell,
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentChromeScenarioName,
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentChromeProgressStep);
            await UiScenarioArtifacts.CaptureLocatorAsync(
                controls,
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentChromeScenarioName,
                BrowserTestConstants.TeleprompterFlow.SecurityIncidentChromeControlsStep);

            await Assert.That(HasMaximumChannels(progressLabelColor, BrowserTestConstants.TeleprompterFlow.MaximumMutedProgressLabelChannel)).IsTrue().Because($"Expected the progress label to stay subdued during playback, but got rgba({progressLabelColor.R:0.##}, {progressLabelColor.G:0.##}, {progressLabelColor.B:0.##}, {progressLabelColor.A:0.##}).");
            await Assert.That(HasMaximumChannels(playToggleColor, BrowserTestConstants.TeleprompterFlow.MaximumMutedControlIconChannel)).IsTrue().Because($"Expected the play icon to stay subdued during playback, but got rgba({playToggleColor.R:0.##}, {playToggleColor.G:0.##}, {playToggleColor.B:0.##}, {playToggleColor.A:0.##}).");
            await Assert.That(playToggleBackground.A <= BrowserTestConstants.TeleprompterFlow.MaximumMutedPlayButtonBackgroundAlpha).IsTrue().Because($"Expected the play button background alpha to stay at or below {BrowserTestConstants.TeleprompterFlow.MaximumMutedPlayButtonBackgroundAlpha:0.##}, but got {playToggleBackground.A:0.##}.");
            await Assert.That(HasMaximumChannels(progressFillColors.Start, BrowserTestConstants.TeleprompterFlow.MaximumMutedProgressFillChannel) &&
                HasMaximumChannels(progressFillColors.End, BrowserTestConstants.TeleprompterFlow.MaximumMutedProgressFillChannel)).IsTrue().Because($"Expected the progress fill gradient to stay muted during playback, but got start rgba({progressFillColors.Start.R:0.##}, {progressFillColors.Start.G:0.##}, {progressFillColors.Start.B:0.##}, {progressFillColors.Start.A:0.##}) and end rgba({progressFillColors.End.R:0.##}, {progressFillColors.End.G:0.##}, {progressFillColors.End.B:0.##}, {progressFillColors.End.A:0.##}).");
            await Assert.That(progressFillColors.Start.A <= BrowserTestConstants.TeleprompterFlow.MaximumMutedProgressFillAlpha &&
                progressFillColors.End.A <= BrowserTestConstants.TeleprompterFlow.MaximumMutedProgressFillAlpha).IsTrue().Because($"Expected the progress fill alpha to stay at or below {BrowserTestConstants.TeleprompterFlow.MaximumMutedProgressFillAlpha:0.##}, but got start {progressFillColors.Start.A:0.##} and end {progressFillColors.End.A:0.##}.");
        });

    private static bool HasMaximumChannels(CssColor color, double maximum) =>
        color.R <= maximum && color.G <= maximum && color.B <= maximum;

    private static Task ClearChromeHoverAsync(IPage page) =>
        page.Mouse.MoveAsync(
            BrowserTestConstants.TeleprompterFlow.SecurityIncidentViewportWidth / 2,
            BrowserTestConstants.TeleprompterFlow.SecurityIncidentViewportHeight / 3);

    private static async Task<CssColor> ReadCssColorAsync(ILocator locator, string propertyName) =>
        await locator.EvaluateAsync<CssColor>(
            """
            (element, propertyName) => {
                const value = getComputedStyle(element)[propertyName];
                const match = value.match(/rgba?\(([^)]+)\)/);
                if (!match) {
                    return { r: 0, g: 0, b: 0, a: 0 };
                }

                const parts = match[1].split(',').map(part => Number.parseFloat(part.trim()));
                return {
                    r: parts[0] ?? 0,
                    g: parts[1] ?? 0,
                    b: parts[2] ?? 0,
                    a: parts[3] ?? 1
                };
            }
            """,
            propertyName);

    private static async Task<GradientColors> ReadGradientColorsAsync(ILocator locator) =>
        await locator.EvaluateAsync<GradientColors>(
            """
            (element) => {
                const value = getComputedStyle(element).backgroundImage;
                const matches = [...value.matchAll(/rgba?\(([^)]+)\)/g)];
                const colors = matches.slice(0, 2).map(match => {
                    const parts = match[1].split(',').map(part => Number.parseFloat(part.trim()));
                    return {
                        r: parts[0] ?? 0,
                        g: parts[1] ?? 0,
                        b: parts[2] ?? 0,
                        a: parts[3] ?? 1
                    };
                });

                return {
                    start: colors[0] ?? { r: 0, g: 0, b: 0, a: 0 },
                    end: colors[1] ?? { r: 0, g: 0, b: 0, a: 0 }
                };
            }
            """);
}
