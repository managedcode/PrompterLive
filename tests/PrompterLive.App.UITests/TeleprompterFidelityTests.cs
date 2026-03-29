using Microsoft.Playwright;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class TeleprompterFidelityTests(StandaloneAppFixture fixture)
{
    [Fact]
    public async Task Teleprompter_UsesReferenceSizedReaderGroupsForSecurityIncident()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync("/teleprompter?id=security-incident");
            await Expect(page.GetByTestId("teleprompter-page")).ToBeVisibleAsync(new() { Timeout = 15000 });

            var wordCounts = await page.Locator(".rd-card-active .rd-g").EvaluateAllAsync<int[]>(
                "elements => elements.map(element => element.querySelectorAll('.rd-w').length)");

            Assert.NotEmpty(wordCounts);
            Assert.True(wordCounts.Length >= 4);
            Assert.All(wordCounts, wordCount => Assert.InRange(wordCount, 1, 5));

            var hasHorizontalOverflow = await page.Locator(".rd-card-active .rd-cluster-text").EvaluateAsync<bool>(
                "element => element.scrollWidth > element.clientWidth + 8");
            Assert.False(hasHorizontalOverflow);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
