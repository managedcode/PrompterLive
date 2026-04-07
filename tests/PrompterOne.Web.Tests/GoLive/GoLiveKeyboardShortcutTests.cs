using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class GoLiveKeyboardShortcutTests : BunitContext
{
    private const string ActiveCssClass = "active";

    [Test]
    public void GoLivePage_Hotkeys_ToggleModeAndLayoutChrome()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.GoLiveDemo);

        var cut = Render<GoLivePage>();

        cut.WaitForAssertion(() =>
        {
            var page = cut.FindByTestId(UiTestIds.GoLive.Page);

            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.Digit2 });
            Assert.Contains(
                ActiveCssClass,
                cut.FindByTestId(UiTestIds.GoLive.ModeStudio).ClassName,
                StringComparison.Ordinal);

            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.BracketLeft });
            Assert.Empty(cut.FindAll($"[data-test='{UiTestIds.GoLive.SourceRail}']"));

            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.FLower });
            Assert.Contains(
                ActiveCssClass,
                cut.FindByTestId(UiTestIds.GoLive.FullProgramToggle).ClassName,
                StringComparison.Ordinal);
        });
    }
}
