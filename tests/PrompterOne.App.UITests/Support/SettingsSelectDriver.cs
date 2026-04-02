using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

internal static class SettingsSelectDriver
{
    public static Task SelectByValueAsync(IPage page, string triggerTestId, string optionValue) =>
        SelectByValueAsync(page, page.GetByTestId(triggerTestId), triggerTestId, optionValue);

    public static async Task SelectByValueAsync(
        IPage page,
        ILocator trigger,
        string triggerTestId,
        string optionValue)
    {
        await trigger.ClickAsync();
        await page.GetByTestId(UiTestIds.Settings.SelectOption(triggerTestId, optionValue)).ClickAsync();
        await Expect(trigger).ToHaveAttributeAsync(BrowserTestConstants.Html.ValueAttribute, optionValue);
    }
}
