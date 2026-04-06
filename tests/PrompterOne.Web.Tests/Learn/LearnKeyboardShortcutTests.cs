using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class LearnKeyboardShortcutTests : BunitContext
{
    private const string PressedAttributeName = "aria-pressed";
    private const string TrueValue = "true";

    [Test]
    public void LearnPage_EscapeShortcut_NavigatesBackToEditor()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.LearnQuantum);

        var cut = Render<LearnPage>();

        cut.WaitForAssertion(() =>
        {
            var page = cut.FindByTestId(UiTestIds.Learn.Page);
            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.Escape });

            Assert.EndsWith(AppTestData.Routes.EditorQuantum, Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
        });
    }

    [Test]
    public void LearnPage_LShortcut_TogglesLoopPlayback()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.LearnQuantum);

        var cut = Render<LearnPage>();

        cut.WaitForAssertion(() =>
        {
            var page = cut.FindByTestId(UiTestIds.Learn.Page);
            var loopToggle = cut.FindByTestId(UiTestIds.Learn.LoopToggle);

            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.LLower });

            Assert.Equal(TrueValue, loopToggle.GetAttribute(PressedAttributeName));
        });
    }

    [Test]
    public void LearnPage_SpaceShortcut_TogglesPlayback()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.LearnQuantum);

        var cut = Render<LearnPage>();

        cut.WaitForAssertion(() =>
        {
            var page = cut.FindByTestId(UiTestIds.Learn.Page);
            var playToggle = cut.FindByTestId(UiTestIds.Learn.PlayToggle);

            page.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = UiKeyboardKeys.Space });

            Assert.Equal(TrueValue, playToggle.GetAttribute(PressedAttributeName));
        });
    }
}
