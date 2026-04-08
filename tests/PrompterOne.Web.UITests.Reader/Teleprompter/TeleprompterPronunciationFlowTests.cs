using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterPronunciationFlowTests(StandaloneAppFixture fixture)
{
    private const int InspirationCardIndex = 6;
    private const string ScenarioName = "teleprompter-pronunciation";
    private const string StepName = "01-vision-pronunciation";

    [Test]
    public async Task TeleprompterDemo_UsesReadableVisionPronunciationGuide()
    {
        UiScenarioArtifacts.ResetScenario(ScenarioName);

        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var probe = await page.GetByTestId(UiTestIds.Teleprompter.CardText(InspirationCardIndex)).EvaluateAsync<PronunciationProbe>(
                """
                (element, args) => {
                    const word = Array.from(element.querySelectorAll(`[data-test^="${args.wordPrefix}"]`))
                        .find(node => node.textContent?.trim() === args.expectedWord);

                    if (!(word instanceof HTMLElement)) {
                        return null;
                    }

                    return {
                        pronunciation: word.getAttribute(args.pronunciationAttributeName) ?? '',
                        title: word.getAttribute('title') ?? ''
                    };
                }
                """,
                new
                {
                    expectedWord = BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionWord,
                    pronunciationAttributeName = UiDataAttributes.Teleprompter.Pronunciation,
                    wordPrefix = UiTestIds.Teleprompter.CardWordPrefix(InspirationCardIndex)
                });

            await Assert.That(probe).IsNotNull();
            await Assert.That(probe!.Pronunciation).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionPronunciation);
            await Assert.That(probe.Title).IsEqualTo(string.Empty);

            await UiScenarioArtifacts.CapturePageAsync(page, ScenarioName, StepName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private sealed class PronunciationProbe
    {
        public string Pronunciation { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;
    }
}
