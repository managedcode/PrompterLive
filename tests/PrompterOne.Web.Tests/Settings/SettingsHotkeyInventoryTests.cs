using Bunit;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class SettingsHotkeyInventoryTests : BunitContext
{
    [Test]
    public void SettingsPage_ShortcutsSection_RendersEveryCatalogGroupAndAction()
    {
        TestHarnessFactory.Create(this);

        var cut = Render<SettingsPage>();

        cut.FindByTestId(UiTestIds.Settings.NavShortcuts).Click();

        cut.WaitForAssertion(() =>
        {
            var panel = cut.FindByTestId(UiTestIds.Settings.ShortcutsPanel);

            foreach (var group in AppHotkeys.Groups)
            {
                cut.FindByTestId(UiTestIds.Settings.ShortcutsGroup(group.Id));

                foreach (var definition in group.Definitions)
                {
                    cut.FindByTestId(UiTestIds.Settings.ShortcutsAction(group.Id, definition.Id));
                }
            }

            Assert.DoesNotContain("OBS", panel.TextContent, StringComparison.OrdinalIgnoreCase);
        });
    }
}
