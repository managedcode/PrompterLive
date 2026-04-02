using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Storage.Cloud;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class SettingsCloudStorageFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture), IClassFixture<StandaloneAppFixture>
{
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

            await SettingsSelectDriver.SelectByValueAsync(
                page,
                UiTestIds.Settings.CloudDefaultProvider,
                CloudStorageProviderIds.Dropbox);
            await ToggleSettingsButtonAsync(page.GetByTestId(UiTestIds.Settings.CloudAutoSyncOnSave));
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

    private static async Task ToggleSettingsButtonAsync(ILocator locator)
    {
        var wasOn = await HasOnClassAsync(locator);
        await locator.ClickAsync();

        if (wasOn)
        {
            await Expect(locator).Not.ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
        }
        else
        {
            await Expect(locator).ToHaveClassAsync(BrowserTestConstants.Regexes.ToggleOnClass);
        }
    }

    private static async Task<bool> HasOnClassAsync(ILocator locator) =>
        (await locator.GetAttributeAsync(BrowserTestConstants.Html.ClassAttribute) ?? string.Empty)
        .Split(BrowserTestConstants.Html.ClassSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Contains(BrowserTestConstants.Css.OnClass, StringComparer.Ordinal);
}
