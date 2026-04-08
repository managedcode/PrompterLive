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

            await page.GotoAsync(BrowserTestConstants.Routes.Library);
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Library.CardLearn(BrowserTestConstants.Scripts.QuantumId)).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.LearnQuantum));
            await Expect(page.GetByTestId(UiTestIds.Learn.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.PlaybackLaunchScenario,
                BrowserTestConstants.AppShellFlow.LearnLaunchStep);

            await page.GetByTestId(UiTestIds.Header.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.EditorQuantum));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.PlaybackLaunchScenario,
                BrowserTestConstants.AppShellFlow.LearnBackStep);

            await page.GetByTestId(UiTestIds.Header.Home).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Library));
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Library.CardRead(BrowserTestConstants.Scripts.QuantumId)).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.TeleprompterQuantum));
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.Scripts.QuantumTitle);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.PlaybackLaunchScenario,
                BrowserTestConstants.AppShellFlow.TeleprompterLaunchStep);

            await page.GetByTestId(UiTestIds.Header.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.EditorQuantum));
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
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

            await page.GotoAsync(BrowserTestConstants.Routes.GoLiveDemo);
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.GoLive.OpenSettings).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.Settings));
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Header.Title))
                .ToHaveTextAsync(BrowserTestConstants.AppShellFlow.SettingsTitle);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.SettingsOriginScenario,
                BrowserTestConstants.AppShellFlow.SettingsLaunchStep);

            await page.GetByTestId(UiTestIds.Header.Back).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(BrowserTestConstants.Routes.GoLiveDemo));
            await Expect(page.GetByTestId(UiTestIds.GoLive.Page)).ToBeVisibleAsync();
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.AppShellFlow.SettingsOriginScenario,
                BrowserTestConstants.AppShellFlow.SettingsBackStep);
        });
}
