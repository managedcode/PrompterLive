using System.Globalization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterPronunciationFlowTests(StandaloneAppFixture fixture)
{
    private const int InspirationCardIndex = 6;
    private const double MinimumReadableGuideFontSizePx = 24d;
    private const string ScenarioName = "teleprompter-pronunciation";
    private const string StepName = "01-vision-pronunciation";

    [Test]
    public async Task TeleprompterDemo_UsesReadableVisionPronunciationGuide()
    {
        UiScenarioArtifacts.ResetScenario(ScenarioName);

        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await AdvanceToCardAsync(page, InspirationCardIndex);

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Card(InspirationCardIndex))).ToHaveAttributeAsync(
                UiDataAttributes.Teleprompter.CardState,
                UiDataAttributes.Teleprompter.ActiveState,
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var probe = await page.GetByTestId(UiTestIds.Teleprompter.CardText(InspirationCardIndex)).EvaluateAsync<PronunciationProbe>(
                """
                (element, args) => {
                    const word = Array.from(element.querySelectorAll(`[data-test^="${args.wordPrefix}"]`))
                        .find(node => node.textContent?.trim() === args.expectedGuide);

                    if (!(word instanceof HTMLElement)) {
                        return null;
                    }

                    return {
                        guideContent: getComputedStyle(word, '::after').content ?? '',
                        guideFontSize: getComputedStyle(word, '::after').fontSize ?? '',
                        mainFontSize: getComputedStyle(word).fontSize ?? '',
                        originalText: word.getAttribute(args.originalTextAttributeName) ?? '',
                        pronunciation: word.getAttribute(args.pronunciationAttributeName) ?? '',
                        title: word.getAttribute('title') ?? ''
                    };
                }
                """,
                new
                {
                    expectedGuide = BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionPronunciation,
                    originalTextAttributeName = UiDataAttributes.Teleprompter.OriginalText,
                    pronunciationAttributeName = UiDataAttributes.Teleprompter.Pronunciation,
                    wordPrefix = UiTestIds.Teleprompter.CardWordPrefix(InspirationCardIndex)
                });

            await Assert.That(probe).IsNotNull();
            await Assert.That(probe!.Pronunciation).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionPronunciation);
            await Assert.That(probe.OriginalText).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionWord);
            await Assert.That(probe.GuideContent).Contains(BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionWord);
            await Assert.That(ParseCssPixels(probe.MainFontSize))
                .IsGreaterThanOrEqualTo(MinimumReadableGuideFontSizePx)
                .Because("Expected the pronunciation guide to be the primary large rehearsal text, not a tiny tooltip.");
            await Assert.That(ParseCssPixels(probe.GuideFontSize))
                .IsLessThan(ParseCssPixels(probe.MainFontSize))
                .Because("Expected the original spelling annotation to stay secondary to the guide.");
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
        public string GuideContent { get; init; } = string.Empty;

        public string GuideFontSize { get; init; } = string.Empty;

        public string MainFontSize { get; init; } = string.Empty;

        public string OriginalText { get; init; } = string.Empty;

        public string Pronunciation { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;
    }

    private static double ParseCssPixels(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "normal", StringComparison.Ordinal))
        {
            return 0d;
        }

        return double.Parse(value.Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);
    }

    private static async Task AdvanceToCardAsync(Microsoft.Playwright.IPage page, int targetCardIndex)
    {
        for (var index = 0; index < targetCardIndex; index++)
        {
            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Card(index + 1))).ToHaveAttributeAsync(
                UiDataAttributes.Teleprompter.CardState,
                UiDataAttributes.Teleprompter.ActiveState,
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
        }
    }
}
