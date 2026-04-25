using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class SettingsLayoutFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private static readonly ResponsiveViewport[] SettingsNavigationReachabilityViewports =
    [
        new("iphone-medium-portrait", BrowserTestConstants.ResponsiveLayout.IphoneMediumWidth, BrowserTestConstants.ResponsiveLayout.IphoneMediumHeight),
        new("ipad-mini-landscape", BrowserTestConstants.ResponsiveLayout.IpadMiniHeight, BrowserTestConstants.ResponsiveLayout.IpadMiniWidth)
    ];

    private sealed class ElementBounds
    {
        public double Left { get; set; }

        public double Right { get; set; }

        public double Width { get; set; }
    }

    [Test]
    public Task SettingsDesktopLayout_ActivePanelUsesMostOfTheMainWorkspaceWidth() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.SettingsFlow.DesktopLayoutScenario);

            await page.SetViewportSizeAsync(
                BrowserTestConstants.Viewport.DefaultWidth,
                BrowserTestConstants.Viewport.DefaultHeight);

            await ShellRouteDriver.OpenSettingsAsync(page);
            await UiInteractionDriver.ClickAndContinueAsync(page.GetByTestId(UiTestIds.Settings.NavAi));

            var main = page.GetByTestId(UiTestIds.Settings.Main);
            var aiPanel = page.GetByTestId(UiTestIds.Settings.AiPanel);

            await Expect(main).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(aiPanel).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var mainBounds = await ReadBoundsAsync(main);
            var panelBounds = await ReadBoundsAsync(aiPanel);

            var panelToMainRatio = panelBounds.Width / mainBounds.Width;
            var leftGap = panelBounds.Left - mainBounds.Left;
            var rightGap = mainBounds.Right - panelBounds.Right;

            await Assert.That(panelToMainRatio >= BrowserTestConstants.SettingsFlow.MinimumDesktopPanelToMainWidthRatio)
                .IsTrue()
                .Because($"Expected the active settings panel to use most of the main workspace width, but the ratio was {panelToMainRatio:0.###}.");
            await Assert.That(leftGap <= BrowserTestConstants.SettingsFlow.MaximumDesktopPanelSideGapPx)
                .IsTrue()
                .Because($"Expected the left gap between the settings main workspace and active panel to stay compact, but it was {leftGap:0.###} px.");
            await Assert.That(rightGap <= BrowserTestConstants.SettingsFlow.MaximumDesktopPanelSideGapPx)
                .IsTrue()
                .Because($"Expected the right gap between the settings main workspace and active panel to stay compact, but it was {rightGap:0.###} px.");

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.SettingsFlow.DesktopLayoutScenario,
                BrowserTestConstants.SettingsFlow.DesktopLayoutStep);
        });

    [Test]
    public Task SettingsNavigation_PrioritizesRecordingBeforeCaptureAndStreamingControls() =>
        RunPageAsync(async page =>
        {
            await ShellRouteDriver.OpenSettingsAsync(page);

            var navOrder = await ReadSettingsNavigationOrderAsync(page);

            await Assert.That(IndexOf(navOrder, UiTestIds.Settings.NavRecording))
                .IsLessThan(IndexOf(navOrder, UiTestIds.Settings.NavCameras));
            await Assert.That(IndexOf(navOrder, UiTestIds.Settings.NavRecording))
                .IsLessThan(IndexOf(navOrder, UiTestIds.Settings.NavMics));
            await Assert.That(IndexOf(navOrder, UiTestIds.Settings.NavRecording))
                .IsLessThan(IndexOf(navOrder, UiTestIds.Settings.NavStreaming));
        });

    [Test]
    public Task SettingsNavigation_RecordingPanelRemainsReachableAcrossResponsiveLayouts() =>
        RunPageAsync(async page =>
        {
            foreach (var viewport in SettingsNavigationReachabilityViewports)
            {
                await page.SetViewportSizeAsync(viewport.Width, viewport.Height);
                await ShellRouteDriver.OpenSettingsAsync(page, viewport.Name);

                var recordingNav = page.GetByTestId(UiTestIds.Settings.NavRecording);
                await recordingNav.ScrollIntoViewIfNeededAsync();
                await UiInteractionDriver.ClickAndContinueAsync(recordingNav);

                await Expect(page.GetByTestId(UiTestIds.Settings.RecordingPanel)).ToBeVisibleAsync(
                    new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            }
        });

    private static Task<ElementBounds> ReadBoundsAsync(ILocator locator) =>
        locator.EvaluateAsync<ElementBounds>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                return {
                    left: rect.left,
                    right: rect.right,
                    width: rect.width
                };
            }
            """);

    private static Task<string[]> ReadSettingsNavigationOrderAsync(IPage page) =>
        page.EvaluateAsync<string[]>(
            """
            () => Array.from(document.querySelectorAll('.set-nav [data-test]'))
                .map(element => element.getAttribute('data-test') ?? '')
                .filter(Boolean)
            """);

    private static int IndexOf(IReadOnlyList<string> values, string value)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal))
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Expected settings navigation item '{value}' to be present.");
    }
}
