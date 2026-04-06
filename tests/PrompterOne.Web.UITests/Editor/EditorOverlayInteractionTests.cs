using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorOverlayInteractionTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_HidesFloatingBarWhileToolbarDropdownIsOpen()
    {
        var page = await _fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);

            var floatingBar = page.GetByTestId(UiTestIds.Editor.FloatingBar);
            var colorMenu = page.GetByTestId(UiTestIds.Editor.MenuColor);

            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetSelectionByTextAsync(page, BrowserTestConstants.Editor.Welcome);

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