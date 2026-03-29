using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

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
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterSecurityIncident);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.Locator($"#{UiDomIds.Teleprompter.CameraOverlay(1)}")).ToHaveCountAsync(0);
            var firstCard = page.GetByTestId(UiTestIds.Teleprompter.Card(0));
            await Expect(firstCard).ToBeVisibleAsync();

            var wordCounts = await firstCard.Locator($"[data-testid^='{UiTestIds.Teleprompter.CardGroupPrefix(0)}']").EvaluateAllAsync<int[]>(
                "elements => elements.map(element => element.querySelectorAll('.rd-w').length)");

            Assert.NotEmpty(wordCounts);
            Assert.All(wordCounts, wordCount => Assert.InRange(wordCount, 1, 5));

            var hasHorizontalOverflow = await page.GetByTestId(UiTestIds.Teleprompter.CardText(0)).EvaluateAsync<bool>(
                "element => element.scrollWidth > element.clientWidth + 8");
            Assert.False(hasHorizontalOverflow);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
