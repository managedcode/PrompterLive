using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class TeleprompterKeyboardShortcutTests : BunitContext
{
    private const string ActiveStateValue = "active";

    [Test]
    public void TeleprompterPage_EscapeShortcut_NavigatesBackToEditor()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterQuantum);

        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var page = cut.FindByTestId(UiTestIds.Teleprompter.Page);
            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.Escape });

            Assert.EndsWith(AppTestData.Routes.EditorQuantum, Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
        });
    }

    [Test]
    public void TeleprompterPage_Hotkeys_ToggleMirrorAndJustifyAlignment()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterQuantum);

        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var page = cut.FindByTestId(UiTestIds.Teleprompter.Page);
            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.HLower });

            Assert.Equal(
                ActiveStateValue,
                cut.FindByTestId(UiTestIds.Teleprompter.MirrorHorizontalToggle).GetAttribute("data-active"));

            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.Digit4 });

            Assert.Equal(
                ActiveStateValue,
                cut.FindByTestId(UiTestIds.Teleprompter.AlignmentJustify).GetAttribute("data-active"));
        });
    }
}
