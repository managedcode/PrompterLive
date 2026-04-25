using System.Globalization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterFullFlowTests(StandaloneAppFixture fixture)
{
    private const int BenefitsCardIndex = 5;
    private const int ClosingCardIndex = 7;
    private const int OpeningCardIndex = 0;
    private const int PurposeCardIndex = 1;
    private const int SpeedOffsetsCardIndex = 0;
    private const int StatisticsCardIndex = 2;
    private const int InspirationCardIndex = 6;
    private const string SpeedCueDisplayModeMultiplierValue = "multiplier";
    private const string SpeedCueDisplayModeWpmValue = "wpm";
    private const string SpeedOffsetsSlowMultiplier = "x0.9";

    [Test]
    public async Task TeleprompterProductLaunch_FullTpsScenario_CapturesArtifactsAndKeepsAlignment()
    {
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.TeleprompterFullFlow.Name);
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
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

            await Expect(page.GetByTestId(UiTestIds.Teleprompter.BlockIndicator))
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
        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterSpeedOffsets);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var slowWord = await GetWordProbeAsync(page, SpeedOffsetsCardIndex, BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWord);
            var normalWord = await GetWordProbeAsync(page, SpeedOffsetsCardIndex, BrowserTestConstants.TeleprompterFlow.SpeedOffsetsNormalWord);
            var resumedSlowWord = await GetWordProbeAsync(page, SpeedOffsetsCardIndex, BrowserTestConstants.TeleprompterFlow.SpeedOffsetsResumedSlowWord);
            var fastWord = await GetWordProbeAsync(page, SpeedOffsetsCardIndex, BrowserTestConstants.TeleprompterFlow.SpeedOffsetsFastWord);

            await Assert.That(slowWord.SpeedCue).IsEqualTo(TpsVisualCueContracts.SpeedCueSlow);
            await Assert.That(slowWord.EffectiveWpm).IsEqualTo(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWpm);
            await Assert.That(slowWord.SpeedCueLabel).IsEqualTo(BuildWordsPerMinuteLabel(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWpm));
            await Assert.That(slowWord.SpeedCueDisplayMode).IsEqualTo(SpeedCueDisplayModeWpmValue);
            await Assert.That(slowWord.VisibleSpeedCueLabel).IsEqualTo(BuildWordsPerMinuteLabel(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWpm));
            await Assert.That(slowWord.VisibleSpeedCueDisplayMode).IsEqualTo(SpeedCueDisplayModeWpmValue);
            await Assert.That(slowWord.Title).IsEqualTo(string.Empty);

            await Assert.That(normalWord.EffectiveWpm).IsEqualTo(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsNormalWpm);
            await Assert.That(normalWord.SpeedCue).IsEqualTo(string.Empty);
            await Assert.That(normalWord.Style).IsEqualTo(string.Empty);
            await Assert.That(normalWord.Title).IsEqualTo(string.Empty);

            await Assert.That(resumedSlowWord.SpeedCue).IsEqualTo(TpsVisualCueContracts.SpeedCueSlow);
            await Assert.That(resumedSlowWord.EffectiveWpm).IsEqualTo(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWpm);

            await Assert.That(fastWord.SpeedCue).IsEqualTo(TpsVisualCueContracts.SpeedCueFast);
            await Assert.That(fastWord.EffectiveWpm).IsEqualTo(BrowserTestConstants.TeleprompterFlow.SpeedOffsetsFastWpm);

            await Assert.That(ParseMilliseconds(slowWord.DurationMs) > ParseMilliseconds(normalWord.DurationMs)).IsTrue();
            await Assert.That(ParseMilliseconds(resumedSlowWord.DurationMs) > ParseMilliseconds(normalWord.DurationMs)).IsTrue();
            await Assert.That(ParseMilliseconds(normalWord.DurationMs) > ParseMilliseconds(fastWord.DurationMs)).IsTrue();

            await Assert.That(ParsePixels(slowWord.LetterSpacing) > ParsePixels(normalWord.LetterSpacing)).IsTrue();
            await Assert.That(ParsePixels(resumedSlowWord.LetterSpacing) > ParsePixels(normalWord.LetterSpacing)).IsTrue();
            await Assert.That(ParsePixels(fastWord.LetterSpacing) < ParsePixels(normalWord.LetterSpacing)).IsTrue();

            await page.GetByTestId(UiTestIds.Teleprompter.SpeedCueDisplayMultiplier).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.SpeedCueDisplayMultiplier))
                .ToHaveAttributeAsync(BrowserTestConstants.State.ActiveAttribute, BrowserTestConstants.Teleprompter.ActiveStateValue);

            var multiplierSlowWord = await GetWordProbeAsync(page, SpeedOffsetsCardIndex, BrowserTestConstants.TeleprompterFlow.SpeedOffsetsSlowWord);
            await Assert.That(multiplierSlowWord.SpeedCueLabel).IsEqualTo(SpeedOffsetsSlowMultiplier);
            await Assert.That(multiplierSlowWord.SpeedCueDisplayMode).IsEqualTo(SpeedCueDisplayModeMultiplierValue);
            await Assert.That(multiplierSlowWord.VisibleSpeedCueLabel).IsEqualTo(SpeedOffsetsSlowMultiplier);
            await Assert.That(multiplierSlowWord.VisibleSpeedCueDisplayMode).IsEqualTo(SpeedCueDisplayModeMultiplierValue);
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
        var visionWord = await GetWordProbeAsync(page, InspirationCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionPronunciation);
        var rhetoricalWord = await GetWordProbeAsync(page, InspirationCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchRhetoricalWord);
        var teleprompterWord = await GetWordProbeAsync(page, ClosingCardIndex, BrowserTestConstants.TeleprompterFlow.ProductLaunchTeleprompterPronunciation);

        await Assert.That(neutralWord.EmotionCue).IsEqualTo(string.Empty);
        await Assert.That(neutralWord.VolumeCue).IsEqualTo(string.Empty);
        await Assert.That(neutralWord.DeliveryCue).IsEqualTo(string.Empty);

        await Assert.That(professionalWord.EmotionCue).IsEqualTo("professional");
        await Assert.That(professionalWord.EmotionCue).IsNotEqualTo("warm");

        await Assert.That(highlightWord.HighlightCue).IsEqualTo(TpsVisualCueContracts.HighlightAttributeValue);
        await Assert.That(highlightWord.CardState).IsEqualTo(UiDataAttributes.Teleprompter.NextState);
        await Assert.That(highlightWord.BackgroundColor).IsEqualTo(BrowserTestConstants.TeleprompterFlow.TransparentBackgroundColor);

        await Assert.That(slowWord.SpeedCue).IsEqualTo(TpsVisualCueContracts.SpeedCueXslow);
        await Assert.That(fastWord.SpeedCue).IsEqualTo(TpsVisualCueContracts.SpeedCueXfast);
        await Assert.That(softWord.VolumeCue).IsEqualTo(TpsVisualCueContracts.VolumeSoft);
        await Assert.That(softWord.EmotionCue).IsNotEqualTo("motivational");
        await Assert.That(urgentWord.EmotionCue).IsEqualTo("urgent");
        await Assert.That(urgentWord.EmotionCue).IsNotEqualTo("energetic");
        await Assert.That(rhetoricalWord.DeliveryCue).IsEqualTo("rhetorical");
        await Assert.That(teleprompterWord.EmotionCue).IsNotEqualTo("energetic");

        await Assert.That(visionWord.Pronunciation).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionPronunciation);
        await Assert.That(visionWord.OriginalText).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchVisionWord);
        await Assert.That(visionWord.Title).IsEqualTo(string.Empty);

        await Assert.That(teleprompterWord.Pronunciation).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchTeleprompterPronunciation);
        await Assert.That(teleprompterWord.OriginalText).IsEqualTo(BrowserTestConstants.TeleprompterFlow.ProductLaunchTeleprompterWord);
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
        var activeWordSelector = BrowserTestConstants.Teleprompter.ActiveWordSelector;
        var activeWord = page.Locator(activeWordSelector);
        await Expect(activeWord).ToBeVisibleAsync(new()
        {
            Timeout = BrowserTestConstants.Timing.ReaderPlaybackAdvanceTimeoutMs
        });
        await TeleprompterReaderAlignmentAssertions.AssertWordAlignedToGuideAsync(page, activeWordSelector);
    }

    private static async Task<ReaderWordProbe> GetWordProbeAsync(Microsoft.Playwright.IPage page, int cardIndex, string wordText)
    {
        var probe = await page.GetByTestId(UiTestIds.Teleprompter.CardText(cardIndex)).EvaluateAsync<ReaderWordProbe>(
            """
            (element, args) => {
                const word = Array.from(element.querySelectorAll(`[data-test^="${args.wordPrefix}"]`))
                    .find(node => node.textContent?.trim() === args.expectedWord);

                if (!(word instanceof HTMLElement)) {
                    return null;
                }

                const computed = window.getComputedStyle(word);
                const card = word.closest(`[${args.cardStateAttributeName}]`);
                const probe = {
                    cardState: card instanceof HTMLElement ? card.getAttribute(args.cardStateAttributeName) ?? '' : '',
                    style: word.getAttribute('style') ?? '',
                    title: word.getAttribute('title') ?? '',
                    originalText: word.getAttribute(args.originalTextAttributeName) ?? '',
                    pronunciation: word.getAttribute(args.pronunciationAttributeName) ?? '',
                    effectiveWpm: word.getAttribute(args.effectiveWpmAttributeName) ?? '',
                    durationMs: word.getAttribute(args.durationAttributeName) ?? '',
                    emotionCue: word.getAttribute(args.emotionAttributeName) ?? '',
                    volumeCue: word.getAttribute(args.volumeAttributeName) ?? '',
                    deliveryCue: word.getAttribute(args.deliveryAttributeName) ?? '',
                    speedCue: word.getAttribute(args.speedAttributeName) ?? '',
                    highlightCue: word.getAttribute(args.highlightAttributeName) ?? '',
                    stressCue: word.getAttribute(args.stressAttributeName) ?? '',
                    speedCueLabel: word.getAttribute(args.speedCueLabelAttributeName) ?? '',
                    speedCueDisplayMode: word.getAttribute(args.speedCueDisplayModeAttributeName) ?? '',
                    visibleSpeedCueLabel: '',
                    visibleSpeedCueDisplayMode: '',
                    letterSpacing: computed.letterSpacing ?? '',
                    color: computed.color ?? '',
                    backgroundColor: computed.backgroundColor ?? ''
                };

                const visibleLabel = word.nextElementSibling;
                if (visibleLabel instanceof HTMLElement &&
                    visibleLabel.getAttribute('data-test')?.startsWith(args.speedCueLabelPrefix)) {
                    probe.visibleSpeedCueLabel = visibleLabel.textContent?.trim() ?? '';
                    probe.visibleSpeedCueDisplayMode = visibleLabel.getAttribute(args.speedCueDisplayModeAttributeName) ?? '';
                }

                return probe;
            }
            """,
            new
            {
                cardStateAttributeName = UiDataAttributes.Teleprompter.CardState,
                durationAttributeName = UiDataAttributes.Teleprompter.DurationMilliseconds,
                effectiveWpmAttributeName = UiDataAttributes.Teleprompter.EffectiveWordsPerMinute,
                emotionAttributeName = TpsVisualCueContracts.EmotionAttributeName,
                expectedWord = wordText,
                highlightAttributeName = TpsVisualCueContracts.HighlightAttributeName,
                originalTextAttributeName = UiDataAttributes.Teleprompter.OriginalText,
                wordPrefix = UiTestIds.Teleprompter.CardWordPrefix(cardIndex),
                pronunciationAttributeName = UiDataAttributes.Teleprompter.Pronunciation,
                speedAttributeName = TpsVisualCueContracts.SpeedAttributeName,
                speedCueDisplayModeAttributeName = UiDataAttributes.Teleprompter.SpeedCueDisplayMode,
                speedCueLabelAttributeName = UiDataAttributes.Teleprompter.SpeedCueLabel,
                speedCueLabelPrefix = UiTestIds.Teleprompter.CardWordSpeedCueLabelPrefix(cardIndex),
                stressAttributeName = TpsVisualCueContracts.StressAttributeName,
                volumeAttributeName = TpsVisualCueContracts.VolumeAttributeName,
                deliveryAttributeName = TpsVisualCueContracts.DeliveryAttributeName
            });

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

    private static string BuildWordsPerMinuteLabel(string speedWpm) => $"{speedWpm} WPM";

    private sealed class ReaderWordProbe
    {
        public string CardState { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string Pronunciation { get; set; } = string.Empty;
        public string EffectiveWpm { get; set; } = string.Empty;
        public string DurationMs { get; set; } = string.Empty;
        public string EmotionCue { get; set; } = string.Empty;
        public string VolumeCue { get; set; } = string.Empty;
        public string DeliveryCue { get; set; } = string.Empty;
        public string SpeedCue { get; set; } = string.Empty;
        public string SpeedCueDisplayMode { get; set; } = string.Empty;
        public string SpeedCueLabel { get; set; } = string.Empty;
        public string HighlightCue { get; set; } = string.Empty;
        public string StressCue { get; set; } = string.Empty;
        public string VisibleSpeedCueDisplayMode { get; set; } = string.Empty;
        public string VisibleSpeedCueLabel { get; set; } = string.Empty;
        public string LetterSpacing { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
    }
}
