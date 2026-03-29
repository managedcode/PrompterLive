using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterLive.Shared.Contracts;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class TeleprompterFidelityTests : BunitContext
{
    [Fact]
    public void TeleprompterPage_UsesReferenceSizedReaderGroupsForSecurityIncident()
    {
        var harness = TestHarnessFactory.Create(this);
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.TeleprompterSecurityIncident);
        var cut = Render<TeleprompterPage>();

        cut.WaitForAssertion(() =>
        {
            var groups = cut.FindAll($"{BunitTestSelectors.BuildTestIdSelector(UiTestIds.Teleprompter.Card(0))} [data-testid^='{UiTestIds.Teleprompter.CardGroupPrefix(0)}']");
            var groupTexts = groups.Select(group => group.TextContent).ToArray();

            Assert.NotEmpty(groups);
            Assert.True(groups.Count >= 4);
            Assert.All(groups, group =>
            {
                var wordCount = group.QuerySelectorAll(".rd-w").Length;
                Assert.InRange(wordCount, 1, 5);
            });
            Assert.Contains(groupTexts, text => text.Contains("At 04:12 this morning", StringComparison.Ordinal));
            Assert.DoesNotContain(
                groupTexts,
                text => text.Contains(
                    "At 04:12 this morning, our monitoring systems detected unauthorized activity in a production environment",
                    StringComparison.Ordinal));
            Assert.DoesNotContain("rd-camera-overlay-", cut.Markup, StringComparison.Ordinal);
        });
    }
}
