using Bunit;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class TeleprompterFidelityTests : BunitContext
{
    [Fact]
    public void TeleprompterPage_UsesReferenceSizedReaderGroupsForSecurityIncident()
    {
        var harness = TestHarnessFactory.Create(this);
        var cut = Render<TeleprompterPage>(parameters => parameters.Add(page => page.ScriptId, "security-incident"));

        cut.WaitForAssertion(() =>
        {
            var groups = cut.FindAll(".rd-card-active .rd-g");

            Assert.NotEmpty(groups);
            Assert.True(groups.Count >= 4);
            Assert.All(groups, group =>
            {
                var wordCount = group.QuerySelectorAll(".rd-w").Length;
                Assert.InRange(wordCount, 1, 5);
            });

            var clusterText = cut.Find(".rd-card-active .rd-cluster-text").TextContent;
            Assert.Contains("At 04:12 this morning", clusterText, StringComparison.Ordinal);
            Assert.DoesNotContain("At 04:12 this morning, our monitoring systems detected unauthorized activity in a production environment", clusterText, StringComparison.Ordinal);
        });
    }
}
