using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class TeleprompterChromeStateTests : BunitContext
{
    private const string ReadingActiveCssClass = "rd-reading-active";

    [Test]
    public void TeleprompterPage_PlaybackMarksChromeAsReadingActive()
    {
        TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterDemo);

        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(
                ReadingActiveCssClass,
                cut.FindByTestId(UiTestIds.Teleprompter.Controls).ClassName ?? string.Empty,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ReadingActiveCssClass,
                cut.FindByTestId(UiTestIds.Teleprompter.Progress).ClassName ?? string.Empty,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ReadingActiveCssClass,
                cut.FindByTestId(UiTestIds.Teleprompter.EdgeInfo).ClassName ?? string.Empty,
                StringComparison.Ordinal);
        });

        cut.FindByTestId(UiTestIds.Teleprompter.NextWord).Click();
        cut.FindByTestId(UiTestIds.Teleprompter.PlayToggle).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(
                ReadingActiveCssClass,
                cut.FindByTestId(UiTestIds.Teleprompter.Controls).ClassName ?? string.Empty,
                StringComparison.Ordinal);
            Assert.Contains(
                ReadingActiveCssClass,
                cut.FindByTestId(UiTestIds.Teleprompter.Progress).ClassName ?? string.Empty,
                StringComparison.Ordinal);
            Assert.Contains(
                ReadingActiveCssClass,
                cut.FindByTestId(UiTestIds.Teleprompter.EdgeInfo).ClassName ?? string.Empty,
                StringComparison.Ordinal);
        });
    }
}
