using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Storage.Cloud;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class SettingsCloudStorageFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
    private readonly record struct CssColor(double R, double G, double B, double A);

    [Fact]
    public Task SettingsCloudStorage_PersistsDropboxDraftAcrossReload() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.SettingsFlow.CloudStorageScenario);

            await page.GotoAsync(
                BrowserTestConstants.Routes.Settings,
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync(
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Settings.CloudPanel)).ToBeVisibleAsync(
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await EnsureToggleOffAsync(page.GetByTestId(UiTestIds.Settings.CloudAutoSyncOnSave));

            await SettingsSelectDriver.SelectByValueAsync(
                page,
                UiTestIds.Settings.CloudDefaultProvider,
                CloudStorageProviderIds.Dropbox);
            await Expect(page.GetByTestId(UiTestIds.Settings.CloudAutoSyncOnSave))
                .Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            var accountLabelField = page.GetByTestId(
                UiTestIds.Settings.CloudProviderField(CloudStorageProviderIds.Dropbox, CloudStorageFieldIds.AccountLabel));
            await Expect(accountLabelField).ToBeVisibleAsync();
            await accountLabelField.FillAsync(BrowserTestConstants.SettingsFlow.DropboxLabel);
            await page.GetByTestId(UiTestIds.Settings.CloudProviderConnect(CloudStorageProviderIds.Dropbox))
                .ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Settings.CloudProviderMessage(CloudStorageProviderIds.Dropbox)))
                .ToHaveTextAsync(BrowserTestConstants.SettingsFlow.DropboxValidationMessage);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.SettingsFlow.CloudStorageScenario,
                BrowserTestConstants.SettingsFlow.CloudStorageConfiguredStep);

            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync(
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Settings.CloudDefaultProvider))
                .ToHaveAttributeAsync(BrowserTestConstants.Html.ValueAttribute, CloudStorageProviderIds.Dropbox);
            await Expect(page.GetByTestId(UiTestIds.Settings.CloudProviderSubtitle(CloudStorageProviderIds.Dropbox)))
                .ToHaveTextAsync(BrowserTestConstants.SettingsFlow.DropboxLabel);
            await Expect(page.GetByTestId(UiTestIds.Settings.CloudAutoSyncOnSave))
                .Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
            await Expect(page.GetByTestId(UiTestIds.Settings.CloudProviderMessage(CloudStorageProviderIds.Dropbox)))
                .ToHaveTextAsync(BrowserTestConstants.SettingsFlow.DropboxValidationMessage);

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.SettingsFlow.CloudStorageScenario,
                BrowserTestConstants.SettingsFlow.CloudStorageReloadedStep);
        });

    [Fact]
    public Task SettingsCloudStorage_LightTheme_UsesReadableLightSurfaces() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(
                BrowserTestConstants.Routes.Settings,
                new() { WaitUntil = WaitUntilState.NetworkIdle });
            await Expect(page.GetByTestId(UiTestIds.Settings.Page)).ToBeVisibleAsync(
                new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Settings.NavAppearance).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.AppearancePanel)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Settings.ThemeOption(BrowserTestConstants.SettingsFlow.LightTheme)).ClickAsync();
            await Expect(page.Locator("html")).ToHaveAttributeAsync(
                BrowserTestConstants.SettingsFlow.HtmlThemeAttribute,
                BrowserTestConstants.SettingsFlow.LightTheme);

            await page.GetByTestId(UiTestIds.Settings.NavCloud).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.CloudPanel)).ToBeVisibleAsync();

            var oneDriveCard = page.GetByTestId(UiTestIds.Settings.CloudProviderCard(CloudStorageProviderIds.OneDrive));
            var defaultProvider = page.GetByTestId(UiTestIds.Settings.CloudDefaultProvider);
            var defaultProviderPanel = page.GetByTestId(UiTestIds.Settings.SelectPanel(UiTestIds.Settings.CloudDefaultProvider));
            var defaultProviderOption = page.GetByTestId(
                UiTestIds.Settings.SelectOption(UiTestIds.Settings.CloudDefaultProvider, CloudStorageProviderIds.OneDrive));
            var accountLabelField = page.GetByTestId(
                UiTestIds.Settings.CloudProviderField(CloudStorageProviderIds.OneDrive, CloudStorageFieldIds.AccountLabel));
            var oneDriveSubtitle = page.GetByTestId(UiTestIds.Settings.CloudProviderSubtitle(CloudStorageProviderIds.OneDrive));

            await Expect(oneDriveCard).ToBeVisibleAsync();
            await Expect(defaultProvider).ToBeVisibleAsync();
            await Expect(accountLabelField).ToBeVisibleAsync();
            await Expect(oneDriveSubtitle).ToBeVisibleAsync();

            await defaultProvider.ClickAsync();
            await Expect(defaultProviderPanel).ToBeVisibleAsync();
            await Expect(defaultProviderOption).ToBeVisibleAsync();

            var cardBackground = await ReadCssColorAsync(oneDriveCard, "backgroundColor");
            var selectBackground = await ReadCssColorAsync(defaultProvider, "backgroundColor");
            var selectPanelBackground = await ReadCssColorAsync(defaultProviderPanel, "backgroundColor");
            var selectOptionColor = await ReadCssColorAsync(defaultProviderOption, "color");
            var inputBackground = await ReadCssColorAsync(accountLabelField, "backgroundColor");
            var subtitleColor = await ReadCssColorAsync(oneDriveSubtitle, "color");

            Assert.True(
                HasMinimumChannels(cardBackground, BrowserTestConstants.SettingsFlow.MinimumLightSurfaceChannel),
                $"Expected the light-theme cloud card surface to stay light, but got rgba({cardBackground.R:0.##}, {cardBackground.G:0.##}, {cardBackground.B:0.##}, {cardBackground.A:0.##}).");
            Assert.True(
                HasMinimumChannels(selectBackground, BrowserTestConstants.SettingsFlow.MinimumLightFieldChannel),
                $"Expected the light-theme provider select surface to stay light, but got rgba({selectBackground.R:0.##}, {selectBackground.G:0.##}, {selectBackground.B:0.##}, {selectBackground.A:0.##}).");
            Assert.True(
                HasMinimumChannels(selectPanelBackground, BrowserTestConstants.SettingsFlow.MinimumLightFieldChannel),
                $"Expected the light-theme provider dropdown surface to stay light, but got rgba({selectPanelBackground.R:0.##}, {selectPanelBackground.G:0.##}, {selectPanelBackground.B:0.##}, {selectPanelBackground.A:0.##}).");
            Assert.True(
                HasMinimumChannels(inputBackground, BrowserTestConstants.SettingsFlow.MinimumLightFieldChannel),
                $"Expected the light-theme account label field surface to stay light, but got rgba({inputBackground.R:0.##}, {inputBackground.G:0.##}, {inputBackground.B:0.##}, {inputBackground.A:0.##}).");
            Assert.True(
                HasMaximumChannels(selectOptionColor, BrowserTestConstants.SettingsFlow.MaximumReadableTextChannel),
                $"Expected the light-theme provider dropdown option text to stay readable, but got rgba({selectOptionColor.R:0.##}, {selectOptionColor.G:0.##}, {selectOptionColor.B:0.##}, {selectOptionColor.A:0.##}).");
            Assert.True(
                HasMaximumChannels(subtitleColor, BrowserTestConstants.SettingsFlow.MaximumReadableSecondaryTextChannel),
                $"Expected the light-theme cloud subtitle text to stay readable, but got rgba({subtitleColor.R:0.##}, {subtitleColor.G:0.##}, {subtitleColor.B:0.##}, {subtitleColor.A:0.##}).");

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.SettingsFlow.LightThemeScenario,
                BrowserTestConstants.SettingsFlow.LightThemeStep);
        });

    private static async Task EnsureToggleOffAsync(ILocator locator)
    {
        if (await HasOnClassAsync(locator))
        {
            await locator.ClickAsync();
            await Expect(locator).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
        }
    }

    private static async Task<bool> HasOnClassAsync(ILocator locator)
    {
        var classes = await locator.GetAttributeAsync("class");
        return (classes ?? string.Empty).Contains("on", StringComparison.Ordinal);
    }

    private static bool HasMinimumChannels(CssColor color, double minimum) =>
        color.R >= minimum && color.G >= minimum && color.B >= minimum;

    private static bool HasMaximumChannels(CssColor color, double maximum) =>
        color.R <= maximum && color.G <= maximum && color.B <= maximum;

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
}
