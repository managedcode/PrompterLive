using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class HeaderPlaybackLaunchFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    [Test]
    public Task LibraryCardPlaybackActions_OpenScopedReaderRoutes_AndHeaderBackReturnsToEditor() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.AppShellFlow.PlaybackLaunchScenario);

            await page.SetViewportSizeAsync(
                BrowserTestConstants.ResponsiveLayout.IphoneMediumWidth,
                BrowserTestConstants.ResponsiveLayout.IphoneMediumHeight);

            await ShellRouteDriver.OpenLibraryAsync(page);

            await Expect(page.GetByTestId(UiTestIds.Library.CardLearn(BrowserTestConstants.Scripts.QuantumId)))
                .ToHaveTextAsync(BrowserTestConstants.Library.PracticeActionLabel);
            await Expect(page.GetByTestId(UiTestIds.Library.CardRead(BrowserTestConstants.Scripts.QuantumId)))
                .ToHaveTextAsync(BrowserTestConstants.Library.TeleprompterActionLabel);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CardLearn(BrowserTestConstants.Scripts.QuantumId)),
                noWaitAfter: true);
            await PlaybackRouteDriver.WaitForLearnReadyAsync(page, BrowserTestConstants.Routes.LearnQuantum);
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.PlaybackLaunchScenario,
                BrowserTestConstants.AppShellFlow.LearnLaunchStep);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.Back),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.PlaybackLaunchScenario,
                BrowserTestConstants.AppShellFlow.LearnBackStep);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.Home),
                noWaitAfter: true);
            await ShellRouteDriver.WaitForLibraryReadyAsync(page, AppRoutes.Library);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Library.CardRead(BrowserTestConstants.Scripts.QuantumId)),
                noWaitAfter: true);
            await PlaybackRouteDriver.WaitForTeleprompterReadyAsync(page, BrowserTestConstants.Routes.TeleprompterQuantum);
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.PlaybackLaunchScenario,
                BrowserTestConstants.AppShellFlow.TeleprompterLaunchStep);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.Back),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.PlaybackLaunchScenario,
                BrowserTestConstants.AppShellFlow.TeleprompterBackStep);
        });

    [Test]
    public Task GoLiveSettingsFlow_HeaderBackReturnsToOriginStudioRoute() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.AppShellFlow.SettingsOriginScenario);

            await page.SetViewportSizeAsync(
                BrowserTestConstants.ResponsiveLayout.IphoneMediumWidth,
                BrowserTestConstants.ResponsiveLayout.IphoneMediumHeight);

            await ShellRouteDriver.OpenGoLiveRouteAsync(page, BrowserTestConstants.Routes.GoLiveDemo);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.GoLive.OpenSettings),
                noWaitAfter: true);
            await BrowserRouteDriver.WaitForRouteAsync(page, BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.SettingsTitle);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.SettingsOriginScenario,
                BrowserTestConstants.AppShellFlow.SettingsLaunchStep);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Header.Back),
                noWaitAfter: true);
            await ShellRouteDriver.WaitForGoLiveReadyAsync(page, BrowserTestConstants.Routes.GoLiveDemo);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.SettingsOriginScenario,
                BrowserTestConstants.AppShellFlow.SettingsBackStep);
        });
}
