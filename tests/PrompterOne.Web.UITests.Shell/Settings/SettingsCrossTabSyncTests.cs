using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class SettingsCrossTabSyncTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task SettingsAppearance_PropagatesThemeChangesAcrossTabsInSharedContext()
    {
        UiScenarioArtifacts.ResetScenario(BrowserTestConstants.SettingsFlow.CrossTabThemeScenario);

        var pages = await _fixture.NewSharedPagesAsync(BrowserTestConstants.SettingsFlow.SharedContextPageCount);
        var primaryPage = pages[0];
        var secondaryPage = pages[1];

        try
        {
            await OpenAppearancePanelAsync(primaryPage);
            await OpenAppearancePanelAsync(secondaryPage);

            await primaryPage.GetByTestId(UiTestIds.Settings.ThemeOption(BrowserTestConstants.SettingsFlow.LightTheme)).ClickAsync();

            await Expect(primaryPage.Locator("html")).ToHaveAttributeAsync(
                BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
                BrowserTestConstants.SettingsFlow.LightTheme);
            await Expect(secondaryPage.Locator("html")).ToHaveAttributeAsync(
                BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
                BrowserTestConstants.SettingsFlow.LightTheme);
            await Expect(secondaryPage.GetByTestId(UiTestIds.Settings.ThemeOption(BrowserTestConstants.SettingsFlow.LightTheme)))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.State.ActiveAttribute,
                    BrowserTestConstants.State.ActiveValue);

            await UiScenarioArtifacts.CapturePageAsync(
                secondaryPage,
                BrowserTestConstants.SettingsFlow.CrossTabThemeScenario,
                BrowserTestConstants.SettingsFlow.CrossTabThemeSyncedStep);
        }
        finally
        {
            await primaryPage.Context.CloseAsync();
        }
    }

    private static async Task OpenAppearancePanelAsync(IPage page)
    {
        await ShellRouteDriver.OpenSettingsAsync(page);
        await page.GetByTestId(UiTestIds.Settings.NavAppearance).ClickAsync();
        await Expect(page.GetByTestId(UiTestIds.Settings.AppearancePanel)).ToBeVisibleAsync(
            new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
    }
}
