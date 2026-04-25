using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class SettingsHelpFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    [Test]
    public Task SettingsHelpSection_IsDiscoverableAndExplainsPrompterOneAndTps() =>
        RunPageAsync(async page =>
        {
            await ShellRouteDriver.OpenSettingsAsync(page);
            await page.GetByTestId(UiTestIds.Settings.NavHelp).ClickAsync();

            var helpPanel = page.GetByTestId(UiTestIds.Settings.HelpPanel);
            await Expect(helpPanel).ToBeVisibleAsync();
            await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.HelpAppFlowCard);
            await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.HelpTpsBasicsCard);
            await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.HelpModesCard);
            await SettingsCardDriver.EnsureExpandedAsync(page, UiTestIds.Settings.HelpLocalFilesCard);

            await Expect(helpPanel).ToContainTextAsync("PrompterOne workflow");
            await Expect(helpPanel).ToContainTextAsync("TPS standard basics");
            await Expect(helpPanel).ToContainTextAsync("## [Segment|140WPM|neutral]");
            await Expect(helpPanel).ToContainTextAsync("Teleprompter");
            await Expect(helpPanel).ToContainTextAsync("Import and Export");
        });
}
