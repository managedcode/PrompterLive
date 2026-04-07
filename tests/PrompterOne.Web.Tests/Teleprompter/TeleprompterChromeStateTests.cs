using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class TeleprompterChromeStateTests : BunitContext
{
    private const string ActiveStateValue = "active";
    private const string InactiveStateValue = "inactive";

    [Test]
    public void TeleprompterPage_PlaybackMarksChromeAsReadingActive()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);

        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(InactiveStateValue, cut.FindByTestId(UiTestIds.Teleprompter.Controls).GetAttribute("data-active"));
            Assert.Equal(InactiveStateValue, cut.FindByTestId(UiTestIds.Teleprompter.Progress).GetAttribute("data-active"));
            Assert.Equal(InactiveStateValue, cut.FindByTestId(UiTestIds.Teleprompter.EdgeInfo).GetAttribute("data-active"));
        });

        cut.FindByTestId(UiTestIds.Teleprompter.NextWord).Click();
        cut.FindByTestId(UiTestIds.Teleprompter.PlayToggle).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Teleprompter.Controls).GetAttribute("data-active"));
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Teleprompter.Progress).GetAttribute("data-active"));
            Assert.Equal(ActiveStateValue, cut.FindByTestId(UiTestIds.Teleprompter.EdgeInfo).GetAttribute("data-active"));
        });
    }
}
