using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

public sealed partial class EditorInteractionTests
{
    private static async Task OpenFloatingMenuAsync(IPage page, string triggerTestId, string panelTestId)
    {
        var trigger = page.GetByTestId(triggerTestId);
        var panel = page.GetByTestId(panelTestId);

        await trigger.ScrollIntoViewIfNeededAsync();
        await Expect(trigger).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
        await UiInteractionDriver.ClickAndWaitForVisibleAsync(trigger, panel, noWaitAfter: true);
    }
}
