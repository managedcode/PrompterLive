using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class SettingsHotkeyInventoryFlowTests(StandaloneAppFixture fixture)
    : AppUiTestBase(fixture)
{
    public static IEnumerable<AppHotkeyGroup> HotkeyGroups => AppHotkeys.Groups;

    [Test]
    [MethodDataSource(nameof(HotkeyGroups))]
    public Task SettingsPage_ShortcutsGroup_ExpandsAndShowsItsFirstAction(AppHotkeyGroup group) =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Settings.NavShortcuts).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.ShortcutsPanel)).ToBeVisibleAsync();

            var groupCard = page.GetByTestId(UiTestIds.Settings.ShortcutsGroup(group.Id));
            await groupCard.ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.ShortcutsAction(group.Id, group.Definitions[0].Id)))
                .ToBeVisibleAsync();
        });

    [Test]
    public Task SettingsPage_ShortcutsSection_CapturesGroupedHotkeyCatalog() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.SettingsFlow.ShortcutsScenario);

            await page.GotoAsync(BrowserTestConstants.Routes.Settings);
            await Expect(page.GetByTestId(UiTestIds.Settings.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Settings.NavShortcuts).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Settings.ShortcutsPanel)).ToBeVisibleAsync();

            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.SettingsFlow.ShortcutsScenario,
                BrowserTestConstants.SettingsFlow.ShortcutsStep);
        });
}
