using System.Globalization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterFullFlowTests(StandaloneAppFixture fixture)
{
    private const int BenefitsCardIndex = 5;
    private const int ClosingCardIndex = 7;
    private const int OpeningCardIndex = 0;
    private const int PurposeCardIndex = 1;
    private const string ReaderNextCardCssClass = "rd-card-next";
    private const int SpeedOffsetsCardIndex = 0;
    private const int StatisticsCardIndex = 2;
    private const int InspirationCardIndex = 6;

    [Test]
    public async Task TeleprompterProductLaunch_FullTpsScenario_CapturesArtifactsAndKeepsAlignment()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.TeleprompterFullFlow.Name);
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await AssertProductLaunchTpsRenderingAsync(page);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFullFlow.Name,
                BrowserTestConstants.TeleprompterFullFlow.InitialStep);

            var playToggle = page.GetByTestId(UiTestIds.Teleprompter.PlayToggle);
            await playToggle.ClickAsync();
            await Expect(playToggle.Locator(BrowserTestConstants.Teleprompter.PauseToggleIconSelector))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ReaderPlaybackReadyTimeoutMs });
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFullFlow.Name,
                BrowserTestConstants.TeleprompterFullFlow.PlaybackStep);

            await Expect(page.Locator($"#{UiDomIds.Teleprompter.BlockIndicator}"))
                .ToHaveTextAsync(
                    BrowserTestConstants.Regexes.ReaderSecondBlockIndicator,
                    new() { Timeout = BrowserTestConstants.Timing.ReaderAutomaticTransitionTimeoutMs });

            await AssertCurrentActiveWordAlignedAsync(page);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.TeleprompterFullFlow.Name,
                BrowserTestConstants.TeleprompterFullFlow.TransitionStep);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task TeleprompterSpeedOffsets_UsesCustomFrontMatterSpeedOffsetsAndNormalResetSpacing()
    {
        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterSpeedOffsets);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var slowWord = await GetWordProbeAsync(page, SpeedOffsetsCardIndex, BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWord);
            var normalWord = await GetWordProbeAsync(page, SpeedOffsetsCardIndex, BrowserTestConstants.TeleprompterFlow.SpeedOffsetsNormalWord);
            var resumedSlowWord = await GetWordProbeAsync(page, SpeedOffsetsCardIndex, BrowserTestConstants.TeleprompterFlow.SpeedOffsetsResumedSlowWord);
            var fastWord = await GetWordProbeAsync(page, SpeedOffsetsCardIndex, BrowserTestConstants.TeleprompterFlow.SpeedOffsetsFastWord);

            await Assert.That(slowWord.Classes).Contains("tps-slow");
            await Assert.That(slowWord.EffectiveWpm).IsEqualTo(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWpm);
            await Assert.That(slowWord.Title).IsEqualTo(string.Empty);

            await Assert.That(normalWord.EffectiveWpm).IsEqualTo(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsNormalWpm);
            await Assert.That(normalWord.Classes).DoesNotContain("tps-slow");
            await Assert.That(normalWord.Classes).DoesNotContain("tps-fast");
            await Assert.That(normalWord.Style).IsEqualTo(string.Empty);
            await Assert.That(normalWord.Title).IsEqualTo(string.Empty);

            await Assert.That(resumedSlowWord.Classes).Contains("tps-slow");
            await Assert.That(resumedSlowWord.EffectiveWpm).IsEqualTo(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWpm);

            await Assert.That(fastWord.Classes).Contains("tps-fast");
            await Assert.That(fastWord.EffectiveWpm).IsEqualTo(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsFastWpm);

            await Assert.That(ParseMilliseconds(slowWord.DurationMs) > ParseMilliseconds(normalWord.DurationMs)).IsTrue();
            await Assert.That(ParseMilliseconds(resumedSlowWord.DurationMs) > ParseMilliseconds(normalWord.DurationMs)).IsTrue();
            await Assert.That(ParseMilliseconds(normalWord.DurationMs) > ParseMilliseconds(fastWord.DurationMs)).IsTrue();

            await Assert.That(ParsePixels(slowWord.LetterSpacing) > ParsePixels(normalWord.LetterSpacing)).IsTrue();
            await Assert.That(ParsePixels(resumedSlowWord.LetterSpacing) > ParsePixels(normalWord.LetterSpacing)).IsTrue();
            await Assert.That(ParsePixels(fastWord.LetterSpacing) < ParsePixels(normalWord.LetterSpacing)).IsTrue();
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task AssertProductLaunchTpsRenderingAsync(Microsoft.Playwright.IPage page)
    {
        var neutralWord = await GetWordProbeAsync(page, OpeningCardIndex, BrowserTestConstants.TeleprompterFlow.NeutralWord);
        var professionalWord = await GetWordProbeAsync(page, OpeningCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchProfessionalWord);
        var highlightWord = await GetWordProbeAsync(page, PurposeCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchHighlightWord);
        var slowWord = await GetWordProbeAsync(page, StatisticsCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchSlowWord);
        var fastWord = await GetWordProbeAsync(page, BenefitsCardIndex, BrowserTestConstants.TeleprompterFlow.FastWord);
        var softWord = await GetWordProbeAsync(page, InspirationCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchSoftWord);
        var urgentWord = await GetWordProbeAsync(page, ClosingCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchUrgentWord);
        var visionWord = await GetWordProbeAsync(page, InspirationCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionWord);
        var rhetoricalWord = await GetWordProbeAsync(page, InspirationCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchRhetoricalWord);
        var teleprompterWord = await GetWordProbeAsync(page, ClosingCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchTeleprompterWord);

        await Assert.That(neutralWord.Classes).DoesNotContain("tps-neutral");
        await Assert.That(neutralWord.Classes).DoesNotContain("tps-warm");
        await Assert.That(neutralWord.Classes).DoesNotContain("tps-focused");

        await Assert.That(professionalWord.Classes).Contains("tps-professional");
        await Assert.That(professionalWord.Classes).DoesNotContain("tps-warm");

        await Assert.That(highlightWord.Classes).Contains("tps-highlight");
        await Assert.That(highlightWord.CardClasses).Contains(ReaderNextCardCssClass);
        await Assert.That(highlightWord.BackgroundColor).IsEqualTo(BrowserTestConstants.TeleprompterFlow.TransparentBackgroundColor);

        await Assert.That(slowWord.Classes).Contains("tps-xslow");
        await Assert.That(fastWord.Classes).Contains("tps-xfast");
        await Assert.That(softWord.Classes).Contains("tps-soft");
        await Assert.That(softWord.Classes).DoesNotContain("tps-motivational");
        await Assert.That(urgentWord.Classes).Contains("tps-urgent");
        await Assert.That(urgentWord.Classes).DoesNotContain("tps-energetic");
        await Assert.That(rhetoricalWord.Classes).Contains("tps-rhetorical");
        await Assert.That(teleprompterWord.Classes).DoesNotContain("tps-energetic");

        await Assert.That(visionWord.Pronunciation).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionPronunciation);
        await Assert.That(visionWord.Title).IsEqualTo(string.Empty);

        await Assert.That(teleprompterWord.Pronunciation).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchTeleprompterPronunciation);
        await Assert.That(teleprompterWord.EffectiveWpm).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchTeleprompterWpm);
        await Assert.That(teleprompterWord.Title).IsEqualTo(string.Empty);

        await Assert.That(ParseMilliseconds(slowWord.DurationMs) > ParseMilliseconds(fastWord.DurationMs)).IsTrue();
        await Assert.That(professionalWord.Color).IsNotEqualTo(neutralWord.Color);
        await Assert.That(softWord.Color).IsNotEqualTo(neutralWord.Color);
        await Assert.That(urgentWord.Color).IsNotEqualTo(neutralWord.Color);
        await Assert.That(ParsePixels(slowWord.LetterSpacing) > ParsePixels(neutralWord.LetterSpacing)).IsTrue();
        await Assert.That(ParsePixels(fastWord.LetterSpacing) < ParsePixels(neutralWord.LetterSpacing)).IsTrue();
    }

    private static async Task AssertCurrentActiveWordAlignedAsync(Microsoft.Playwright.IPage page)
    {
        var activeWordTestId = string.Empty;
        var attemptCount = BrowserTestConstants.Teleprompter.AlignmentTimeoutMs /
            BrowserTestConstants.Teleprompter.AlignmentPollDelayMs;

        for (var attempt = 0; attempt < attemptCount; attempt++)
        {
            activeWordTestId = await page.GetByTestId(UiTestIds.Teleprompter.Page).EvaluateAsync<string>(
                """
                element => element.querySelector('.rd-card-active .rd-w.rd-now')?.getAttribute('data-testid') ?? ''
                """);

            if (!string.IsNullOrWhiteSpace(activeWordTestId))
            {
                break;
            }

            await page.WaitForTimeoutAsync(BrowserTestConstants.Teleprompter.AlignmentPollDelayMs);
        }

        await Assert.That(string.IsNullOrWhiteSpace(activeWordTestId)).IsFalse();
        await AssertGuideAlignmentAsync(
            page,
            page.GetByTestId(UiTestIds.Teleprompter.FocalGuide),
            page.GetByTestId(activeWordTestId));
    }

    private static async Task AssertGuideAlignmentAsync(
        Microsoft.Playwright.IPage page,
        Microsoft.Playwright.ILocator focalGuide,
        Microsoft.Playwright.ILocator word)
    {
        var attemptCount = BrowserTestConstants.Teleprompter.AlignmentTimeoutMs /
            BrowserTestConstants.Teleprompter.AlignmentPollDelayMs;
        var lastDelta = double.MaxValue;

        for (var attempt = 0; attempt < attemptCount; attempt++)
        {
            lastDelta = await MeasureVerticalCenterDeltaAsync(focalGuide, word);
            if (Math.Abs(lastDelta) <= BrowserTestConstants.Teleprompter.AlignmentTolerancePx)
            {
                return;
            }

            await page.WaitForTimeoutAsync(BrowserTestConstants.Teleprompter.AlignmentPollDelayMs);
        }

        await Assert.That(Math.Abs(lastDelta)).IsBetween(0,BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
    }

    private static async Task<double> MeasureVerticalCenterDeltaAsync(
        Microsoft.Playwright.ILocator focalGuide,
        Microsoft.Playwright.ILocator word)
    {
        var focalGuideBox = await focalGuide.BoundingBoxAsync();
        var wordBox = await word.BoundingBoxAsync();

        await Assert.That(focalGuideBox).IsNotNull();
        await Assert.That(wordBox).IsNotNull();

        return (focalGuideBox.Y + (focalGuideBox.Height / 2d)) -
            (wordBox.Y + (wordBox.Height / 2d));
    }

    private static async Task<ReaderWordProbe> GetWordProbeAsync(Microsoft.Playwright.IPage page, int cardIndex, string wordText)
    {
        var probe = await page.GetByTestId(UiTestIds.Teleprompter.CardText(cardIndex)).EvaluateAsync<ReaderWordProbe>(
            """
            (element, expectedWord) => {
                const word = Array.from(element.querySelectorAll('.rd-w'))
                    .find(node => node.textContent?.trim() === expectedWord);

                if (!(word instanceof HTMLElement)) {
                    return null;
                }

                const computed = window.getComputedStyle(word);
                const card = word.closest('.rd-card');
                return {
                    classes: word.className,
                    cardClasses: card instanceof HTMLElement ? card.className : '',
                    style: word.getAttribute('style') ?? '',
                    title: word.getAttribute('title') ?? '',
                    pronunciation: word.getAttribute('data-pronunciation') ?? '',
                    effectiveWpm: word.getAttribute('data-effective-wpm') ?? '',
                    durationMs: word.getAttribute('data-ms') ?? '',
                    letterSpacing: computed.letterSpacing ?? '',
                    color: computed.color ?? '',
                    backgroundColor: computed.backgroundColor ?? ''
                };
            }
            """,
            wordText);

        await Assert.That(probe).IsNotNull();
        return probe!;
    }

    private static double ParsePixels(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "normal", StringComparison.Ordinal))
        {
            return 0d;
        }

        return double.Parse(value.Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);
    }

    private static int ParseMilliseconds(string value) =>
        int.Parse(value, CultureInfo.InvariantCulture);

    private sealed class ReaderWordProbe
    {
        public string Classes { get; set; } = string.Empty;
        public string CardClasses { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Pronunciation { get; set; } = string.Empty;
        public string EffectiveWpm { get; set; } = string.Empty;
        public string DurationMs { get; set; } = string.Empty;
        public string LetterSpacing { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
    }
}