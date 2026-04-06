using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

public sealed partial class EditorInteractionTests
{
    private static async Task OpenFloatingMenuAsync(IPage page, string triggerTestId, string panelTestId)
    {
        var trigger = page.GetByTestId(triggerTestId);
        var panel = page.GetByTestId(panelTestId);

        await trigger.ScrollIntoViewIfNeededAsync();
        await Expect(trigger).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
        await trigger.ClickAsync();
        await Expect(panel).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.FastVisibleTimeoutMs });
    }
}