using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class SettingsHotkeyInventoryFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture){
    [Test]
    public Task SettingsPage_ShortcutsSection_ShowsGroupedHotkeyCatalog() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.SettingsFlow.ShortcutsScenario);

            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Settings.NavShortcuts).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.ShortcutsPanel)).ToBeVisibleAsync();

            foreach (var group in AppHotkeys.Groups)
            {
                var groupCard = page.GetByTestId(UiTestIds.Settings.ShortcutsGroup(group.Id));
                await groupCard.ClickAsync();
                await Expect(page.GetByTestId(UiTestIds.Settings.ShortcutsAction(group.Id, group.Definitions[0].Id)))
                    .ToBeVisibleAsync();
            }

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.SettingsFlow.ShortcutsScenario,
                BrowserTestConstants.SettingsFlow.ShortcutsStep);
        });
}