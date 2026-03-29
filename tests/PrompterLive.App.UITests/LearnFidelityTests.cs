using PrompterLive.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterLive.App.UITests;

[Collection(StandaloneAppCollection.Name)]
public sealed class LearnFidelityTests(StandaloneAppFixture fixture)
{
    [Fact]
    public async Task LearnScreen_KeepsOrpLetterCenteredOnReferenceGuide()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.LearnQuantum);
            await Expect(page.GetByTestId(UiTestIds.Learn.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Learn.Word).Locator(".orp")).ToBeVisibleAsync();

            var initialDelta = await MeasureOrpDeltaAsync(page);
            Assert.InRange(initialDelta, 0, 6);

            await page.GetByTestId(UiTestIds.Learn.PlayToggle).ClickAsync();
            await page.WaitForTimeoutAsync(BrowserTestConstants.Timing.LearnPlaybackDelayMs);

            var playbackDelta = await MeasureOrpDeltaAsync(page);
            Assert.InRange(playbackDelta, 0, 6);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static Task<double> MeasureOrpDeltaAsync(Microsoft.Playwright.IPage page) =>
        page.EvaluateAsync<double>(
            """
            () => {
                const line = document.querySelector('[data-testid="learn-orp-line"]');
                const orp = document.querySelector('[data-testid="learn-word"] .orp');
                if (!line || !orp) {
                    return 999;
                }

                const lineRect = line.getBoundingClientRect();
                const orpRect = orp.getBoundingClientRect();
                const lineCenter = lineRect.left + (lineRect.width / 2);
                const orpCenter = orpRect.left + (orpRect.width / 2);
                return Math.abs(lineCenter - orpCenter);
            }
            """);
}
