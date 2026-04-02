using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class EditorOverlayInteractionTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Fact]
    public async Task EditorScreen_HidesFloatingBarWhileToolbarDropdownIsOpen()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);

            var sourceInput = page.GetByTestId(UiTestIds.Editor.SourceInput);
            var floatingBar = page.GetByTestId(UiTestIds.Editor.FloatingBar);
            var colorMenu = page.GetByTestId(UiTestIds.Editor.MenuColor);

            await Expect(sourceInput)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await sourceInput.EvaluateAsync(
                "(element, target) => { const start = element.value.indexOf(target); element.focus(); element.setSelectionRange(start, start + target.length); element.dispatchEvent(new Event('select', { bubbles: true })); element.dispatchEvent(new Event('keyup', { bubbles: true })); }",
                BrowserTestConstants.Editor.Welcome);

            await Expect(floatingBar).ToBeVisibleAsync();

            await page.GetByTestId(UiTestIds.Editor.ColorTrigger).ClickAsync();

            await Expect(colorMenu).ToBeVisibleAsync();
            await Expect(floatingBar).ToBeHiddenAsync();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
