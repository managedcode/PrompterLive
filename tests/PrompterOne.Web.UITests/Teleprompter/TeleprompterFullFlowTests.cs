using System.Globalization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

public sealed class TeleprompterFullFlowTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const int BenefitsCardIndex = 5;
    private const int ClosingCardIndex = 7;
    private const int OpeningCardIndex = 0;
    private const int PurposeCardIndex = 1;
    private const string ReaderNextCardCssClass = "rd-card-next";
    private const int SpeedOffsetsCardIndex = 0;
    private const int StatisticsCardIndex = 2;
    private const int InspirationCardIndex = 6;

    [Fact]
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

    [Fact]
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

            Assert.Contains("tps-slow", slowWord.Classes, StringComparison.Ordinal);
            Assert.Equal(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWpm, slowWord.EffectiveWpm);
            Assert.Equal(string.Empty, slowWord.Title);

            Assert.Equal(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsNormalWpm, normalWord.EffectiveWpm);
            Assert.DoesNotContain("tps-slow", normalWord.Classes, StringComparison.Ordinal);
            Assert.DoesNotContain("tps-fast", normalWord.Classes, StringComparison.Ordinal);
            Assert.Equal(string.Empty, normalWord.Style);
            Assert.Equal(string.Empty, normalWord.Title);

            Assert.Contains("tps-slow", resumedSlowWord.Classes, StringComparison.Ordinal);
            Assert.Equal(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWpm, resumedSlowWord.EffectiveWpm);

            Assert.Contains("tps-fast", fastWord.Classes, StringComparison.Ordinal);
            Assert.Equal(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsFastWpm, fastWord.EffectiveWpm);

            Assert.True(ParseMilliseconds(slowWord.DurationMs) > ParseMilliseconds(normalWord.DurationMs));
            Assert.True(ParseMilliseconds(resumedSlowWord.DurationMs) > ParseMilliseconds(normalWord.DurationMs));
            Assert.True(ParseMilliseconds(normalWord.DurationMs) > ParseMilliseconds(fastWord.DurationMs));

            Assert.True(ParsePixels(slowWord.LetterSpacing) > ParsePixels(normalWord.LetterSpacing));
            Assert.True(ParsePixels(resumedSlowWord.LetterSpacing) > ParsePixels(normalWord.LetterSpacing));
            Assert.True(ParsePixels(fastWord.LetterSpacing) < ParsePixels(normalWord.LetterSpacing));
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

        Assert.DoesNotContain("tps-neutral", neutralWord.Classes, StringComparison.Ordinal);
        Assert.DoesNotContain("tps-warm", neutralWord.Classes, StringComparison.Ordinal);
        Assert.DoesNotContain("tps-focused", neutralWord.Classes, StringComparison.Ordinal);

        Assert.Contains("tps-professional", professionalWord.Classes, StringComparison.Ordinal);
        Assert.DoesNotContain("tps-warm", professionalWord.Classes, StringComparison.Ordinal);

        Assert.Contains("tps-highlight", highlightWord.Classes, StringComparison.Ordinal);
        Assert.Contains(ReaderNextCardCssClass, highlightWord.CardClasses, StringComparison.Ordinal);
        Assert.Equal(BrowserTestConstants.TeleprompterFlow.TransparentBackgroundColor, highlightWord.BackgroundColor);

        Assert.Contains("tps-xslow", slowWord.Classes, StringComparison.Ordinal);
        Assert.Contains("tps-xfast", fastWord.Classes, StringComparison.Ordinal);
        Assert.Contains("tps-soft", softWord.Classes, StringComparison.Ordinal);
        Assert.DoesNotContain("tps-motivational", softWord.Classes, StringComparison.Ordinal);
        Assert.Contains("tps-urgent", urgentWord.Classes, StringComparison.Ordinal);
        Assert.DoesNotContain("tps-energetic", urgentWord.Classes, StringComparison.Ordinal);
        Assert.Contains("tps-rhetorical", rhetoricalWord.Classes, StringComparison.Ordinal);
        Assert.DoesNotContain("tps-energetic", teleprompterWord.Classes, StringComparison.Ordinal);

        Assert.Equal(BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionPronunciation, visionWord.Pronunciation);
        Assert.Equal(string.Empty, visionWord.Title);

        Assert.Equal(BrowserTestConstants.TeleprompterFlow.ProductLaunchTeleprompterPronunciation, teleprompterWord.Pronunciation);
        Assert.Equal(BrowserTestConstants.TeleprompterFlow.ProductLaunchTeleprompterWpm, teleprompterWord.EffectiveWpm);
        Assert.Equal(string.Empty, teleprompterWord.Title);

        Assert.True(ParseMilliseconds(slowWord.DurationMs) > ParseMilliseconds(fastWord.DurationMs));
        Assert.NotEqual(neutralWord.Color, professionalWord.Color);
        Assert.NotEqual(neutralWord.Color, softWord.Color);
        Assert.NotEqual(neutralWord.Color, urgentWord.Color);
        Assert.True(ParsePixels(slowWord.LetterSpacing) > ParsePixels(neutralWord.LetterSpacing));
        Assert.True(ParsePixels(fastWord.LetterSpacing) < ParsePixels(neutralWord.LetterSpacing));
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

        Assert.False(string.IsNullOrWhiteSpace(activeWordTestId));
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

        Assert.InRange(Math.Abs(lastDelta), 0, BrowserTestConstants.Teleprompter.AlignmentTolerancePx);
    }

    private static async Task<double> MeasureVerticalCenterDeltaAsync(
        Microsoft.Playwright.ILocator focalGuide,
        Microsoft.Playwright.ILocator word)
    {
        var focalGuideBox = await focalGuide.BoundingBoxAsync();
        var wordBox = await word.BoundingBoxAsync();

        Assert.NotNull(focalGuideBox);
        Assert.NotNull(wordBox);

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

        Assert.NotNull(probe);
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
