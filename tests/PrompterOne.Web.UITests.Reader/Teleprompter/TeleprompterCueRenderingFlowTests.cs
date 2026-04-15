using System.Globalization;
using Microsoft.Playwright;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterCueRenderingFlowTests(StandaloneAppFixture fixture)
{
    private const string CueScenario = "teleprompter-tps-cue-rendering";
    private const string CueMatrixScenario = "teleprompter-tps-cue-matrix";
    private const int InspirationCardIndex = 6;
    private const int ActiveWordProbeTimeoutMs = 1_000;
    private const double MinimumReadablePronunciationMainFontSizePx = 24d;
    private const string StepName = "01-teleprompter-cue-rendering";
    private const string CueTextStepName = "02-teleprompter-cue-text";
    private static readonly CueMatrixCapture[] CueMatrixCaptures =
    [
        new(0, "01-structure-baseline", "Structure baseline", TargetWord: "baseline"),
        new(1, "02-pause-slash", "Pause slash", ExpectedPauseCount: 1, TargetWord: "slash"),
        new(2, "03-pause-double-slash", "Pause double slash", ExpectedPauseCount: 1, TargetWord: "double"),
        new(3, "04-pause-500ms", "Pause 500ms", ExpectedPauseCount: 1, TargetWord: "pause"),
        new(4, "05-pause-1s", "Pause 1s", ExpectedPauseCount: 1, TargetWord: "silence"),
        new(5, "06-breath", "Breath", ExpectedBreathCount: 1, TargetWord: "breath"),
        new(6, "07-speed-xslow", "[xslow]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueXslow, SpeedProbeKey: "xslow", TargetWord: "xslow"),
        new(7, "08-speed-slow", "[slow]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueSlow, SpeedProbeKey: "slow", TargetWord: "slow"),
        new(8, "09-speed-normal", "[normal]", TpsVisualCueContracts.SpeedAttributeName, ExpectedNoAttribute: true, SpeedProbeKey: "normal", TargetWord: "normal"),
        new(9, "10-speed-fast", "[fast]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueFast, SpeedProbeKey: "fast", TargetWord: "fast"),
        new(10, "11-speed-xfast", "[xfast]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueXfast, SpeedProbeKey: "xfast", TargetWord: "xfast"),
        new(11, "12-speed-180wpm", "[180WPM]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueFast, SpeedProbeKey: "180wpm", TargetWord: "180wpm"),
        new(12, "13-volume-loud", "[loud]", TpsVisualCueContracts.VolumeAttributeName, TpsVisualCueContracts.VolumeLoud, TargetWord: "loud"),
        new(13, "14-volume-soft", "[soft]", TpsVisualCueContracts.VolumeAttributeName, TpsVisualCueContracts.VolumeSoft, TargetWord: "soft"),
        new(14, "15-volume-whisper", "[whisper]", TpsVisualCueContracts.VolumeAttributeName, TpsVisualCueContracts.VolumeWhisper, TargetWord: "whisper"),
        new(15, "16-emotion-warm", "[warm]", TpsVisualCueContracts.EmotionAttributeName, "warm", TargetWord: "warm"),
        new(16, "17-emotion-urgent", "[urgent]", TpsVisualCueContracts.EmotionAttributeName, "urgent", TargetWord: "urgent"),
        new(17, "18-emotion-excited", "[excited]", TpsVisualCueContracts.EmotionAttributeName, "excited", TargetWord: "excited"),
        new(18, "19-emotion-happy", "[happy]", TpsVisualCueContracts.EmotionAttributeName, "happy", TargetWord: "happy"),
        new(19, "20-emotion-sad", "[sad]", TpsVisualCueContracts.EmotionAttributeName, "sad", TargetWord: "sad"),
        new(20, "21-emotion-calm", "[calm]", TpsVisualCueContracts.EmotionAttributeName, "calm", TargetWord: "calm"),
        new(21, "22-emotion-energetic", "[energetic]", TpsVisualCueContracts.EmotionAttributeName, "energetic", TargetWord: "energetic"),
        new(22, "23-emotion-professional", "[professional]", TpsVisualCueContracts.EmotionAttributeName, "professional", TargetWord: "professional"),
        new(23, "24-emotion-focused", "[focused]", TpsVisualCueContracts.EmotionAttributeName, "focused", TargetWord: "focused"),
        new(24, "25-emotion-concerned", "[concerned]", TpsVisualCueContracts.EmotionAttributeName, "concerned", TargetWord: "concerned"),
        new(25, "26-emotion-motivational", "[motivational]", TpsVisualCueContracts.EmotionAttributeName, "motivational", TargetWord: "motivational"),
        new(26, "27-emotion-neutral", "[neutral]", TpsVisualCueContracts.EmotionAttributeName, "neutral", TargetWord: "neutral"),
        new(27, "28-delivery-aside", "[aside]", TpsVisualCueContracts.DeliveryAttributeName, "aside", TargetWord: "aside"),
        new(28, "29-delivery-rhetorical", "[rhetorical]", TpsVisualCueContracts.DeliveryAttributeName, "rhetorical", TargetWord: "rhetorical"),
        new(29, "30-delivery-building", "[building]", TpsVisualCueContracts.DeliveryAttributeName, TpsVisualCueContracts.DeliveryModeBuilding, TargetWord: "building"),
        new(30, "31-delivery-sarcasm", "[sarcasm]", TpsVisualCueContracts.DeliveryAttributeName, "sarcasm", TargetWord: "sarcasm"),
        new(31, "32-articulation-legato", "[legato]", TpsVisualCueContracts.ArticulationAttributeName, TpsVisualCueContracts.ArticulationLegato, TargetWord: "legato"),
        new(32, "33-articulation-staccato", "[staccato]", TpsVisualCueContracts.ArticulationAttributeName, TpsVisualCueContracts.ArticulationStaccato, TargetWord: "staccato"),
        new(33, "34-contour-energy", "[energy:8]", TpsVisualCueContracts.EnergyAttributeName, "8", TargetWord: "energy"),
        new(34, "35-contour-melody", "[melody:3]", TpsVisualCueContracts.MelodyAttributeName, "3", TargetWord: "melody"),
        new(35, "36-editorial-highlight", "[highlight]", TpsVisualCueContracts.HighlightAttributeName, TpsVisualCueContracts.HighlightAttributeValue, TargetWord: "highlight"),
        new(36, "37-editorial-emphasis", "[emphasis]", TpsVisualCueContracts.EmphasisAttributeName, TpsVisualCueContracts.EmphasisTag, ExpectEmphasis: true, TargetWord: "emphasis"),
        new(37, "38-markdown-bold", "Markdown bold", TpsVisualCueContracts.EmphasisAttributeName, TpsVisualCueContracts.EmphasisMarkdownStrong, ExpectEmphasis: true, TargetWord: "bold"),
        new(38, "39-markdown-italic", "Markdown italic", TpsVisualCueContracts.EmphasisAttributeName, TpsVisualCueContracts.EmphasisMarkdownItalic, ExpectEmphasis: true, TargetWord: "italic"),
        new(39, "40-guide-pronunciation", "[pronunciation:/prəˌnʌnsiˈeɪʃən/]", UiDataAttributes.Teleprompter.Pronunciation, "/prəˌnʌnsiˈeɪʃən/", TargetWord: "/prəˌnʌnsiˈeɪʃən/", ExpectedOriginalText: "pronunciation"),
        new(40, "41-guide-phonetic", "[phonetic:/fəˈnɛtɪk/]", UiDataAttributes.Teleprompter.Pronunciation, "/fəˈnɛtɪk/", TargetWord: "/fəˈnɛtɪk/", ExpectedOriginalText: "phonetic"),
        new(41, "42-guide-stress", "[stress:rising]", TpsVisualCueContracts.StressAttributeName, TpsVisualCueContracts.StressAttributeValue, TargetWord: "stress"),
        new(42, "43-edit-point", "[edit_point]", TargetWord: "edit", ExpectedEditPointPriority: TpsVisualCueContracts.EditPointStandard),
        new(43, "44-edit-point-medium", "[edit_point:medium]", TargetWord: "medium", ExpectedEditPointPriority: TpsVisualCueContracts.EditPointMedium),
        new(44, "45-edit-point-high", "[edit_point:high]", TargetWord: "high", ExpectedEditPointPriority: TpsVisualCueContracts.EditPointHigh),
        new(45, "46-metadata-speaker", "Speaker metadata", TargetWord: "speaker"),
        new(46, "47-metadata-archetype", "Archetype metadata", TargetWord: "archetype"),
        new(47, "48-phrase-speed-slow", "[slow] phrase", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueSlow, TargetWord: "slow", ExpectedAttributeMatchCount: 2),
        new(48, "49-phrase-emotion-urgent", "[urgent] phrase", TpsVisualCueContracts.EmotionAttributeName, "urgent", TargetWord: "urgent", ExpectedAttributeMatchCount: 2),
        new(49, "50-phrase-volume-loud", "[loud] phrase", TpsVisualCueContracts.VolumeAttributeName, TpsVisualCueContracts.VolumeLoud, TargetWord: "loud", ExpectedAttributeMatchCount: 2),
        new(50, "51-phrase-delivery-building", "[building] phrase", TpsVisualCueContracts.DeliveryAttributeName, TpsVisualCueContracts.DeliveryModeBuilding, TargetWord: "building", ExpectedAttributeMatchCount: 2),
        new(51, "52-phrase-articulation-legato", "[legato] phrase", TpsVisualCueContracts.ArticulationAttributeName, TpsVisualCueContracts.ArticulationLegato, TargetWord: "legato", ExpectedAttributeMatchCount: 2)
    ];

    [Test]
    public async Task TeleprompterDemo_RendersTypographyDrivenCueVariablesForVolumeAndDeliveryTexture()
    {
        UiScenarioArtifacts.ResetScenario(CueScenario);

        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var cardText = page.GetByTestId(UiTestIds.Teleprompter.CardText(InspirationCardIndex));
            var probe = await cardText.EvaluateAsync<TeleprompterCueProbe>(
                $$"""
                host => {
                    const nodes = [...host.querySelectorAll('*')];
                    const soft = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') === '{{TpsVisualCueContracts.VolumeSoft}}');
                    const loud = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') === '{{TpsVisualCueContracts.VolumeLoud}}');
                    const buildingWords = nodes.filter(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.DeliveryAttributeName}}') === '{{TpsVisualCueContracts.DeliveryModeBuilding}}');
                    const legato = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.ArticulationAttributeName}}') === '{{TpsVisualCueContracts.ArticulationLegato}}');
                    const staccato = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.ArticulationAttributeName}}') === '{{TpsVisualCueContracts.ArticulationStaccato}}');
                    const energy = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.EnergyAttributeName}}') === '8');
                    const melody = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.MelodyAttributeName}}') === '4');
                    const breath = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.BreathAttributeName}}') === '{{TpsVisualCueContracts.BreathAttributeValue}}');

                    const readVariable = (element, name) => {
                        if (!(element instanceof HTMLElement)) {
                            return '';
                        }

                        return getComputedStyle(element).getPropertyValue(name).trim();
                    };

                    return {
                        softVolume: soft?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') ?? '',
                        loudVolume: loud?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') ?? '',
                        firstBuildingDelivery: buildingWords[0]?.getAttribute('{{TpsVisualCueContracts.DeliveryAttributeName}}') ?? '',
                        lastBuildingDelivery: buildingWords.at(-1)?.getAttribute('{{TpsVisualCueContracts.DeliveryAttributeName}}') ?? '',
                        legatoArticulation: legato?.getAttribute('{{TpsVisualCueContracts.ArticulationAttributeName}}') ?? '',
                        staccatoArticulation: staccato?.getAttribute('{{TpsVisualCueContracts.ArticulationAttributeName}}') ?? '',
                        energyValue: energy?.getAttribute('{{TpsVisualCueContracts.EnergyAttributeName}}') ?? '',
                        melodyValue: melody?.getAttribute('{{TpsVisualCueContracts.MelodyAttributeName}}') ?? '',
                        breathValue: breath?.getAttribute('{{TpsVisualCueContracts.BreathAttributeName}}') ?? '',
                        softOpacity: readVariable(soft, '{{TpsVisualCueContracts.CueOpacityVariableName}}'),
                        loudWeight: readVariable(loud, '{{TpsVisualCueContracts.CueWeightVariableName}}'),
                        firstBuildingProgress: readVariable(buildingWords[0], '{{TpsVisualCueContracts.CueBuildProgressVariableName}}'),
                        lastBuildingProgress: readVariable(buildingWords.at(-1), '{{TpsVisualCueContracts.CueBuildProgressVariableName}}'),
                        firstBuildingWeight: readVariable(buildingWords[0], '{{TpsVisualCueContracts.CueWeightVariableName}}'),
                        lastBuildingWeight: readVariable(buildingWords.at(-1), '{{TpsVisualCueContracts.CueWeightVariableName}}'),
                        energyWeight: readVariable(energy, '{{TpsVisualCueContracts.CueWeightVariableName}}'),
                        melodyWeight: readVariable(melody, '{{TpsVisualCueContracts.CueWeightVariableName}}'),
                        energyTone: readVariable(energy, '{{TpsVisualCueContracts.EnergyVariableName}}'),
                        melodyTone: readVariable(melody, '{{TpsVisualCueContracts.MelodyVariableName}}')
                    };
                }
                """);

            await Assert.That(probe.SoftVolume).IsEqualTo(TpsVisualCueContracts.VolumeSoft);
            await Assert.That(probe.LoudVolume).IsEqualTo(TpsVisualCueContracts.VolumeLoud);
            await Assert.That(probe.FirstBuildingDelivery).IsEqualTo(TpsVisualCueContracts.DeliveryModeBuilding);
            await Assert.That(probe.LastBuildingDelivery).IsEqualTo(TpsVisualCueContracts.DeliveryModeBuilding);
            await Assert.That(probe.LegatoArticulation).IsEqualTo(TpsVisualCueContracts.ArticulationLegato);
            await Assert.That(probe.StaccatoArticulation).IsEqualTo(TpsVisualCueContracts.ArticulationStaccato);
            await Assert.That(probe.EnergyValue).IsEqualTo("8");
            await Assert.That(probe.MelodyValue).IsEqualTo("4");
            await Assert.That(probe.BreathValue).IsEqualTo(TpsVisualCueContracts.BreathAttributeValue);
            await Assert.That(double.Parse(probe.SoftOpacity, CultureInfo.InvariantCulture)).IsLessThan(1d);
            await Assert.That(double.Parse(probe.LoudWeight, CultureInfo.InvariantCulture)).IsGreaterThanOrEqualTo(800d);
            await Assert.That(double.Parse(probe.LastBuildingProgress, CultureInfo.InvariantCulture)).IsGreaterThan(double.Parse(probe.FirstBuildingProgress, CultureInfo.InvariantCulture));
            await Assert.That(double.Parse(probe.LastBuildingWeight, CultureInfo.InvariantCulture)).IsGreaterThan(double.Parse(probe.FirstBuildingWeight, CultureInfo.InvariantCulture));
            await Assert.That(double.Parse(probe.EnergyWeight, CultureInfo.InvariantCulture)).IsGreaterThan(700d);
            await Assert.That(double.Parse(probe.MelodyWeight, CultureInfo.InvariantCulture)).IsGreaterThan(640d);
            await Assert.That(double.Parse(probe.EnergyTone, CultureInfo.InvariantCulture)).IsEqualTo(0.778d);
            await Assert.That(double.Parse(probe.MelodyTone, CultureInfo.InvariantCulture)).IsEqualTo(0.333d);
            await AssertReaderWordsDoNotOverlapAsync(cardText, InspirationCardIndex);

            for (var index = 0; index < InspirationCardIndex; index++)
            {
                await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
            }

            await Expect(cardText).ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            var activeWord = page.GetByTestId(UiTestIds.Teleprompter.ActiveWord);
            for (var index = 0; index < 60; index++)
            {
                var text = await activeWord.TextContentAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
                if (text?.Contains("steady", StringComparison.OrdinalIgnoreCase) == true)
                {
                    break;
                }

                await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
            }

            await Expect(activeWord).ToContainTextAsync("steady", new()
            {
                Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
            });
            await UiScenarioArtifacts.CapturePageAsync(page, CueScenario, StepName);
            await UiScenarioArtifacts.CaptureLocatorAsync(cardText, CueScenario, CueTextStepName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task TeleprompterCueMatrix_CapturesReadmeExamplesForEveryCueFamily()
    {
        UiScenarioArtifacts.ResetScenario(CueMatrixScenario);

        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.SetViewportSizeAsync(BrowserTestConstants.Learn.DemoViewportWidth, BrowserTestConstants.Learn.DemoViewportHeight);
            await ReaderRouteDriver.OpenTeleprompterAsync(page, BrowserTestConstants.Routes.TeleprompterTpsCueMatrix);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await AssertCueMatrixCardCountAsync(page);

            var activeCardIndex = 0;
            var readerStage = page.GetByTestId(UiTestIds.Teleprompter.Stage);
            var speedProbes = new Dictionary<string, CueMatrixSpeedProbe>(StringComparer.Ordinal);
            foreach (var capture in CueMatrixCaptures)
            {
                activeCardIndex = await ActivateCueMatrixCardAsync(page, activeCardIndex, capture.CardIndex);
                var cardText = page.GetByTestId(UiTestIds.Teleprompter.CardText(capture.CardIndex));
                var cardCluster = page.GetByTestId(UiTestIds.Teleprompter.CardCluster(capture.CardIndex));
                await Expect(cardCluster).ToBeVisibleAsync(new()
                {
                    Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
                });
                await ActivateCueMatrixTargetWordAsync(page, capture);
                await AssertCueMatrixCardContractAsync(cardCluster, capture);
                await AssertReaderWordsDoNotOverlapAsync(cardText, capture.CardIndex);
                if (!string.IsNullOrWhiteSpace(capture.SpeedProbeKey))
                {
                    speedProbes[capture.SpeedProbeKey] = await ReadCueMatrixSpeedProbeAsync(cardText, capture);
                }

                await UiScenarioArtifacts.CaptureLocatorAsync(readerStage, CueMatrixScenario, capture.StepName);
            }

            await AssertCueMatrixSpeedTreatmentAsync(speedProbes);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static async Task<int> ActivateCueMatrixCardAsync(
        Microsoft.Playwright.IPage page,
        int activeCardIndex,
        int cardIndex)
    {
        while (activeCardIndex < cardIndex)
        {
            await page.GetByTestId(UiTestIds.Teleprompter.NextBlock).ClickAsync();
            activeCardIndex++;
        }

        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Card(cardIndex))).ToHaveAttributeAsync(
            UiDataAttributes.Teleprompter.CardState,
            UiDataAttributes.Teleprompter.ActiveState,
            new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
        return activeCardIndex;
    }

    private static async Task ActivateCueMatrixTargetWordAsync(
        Microsoft.Playwright.IPage page,
        CueMatrixCapture capture)
    {
        var activeWord = page.GetByTestId(UiTestIds.Teleprompter.ActiveWord);
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var text = await TryReadActiveReaderWordAsync(activeWord);
            if (string.Equals(
                    NormalizeReaderWord(text),
                    capture.TargetWord,
                    StringComparison.Ordinal))
            {
                return;
            }

            await page.GetByTestId(UiTestIds.Teleprompter.NextWord).ClickAsync();
        }

        throw new InvalidOperationException(
            $"Unable to activate central TPS cue word for '{capture.CueLabel}'.");
    }

    private static async Task<string?> TryReadActiveReaderWordAsync(ILocator activeWord)
    {
        try
        {
            return await activeWord.TextContentAsync(new()
            {
                Timeout = ActiveWordProbeTimeoutMs
            });
        }
        catch (TimeoutException)
        {
            return null;
        }
        catch (PlaywrightException)
        {
            return null;
        }
    }

    private static async Task AssertCueMatrixCardCountAsync(Microsoft.Playwright.IPage page)
    {
        await Expect(page.GetByTestId(UiTestIds.Teleprompter.Card(CueMatrixCaptures.Length - 1)))
            .ToBeAttachedAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
    }

    private static async Task AssertCueMatrixCardContractAsync(
        Microsoft.Playwright.ILocator cardCluster,
        CueMatrixCapture capture)
    {
        var probe = await cardCluster.EvaluateAsync<CueMatrixCardProbe>(
            """
            (host, args) => {
                const normalize = value => (value ?? '')
                    .trim()
                    .toLowerCase()
                    .replace(/[.,;:!?]+$/g, '');
                const words = [...host.querySelectorAll(`[data-test="${args.activeWordTestId}"], [data-test^="${args.wordPrefix}"]`)];
                const target = words.find(candidate => normalize(candidate.textContent) === args.targetWord);
                const attributeWords = args.attributeName && args.attributeValue
                    ? words.filter(candidate => candidate?.getAttribute(args.attributeName) === args.attributeValue)
                    : [];
                const pronunciationContent = target instanceof HTMLElement
                    ? getComputedStyle(target, '::after').content ?? ''
                    : '';
                const pronunciationFontSize = target instanceof HTMLElement
                    ? getComputedStyle(target, '::after').fontSize ?? ''
                    : '';
                const editPoint = args.expectedEditPointPriority
                    ? host.querySelector(`[${args.editPointAttributeName}="${args.expectedEditPointPriority}"]`)
                    : null;
                const editPointBeforeStyle = editPoint instanceof HTMLElement
                    ? getComputedStyle(editPoint, '::before')
                    : null;
                const editPointAfterStyle = editPoint instanceof HTMLElement
                    ? getComputedStyle(editPoint, '::after')
                    : null;
                const targetStyle = target instanceof HTMLElement
                    ? getComputedStyle(target)
                    : null;
                const hostStyle = getComputedStyle(host);
                const targetBeforeStyle = target instanceof HTMLElement
                    ? getComputedStyle(target, '::before')
                    : null;
                const targetAfterStyle = target instanceof HTMLElement
                    ? getComputedStyle(target, '::after')
                    : null;
                const targetGroup = target instanceof HTMLElement
                    ? target.closest('.rd-g')
                    : null;
                const targetGroupStyle = targetGroup instanceof HTMLElement
                    ? getComputedStyle(targetGroup)
                    : null;
                const targetGroupBeforeStyle = targetGroup instanceof HTMLElement
                    ? getComputedStyle(targetGroup, '::before')
                    : null;
                const targetGroupAfterStyle = targetGroup instanceof HTMLElement
                    ? getComputedStyle(targetGroup, '::after')
                    : null;
                const groupRect = targetGroup instanceof HTMLElement
                    ? targetGroup.getBoundingClientRect()
                    : null;
                const attributeRects = attributeWords
                    .filter(candidate => candidate instanceof HTMLElement)
                    .map(candidate => candidate.getBoundingClientRect())
                    .filter(rect => rect.width > 0 && rect.height > 0);
                const attributeUnion = attributeRects.length > 0
                    ? {
                        left: Math.min(...attributeRects.map(rect => rect.left)),
                        right: Math.max(...attributeRects.map(rect => rect.right))
                    }
                    : null;
                return {
                    rawTagsVisible: /\[[^\]]+\]/.test(host.textContent ?? ''),
                    targetText: target?.textContent?.trim() ?? '',
                    originalText: target?.getAttribute(args.originalTextAttributeName) ?? '',
                    attributeValue: args.attributeName ? target?.getAttribute(args.attributeName) ?? '' : '',
                    attributeMatchCount: attributeWords.length,
                    editPointMatchCount: args.expectedEditPointPriority
                        ? host.querySelectorAll(`[${args.editPointAttributeName}="${args.expectedEditPointPriority}"]`).length
                        : 0,
                    editPointBeforeBorderLeftColor: editPointBeforeStyle?.borderLeftColor ?? '',
                    editPointAfterBorderRightColor: editPointAfterStyle?.borderRightColor ?? '',
                    editPointAfterBorderTopColor: editPointAfterStyle?.borderTopColor ?? '',
                    pauseCueCount: host.querySelectorAll('.rd-pause').length,
                    breathCueCount: host.querySelectorAll('[data-tps-breath="true"]').length,
                    emphasisGroupCount: host.querySelectorAll('[data-emphasis="true"]').length,
                    pronunciationContent,
                    pronunciationFontSize,
                    targetFontStyle: targetStyle?.fontStyle ?? '',
                    targetFontSize: targetStyle?.fontSize ?? '',
                    targetFontWeight: targetStyle?.fontWeight ?? '',
                    hostFontSize: hostStyle.fontSize ?? '',
                    targetLetterSpacing: targetStyle?.letterSpacing ?? '',
                    targetBackgroundImage: targetStyle?.backgroundImage ?? '',
                    targetBoxShadow: targetStyle?.boxShadow ?? '',
                    targetBeforeBackgroundImage: targetBeforeStyle?.backgroundImage ?? '',
                    targetBeforeContent: targetBeforeStyle?.content ?? '',
                    targetTextDecorationLine: targetStyle?.textDecorationLine ?? '',
                    targetTextDecorationStyle: targetStyle?.textDecorationStyle ?? '',
                    targetTextDecorationThickness: targetStyle?.textDecorationThickness ?? '',
                    targetAfterContent: targetAfterStyle?.content ?? '',
                    targetAfterBackgroundImage: targetAfterStyle?.backgroundImage ?? '',
                    targetAfterBorderBottomColor: targetAfterStyle?.borderBottomColor ?? '',
                    targetAfterBorderBottomStyle: targetAfterStyle?.borderBottomStyle ?? '',
                    targetAfterBorderBottomWidth: targetAfterStyle?.borderBottomWidth ?? '',
                    targetAfterBorderTopColor: targetAfterStyle?.borderTopColor ?? '',
                    targetAfterBorderTopStyle: targetAfterStyle?.borderTopStyle ?? '',
                    targetAfterBorderTopWidth: targetAfterStyle?.borderTopWidth ?? '',
                    targetGroupTextDecorationLine: targetGroupStyle?.textDecorationLine ?? '',
                    targetGroupBeforeContent: targetGroupBeforeStyle?.content ?? '',
                    targetGroupBeforeBackgroundImage: targetGroupBeforeStyle?.backgroundImage ?? '',
                    targetGroupAfterContent: targetGroupAfterStyle?.content ?? '',
                    targetGroupAfterBackgroundImage: targetGroupAfterStyle?.backgroundImage ?? '',
                    targetGroupAfterBorderBottomColor: targetGroupAfterStyle?.borderBottomColor ?? '',
                    targetGroupAfterBorderBottomStyle: targetGroupAfterStyle?.borderBottomStyle ?? '',
                    targetGroupAfterBorderBottomWidth: targetGroupAfterStyle?.borderBottomWidth ?? '',
                    targetGroupWidth: groupRect?.width ?? 0,
                    attributeUnionWidth: attributeUnion ? attributeUnion.right - attributeUnion.left : 0
                };
            }
            """,
            new
            {
                attributeName = capture.AttributeName,
                attributeValue = capture.AttributeValue,
                activeWordTestId = UiTestIds.Teleprompter.ActiveWord,
                editPointAttributeName = TpsVisualCueContracts.EditPointAttributeName,
                expectedEditPointPriority = capture.ExpectedEditPointPriority,
                originalTextAttributeName = UiDataAttributes.Teleprompter.OriginalText,
                targetWord = capture.TargetWord,
                wordPrefix = UiTestIds.Teleprompter.CardWordPrefix(capture.CardIndex)
            });

        await Assert.That(probe.RawTagsVisible)
            .IsFalse()
            .Because($"Expected raw TPS tags to be hidden in cue example '{capture.CueLabel}'.");
        await Assert.That(probe.TargetText)
            .IsEqualTo(capture.TargetWord)
            .Because($"Expected cue example '{capture.CueLabel}' to keep the central reader word visible.");

        if (!string.IsNullOrWhiteSpace(capture.AttributeName))
        {
            if (capture.ExpectedNoAttribute)
            {
                await Assert.That(probe.AttributeValue)
                    .IsEqualTo(string.Empty)
                    .Because($"Expected cue example '{capture.CueLabel}' to reset without a visible data attribute.");
            }
            else
            {
                await Assert.That(probe.AttributeValue)
                    .IsEqualTo(capture.AttributeValue ?? string.Empty)
                    .Because($"Expected cue example '{capture.CueLabel}' to expose its reader visual contract.");
                await Assert.That(probe.AttributeMatchCount)
                    .IsEqualTo(capture.ExpectedAttributeMatchCount)
                    .Because($"Expected cue example '{capture.CueLabel}' to style the expected cue scope.");
            }
        }

        if (string.Equals(capture.AttributeName, UiDataAttributes.Teleprompter.Pronunciation, StringComparison.Ordinal))
        {
            await Assert.That(probe.AttributeValue)
                .IsEqualTo(capture.AttributeValue ?? string.Empty)
                .Because($"Expected cue example '{capture.CueLabel}' to keep the pronunciation guide as data.");
            await Assert.That(probe.OriginalText)
                .IsEqualTo(capture.ExpectedOriginalText ?? string.Empty)
                .Because($"Expected cue example '{capture.CueLabel}' to keep the original spelling as secondary context.");
            await Assert.That(ParseCssPixels(probe.TargetFontSize))
                .IsGreaterThanOrEqualTo(MinimumReadablePronunciationMainFontSizePx)
                .Because($"Expected cue example '{capture.CueLabel}' to render the guide itself as the primary readable word.");
            await Assert.That(probe.PronunciationContent)
                .Contains(capture.ExpectedOriginalText ?? string.Empty)
                .Because($"Expected cue example '{capture.CueLabel}' to show the original spelling only as compact secondary text.");
            await Assert.That(ParseCssPixels(probe.PronunciationFontSize))
                .IsLessThan(ParseCssPixels(probe.TargetFontSize))
                .Because($"Expected cue example '{capture.CueLabel}' to keep original spelling smaller than the pronunciation guide.");
        }

        if (string.Equals(capture.AttributeName, TpsVisualCueContracts.VolumeAttributeName, StringComparison.Ordinal))
        {
            if (string.Equals(capture.AttributeValue, TpsVisualCueContracts.VolumeLoud, StringComparison.Ordinal))
            {
                await Assert.That(ParseCssPixels(probe.TargetFontSize))
                    .IsGreaterThan(ParseCssPixels(probe.HostFontSize))
                    .Because("Expected loud to carry a visibly larger reader word.");
                await Assert.That(ParseCssNumber(probe.TargetFontWeight))
                    .IsGreaterThanOrEqualTo(800d)
                    .Because("Expected loud to carry stronger reader weight.");
            }
            else if (string.Equals(capture.AttributeValue, TpsVisualCueContracts.VolumeWhisper, StringComparison.Ordinal))
            {
                await Assert.That(probe.TargetFontStyle)
                    .IsEqualTo("italic")
                    .Because("Expected whisper to have a distinct breathy shape cue.");
                await Assert.That(probe.TargetTextDecorationStyle)
                    .IsEqualTo("dotted")
                    .Because("Expected whisper to stay visually distinct from soft volume.");
            }
            else if (string.Equals(capture.AttributeValue, TpsVisualCueContracts.VolumeSoft, StringComparison.Ordinal))
            {
                await Assert.That(probe.TargetFontStyle)
                    .IsNotEqualTo("italic")
                    .Because("Expected soft volume to stay visually separate from whisper.");
            }
        }

        if (string.Equals(capture.AttributeName, TpsVisualCueContracts.DeliveryAttributeName, StringComparison.Ordinal) &&
            string.Equals(capture.AttributeValue, TpsVisualCueContracts.DeliveryModeBuilding, StringComparison.Ordinal))
        {
            var buildingHairpinImage = HasVisiblePseudoBackground(probe.TargetGroupAfterBackgroundImage)
                ? probe.TargetGroupAfterBackgroundImage
                : probe.TargetAfterBackgroundImage;
            await Assert.That(HasVisiblePseudoBackground(buildingHairpinImage))
                .IsTrue()
                .Because("Expected building delivery to read as a crescendo-style hairpin cue.");
            await Assert.That(buildingHairpinImage)
                .Contains("image/svg+xml")
                .Because("Expected building delivery to use a crisp vector hairpin instead of CSS diagonal gradients.");
            await Assert.That(buildingHairpinImage)
                .DoesNotContain("linear-gradient")
                .Because("Expected building delivery to avoid pixel-stepped diagonal gradient strokes.");
            await Assert.That(buildingHairpinImage)
                .DoesNotContain("radial-gradient")
                .Because("Expected building delivery to avoid raster-like gradient marks.");

            if (capture.ExpectedAttributeMatchCount > 1)
            {
                await Assert.That(IsDisabledPseudoContent(probe.TargetAfterContent))
                    .IsTrue()
                    .Because("Expected phrase building to render one group-level crescendo hairpin, not one hairpin per word.");
                await Assert.That(probe.TargetGroupWidth)
                    .IsGreaterThanOrEqualTo(probe.AttributeUnionWidth - 1d)
                    .Because("Expected phrase building to scope the crescendo marker across the full cue phrase.");
            }

            await Assert.That(probe.TargetTextDecorationLine)
                .DoesNotContain("underline")
                .Because("Expected building to use the crescendo hairpin instead of a generic active underline.");
        }

        if (string.Equals(capture.AttributeName, TpsVisualCueContracts.ArticulationAttributeName, StringComparison.Ordinal))
        {
            if (string.Equals(capture.AttributeValue, TpsVisualCueContracts.ArticulationLegato, StringComparison.Ordinal))
            {
                var hasGroupLegatoSlur = string.Equals(probe.TargetGroupAfterBorderBottomStyle, "solid", StringComparison.Ordinal);
                var legatoSlurImage = hasGroupLegatoSlur
                    ? probe.TargetGroupAfterBackgroundImage
                    : probe.TargetAfterBackgroundImage;
                var legatoSlurStyle = hasGroupLegatoSlur
                    ? probe.TargetGroupAfterBorderBottomStyle
                    : probe.TargetAfterBorderBottomStyle;
                var legatoSlurWidth = hasGroupLegatoSlur
                    ? probe.TargetGroupAfterBorderBottomWidth
                    : probe.TargetAfterBorderBottomWidth;
                var legatoSlurColor = hasGroupLegatoSlur
                    ? probe.TargetGroupAfterBorderBottomColor
                    : probe.TargetAfterBorderBottomColor;

                await Assert.That(IsDisabledPseudoBackground(legatoSlurImage))
                    .IsTrue()
                    .Because("Expected legato to use a vector border arc instead of a stretched background image.");
                await Assert.That(legatoSlurImage)
                    .DoesNotContain("radial-gradient")
                    .Because("Expected legato slurs to avoid jagged radial-gradient rastering.");
                await Assert.That(legatoSlurImage)
                    .DoesNotContain("linear-gradient")
                    .Because("Expected legato slurs to avoid pixel-stepped gradient strokes.");
                await Assert.That(legatoSlurStyle)
                    .IsEqualTo("solid")
                    .Because("Expected legato to render as a continuous music-like slur stroke.");
                await Assert.That(ParseCssPixels(legatoSlurWidth))
                    .IsGreaterThan(0d)
                    .Because("Expected legato slur stroke width to be visible at README screenshot scale.");
                await Assert.That(legatoSlurColor)
                    .IsNotEqualTo("rgba(0, 0, 0, 0)")
                    .Because("Expected legato slur stroke color to be visible.");
                if (capture.ExpectedAttributeMatchCount > 1)
                {
                    await Assert.That(IsDisabledPseudoContent(probe.TargetAfterContent))
                        .IsTrue()
                        .Because("Expected phrase legato to render one group-level slur, not one slur per word.");
                    await Assert.That(probe.TargetGroupWidth)
                        .IsGreaterThanOrEqualTo(probe.AttributeUnionWidth - 1d)
                        .Because("Expected phrase legato to scope the slur across the full cue phrase.");
                }

                await Assert.That(ParseCssPixels(probe.TargetLetterSpacing))
                    .IsGreaterThanOrEqualTo(0d)
                    .Because("Expected legato to connect with a slur without compressing letters into each other.");
                await Assert.That(probe.TargetTextDecorationLine)
                    .DoesNotContain("underline")
                    .Because("Expected legato to use a curved slur instead of a generic underline.");
            }
            else if (string.Equals(capture.AttributeValue, TpsVisualCueContracts.ArticulationStaccato, StringComparison.Ordinal))
            {
                await Assert.That(IsDisabledPseudoBackground(probe.TargetAfterBackgroundImage))
                    .IsTrue()
                    .Because("Expected staccato dots to avoid radial-gradient rastering.");
                await Assert.That(probe.TargetAfterBorderBottomStyle)
                    .IsEqualTo("dotted")
                    .Because("Expected staccato to read as music-like dots.");
                await Assert.That(ParseCssPixels(probe.TargetAfterBorderBottomWidth))
                    .IsGreaterThan(0d)
                    .Because("Expected staccato dots to stay visible at README screenshot scale.");
                await Assert.That(ParseCssPixels(probe.TargetLetterSpacing))
                    .IsGreaterThan(0d)
                    .Because("Expected staccato to separate the cue word rhythm.");
            }
        }

        if (string.Equals(capture.AttributeName, TpsVisualCueContracts.StressAttributeName, StringComparison.Ordinal))
        {
            await Assert.That(ParseCssNumber(probe.TargetFontWeight))
                .IsGreaterThanOrEqualTo(750d)
                .Because("Expected stress to carry visible rehearsal weight.");
            await Assert.That(probe.TargetBeforeContent)
                .Contains("ˈ")
                .Because("Expected stress to show a stress mark above the cue word.");
            await Assert.That(probe.TargetTextDecorationLine)
                .DoesNotContain("underline")
                .Because("Expected stress to use a dedicated vector stress mark instead of generic text-decoration underlines.");
            await Assert.That(probe.TargetAfterBorderTopStyle)
                .IsEqualTo("solid")
                .Because("Expected stress to render the top stroke of its double stress mark.");
            await Assert.That(probe.TargetAfterBorderBottomStyle)
                .IsEqualTo("solid")
                .Because("Expected stress to render the bottom stroke of its double stress mark.");
            await Assert.That(ParseCssPixels(probe.TargetAfterBorderTopWidth))
                .IsGreaterThanOrEqualTo(2d)
                .Because("Expected stress to have a readable top stress stroke.");
            await Assert.That(ParseCssPixels(probe.TargetAfterBorderBottomWidth))
                .IsGreaterThanOrEqualTo(2d)
                .Because("Expected stress to have a readable bottom stress stroke.");
        }

        if (!string.IsNullOrWhiteSpace(capture.ExpectedEditPointPriority))
        {
            await Assert.That(probe.EditPointMatchCount)
                .IsEqualTo(1)
                .Because($"Expected cue example '{capture.CueLabel}' to expose exactly one non-spoken edit-point marker.");
            await Assert.That(probe.EditPointBeforeBorderLeftColor)
                .IsNotEqualTo(string.Empty)
                .Because($"Expected cue example '{capture.CueLabel}' to render a visible edit-point marker.");
            await Assert.That(probe.EditPointBeforeBorderLeftColor)
                .IsNotEqualTo("rgba(0, 0, 0, 0)")
                .Because($"Expected cue example '{capture.CueLabel}' to render a visible edit-point marker.");

            if (!string.Equals(capture.ExpectedEditPointPriority, TpsVisualCueContracts.EditPointStandard, StringComparison.Ordinal))
            {
                await Assert.That(probe.EditPointAfterBorderRightColor)
                    .IsNotEqualTo(string.Empty)
                    .Because($"Expected cue example '{capture.CueLabel}' to add an extra visible priority marker.");
                await Assert.That(probe.EditPointAfterBorderRightColor)
                    .IsNotEqualTo("rgba(0, 0, 0, 0)")
                    .Because($"Expected cue example '{capture.CueLabel}' to add an extra visible priority marker.");
            }
        }

        if (string.Equals(capture.AttributeName, TpsVisualCueContracts.EmphasisAttributeName, StringComparison.Ordinal))
        {
            await Assert.That(probe.TargetGroupTextDecorationLine)
                .DoesNotContain("underline")
                .Because("Expected phrase emphasis not to draw underline strokes through the spaces between words.");
            await Assert.That(probe.TargetTextDecorationLine)
                .DoesNotContain("underline")
                .Because("Expected editorial emphasis to use word shape and weight instead of extra underline strokes.");

            if (string.Equals(capture.AttributeValue, TpsVisualCueContracts.EmphasisMarkdownItalic, StringComparison.Ordinal))
            {
                await Assert.That(probe.TargetFontStyle)
                    .IsEqualTo("italic")
                    .Because("Expected markdown italic to read as an obvious italic word shape.");
            }
            else if (string.Equals(capture.AttributeValue, TpsVisualCueContracts.EmphasisMarkdownStrong, StringComparison.Ordinal))
            {
                await Assert.That(ParseCssNumber(probe.TargetFontWeight))
                    .IsGreaterThanOrEqualTo(800d)
                    .Because("Expected markdown bold to read as a strong word shape.");
            }
            else if (string.Equals(capture.AttributeValue, TpsVisualCueContracts.EmphasisTag, StringComparison.Ordinal))
            {
                await Assert.That(probe.TargetBoxShadow)
                    .DoesNotContain("none")
                    .Because("Expected bracketed emphasis to use a distinct editorial marker, not plain markdown bold.");
            }
        }

        if (string.Equals(capture.AttributeName, TpsVisualCueContracts.HighlightAttributeName, StringComparison.Ordinal))
        {
            await Assert.That(IsDisabledPseudoContent(probe.TargetGroupBeforeContent))
                .IsFalse()
                .Because("Expected highlight to render as an intentional phrase shape behind the reader word.");
            await Assert.That(HasVisiblePseudoBackground(probe.TargetGroupBeforeBackgroundImage))
                .IsTrue()
                .Because("Expected highlight to use a shaped pseudo-element instead of a broken half-height word fill.");
            await Assert.That(IsDisabledPseudoBackground(probe.TargetBackgroundImage))
                .IsTrue()
                .Because("Expected the raw word background to stay clear so the highlight does not become a muddy half-fill.");
            await Assert.That(IsDisabledPseudoBackground(probe.TargetBeforeBackgroundImage))
                .IsTrue()
                .Because("Expected grouped highlight words to avoid stacked per-word highlight fills.");
        }

        if (capture.ExpectedPauseCount > 0)
        {
            await Assert.That(probe.PauseCueCount)
                .IsEqualTo(capture.ExpectedPauseCount)
                .Because($"Expected cue example '{capture.CueLabel}' to render one pause marker.");
        }

        if (capture.ExpectedBreathCount > 0)
        {
            await Assert.That(probe.BreathCueCount)
                .IsEqualTo(capture.ExpectedBreathCount)
                .Because($"Expected cue example '{capture.CueLabel}' to render one breath marker.");
        }

        if (capture.ExpectEmphasis)
        {
            await Assert.That(probe.EmphasisGroupCount)
                .IsGreaterThanOrEqualTo(1)
                .Because($"Expected cue example '{capture.CueLabel}' to render an emphasis group.");
        }
    }

    private static async Task<CueMatrixSpeedProbe> ReadCueMatrixSpeedProbeAsync(
        Microsoft.Playwright.ILocator cardText,
        CueMatrixCapture capture)
    {
        var probe = await cardText.EvaluateAsync<CueMatrixSpeedProbe>(
            """
            (host, args) => {
                const normalize = value => (value ?? '')
                    .trim()
                    .toLowerCase()
                    .replace(/[.,;:!?]+$/g, '');
                const target = [...host.querySelectorAll(`[data-test="${args.activeWordTestId}"], [data-test^="${args.wordPrefix}"]`)]
                    .find(candidate => normalize(candidate.textContent) === args.targetWord);
                if (!(target instanceof HTMLElement)) {
                    return null;
                }

                const computed = getComputedStyle(target);
                return {
                    letterSpacing: computed.letterSpacing ?? '',
                    fontSize: computed.fontSize ?? ''
                };
            }
            """,
            new
            {
                activeWordTestId = UiTestIds.Teleprompter.ActiveWord,
                targetWord = capture.TargetWord,
                wordPrefix = UiTestIds.Teleprompter.CardWordPrefix(capture.CardIndex)
            });

        await Assert.That(probe).IsNotNull();
        return probe!;
    }

    private static async Task AssertCueMatrixSpeedTreatmentAsync(IReadOnlyDictionary<string, CueMatrixSpeedProbe> probes)
    {
        await Assert.That(probes.ContainsKey("xslow")).IsTrue();
        await Assert.That(probes.ContainsKey("slow")).IsTrue();
        await Assert.That(probes.ContainsKey("normal")).IsTrue();
        await Assert.That(probes.ContainsKey("fast")).IsTrue();
        await Assert.That(probes.ContainsKey("xfast")).IsTrue();
        await Assert.That(probes.ContainsKey("180wpm")).IsTrue();

        var xslow = probes["xslow"];
        var slow = probes["slow"];
        var normal = probes["normal"];
        var fast = probes["fast"];
        var xfast = probes["xfast"];
        var customWpm = probes["180wpm"];

        await Assert.That(ParseCssPixels(xslow.LetterSpacing)).IsGreaterThan(ParseCssPixels(slow.LetterSpacing));
        await Assert.That(ParseCssPixels(slow.LetterSpacing)).IsGreaterThan(ParseCssPixels(normal.LetterSpacing));
        await Assert.That(ParseCssPixels(normal.LetterSpacing)).IsGreaterThan(ParseCssPixels(fast.LetterSpacing));
        await Assert.That(ParseCssPixels(fast.LetterSpacing)).IsGreaterThan(ParseCssPixels(xfast.LetterSpacing));
        await Assert.That(ParseCssPixels(customWpm.LetterSpacing)).IsLessThan(ParseCssPixels(normal.LetterSpacing));

        await Assert.That(ParseCssEmRatio(xslow.LetterSpacing, xslow.FontSize))
            .IsLessThanOrEqualTo(0.11d)
            .Because("xslow must stay visually restrained in README screenshots.");
        await Assert.That(ParseCssEmRatio(slow.LetterSpacing, slow.FontSize))
            .IsLessThanOrEqualTo(0.06d)
            .Because("slow must broaden the word without becoming a spaced-letter display.");
    }

    private static double ParseCssPixels(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "normal", StringComparison.Ordinal))
        {
            return 0d;
        }

        return double.Parse(value.Replace("px", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);
    }

    private static double ParseCssEmRatio(string value, string fontSize)
    {
        var fontSizePixels = Math.Max(1d, ParseCssPixels(fontSize));

        return ParseCssPixels(value) / fontSizePixels;
    }

    private static double ParseCssNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0d;
        }

        return double.Parse(value, CultureInfo.InvariantCulture);
    }

    private static bool HasVisiblePseudoBackground(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !string.Equals(value, "none", StringComparison.OrdinalIgnoreCase);

    private static bool IsDisabledPseudoBackground(string value) =>
        string.IsNullOrWhiteSpace(value) ||
        string.Equals(value, "none", StringComparison.OrdinalIgnoreCase);

    private static bool IsDisabledPseudoContent(string value) =>
        string.Equals(value, "none", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "normal", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeReaderWord(string? value) =>
        (value ?? string.Empty)
            .Trim()
            .TrimEnd('.', ',', ';', ':', '!', '?')
            .ToLowerInvariant();

    private static async Task AssertReaderWordsDoNotOverlapAsync(Microsoft.Playwright.ILocator cardText, int cardIndex)
    {
        var probe = await cardText.EvaluateAsync<ReaderSpacingProbe>(
            """
            (host, args) => {
                const nodes = Array.from(host.querySelectorAll(`[data-test="${args.activeWordTestId}"], [data-test^="${args.wordPrefix}"]`));
                const words = nodes
                    .filter(node => node instanceof HTMLElement)
                    .map(node => {
                        const box = node.getBoundingClientRect();
                        return {
                            text: node.textContent?.trim() ?? '',
                            left: box.left,
                            right: box.right,
                            centerY: box.top + (box.height / 2),
                            height: box.height
                        };
                    })
                    .filter(word => word.text.length > 0 && word.height > 0);

                let minimumGap = Number.POSITIVE_INFINITY;
                let overlap = '';
                for (let index = 0; index < words.length - 1; index++) {
                    const current = words[index];
                    const next = words[index + 1];
                    const sameLine = Math.abs(current.centerY - next.centerY) <= Math.min(current.height, next.height) * 0.45;
                    if (!sameLine) {
                        continue;
                    }

                    const gap = next.left - current.right;
                    minimumGap = Math.min(minimumGap, gap);
                    if (gap < -0.5) {
                        overlap = `${current.text}|${next.text}|${gap.toFixed(2)}`;
                        break;
                    }
                }

                return {
                    minimumGap: Number.isFinite(minimumGap) ? minimumGap : 0,
                    overlap
                };
            }
            """,
            new
            {
                activeWordTestId = UiTestIds.Teleprompter.ActiveWord,
                wordPrefix = UiTestIds.Teleprompter.CardWordPrefix(cardIndex)
            });

        await Assert.That(probe.Overlap)
            .IsEqualTo(string.Empty)
            .Because($"Expected TPS reader words not to overlap; minimum gap was {probe.MinimumGap}px.");
    }

    private sealed class TeleprompterCueProbe
    {
        public string SoftVolume { get; init; } = string.Empty;

        public string LoudVolume { get; init; } = string.Empty;

        public string FirstBuildingDelivery { get; init; } = string.Empty;

        public string LastBuildingDelivery { get; init; } = string.Empty;

        public string LegatoArticulation { get; init; } = string.Empty;

        public string StaccatoArticulation { get; init; } = string.Empty;

        public string EnergyValue { get; init; } = string.Empty;

        public string MelodyValue { get; init; } = string.Empty;

        public string BreathValue { get; init; } = string.Empty;

        public string SoftOpacity { get; init; } = string.Empty;

        public string LoudWeight { get; init; } = string.Empty;

        public string FirstBuildingProgress { get; init; } = string.Empty;

        public string LastBuildingProgress { get; init; } = string.Empty;

        public string FirstBuildingWeight { get; init; } = string.Empty;

        public string LastBuildingWeight { get; init; } = string.Empty;

        public string EnergyWeight { get; init; } = string.Empty;

        public string MelodyWeight { get; init; } = string.Empty;

        public string EnergyTone { get; init; } = string.Empty;

        public string MelodyTone { get; init; } = string.Empty;
    }

    private sealed class ReaderSpacingProbe
    {
        public double MinimumGap { get; init; }

        public string Overlap { get; init; } = string.Empty;
    }

    private readonly record struct CueMatrixCapture(
        int CardIndex,
        string StepName,
        string CueLabel,
        string? AttributeName = null,
        string? AttributeValue = null,
        int ExpectedPauseCount = 0,
        int ExpectedBreathCount = 0,
        bool ExpectEmphasis = false,
        bool ExpectedNoAttribute = false,
        string? SpeedProbeKey = null,
        string TargetWord = "",
        int ExpectedAttributeMatchCount = 1,
        string? ExpectedOriginalText = null,
        string? ExpectedEditPointPriority = null);

    private sealed class CueMatrixCardProbe
    {
        public bool RawTagsVisible { get; init; }

        public string TargetText { get; init; } = string.Empty;

        public string OriginalText { get; init; } = string.Empty;

        public string AttributeValue { get; init; } = string.Empty;

        public int AttributeMatchCount { get; init; }

        public int EditPointMatchCount { get; init; }

        public string EditPointBeforeBorderLeftColor { get; init; } = string.Empty;

        public string EditPointAfterBorderRightColor { get; init; } = string.Empty;

        public string EditPointAfterBorderTopColor { get; init; } = string.Empty;

        public int PauseCueCount { get; init; }

        public int BreathCueCount { get; init; }

        public int EmphasisGroupCount { get; init; }

        public string PronunciationContent { get; init; } = string.Empty;

        public string PronunciationFontSize { get; init; } = string.Empty;

        public string TargetFontStyle { get; init; } = string.Empty;

        public string TargetFontSize { get; init; } = string.Empty;

        public string TargetFontWeight { get; init; } = string.Empty;

        public string HostFontSize { get; init; } = string.Empty;

        public string TargetLetterSpacing { get; init; } = string.Empty;

        public string TargetBackgroundImage { get; init; } = string.Empty;

        public string TargetBoxShadow { get; init; } = string.Empty;

        public string TargetBeforeBackgroundImage { get; init; } = string.Empty;

        public string TargetBeforeContent { get; init; } = string.Empty;

        public string TargetTextDecorationLine { get; init; } = string.Empty;

        public string TargetTextDecorationStyle { get; init; } = string.Empty;

        public string TargetTextDecorationThickness { get; init; } = string.Empty;

        public string TargetAfterContent { get; init; } = string.Empty;

        public string TargetAfterBackgroundImage { get; init; } = string.Empty;

        public string TargetAfterBorderBottomColor { get; init; } = string.Empty;

        public string TargetAfterBorderBottomStyle { get; init; } = string.Empty;

        public string TargetAfterBorderBottomWidth { get; init; } = string.Empty;

        public string TargetAfterBorderTopColor { get; init; } = string.Empty;

        public string TargetAfterBorderTopStyle { get; init; } = string.Empty;

        public string TargetAfterBorderTopWidth { get; init; } = string.Empty;

        public string TargetGroupTextDecorationLine { get; init; } = string.Empty;

        public string TargetGroupBeforeContent { get; init; } = string.Empty;

        public string TargetGroupBeforeBackgroundImage { get; init; } = string.Empty;

        public string TargetGroupAfterContent { get; init; } = string.Empty;

        public string TargetGroupAfterBackgroundImage { get; init; } = string.Empty;

        public string TargetGroupAfterBorderBottomColor { get; init; } = string.Empty;

        public string TargetGroupAfterBorderBottomStyle { get; init; } = string.Empty;

        public string TargetGroupAfterBorderBottomWidth { get; init; } = string.Empty;

        public double TargetGroupWidth { get; init; }

        public double AttributeUnionWidth { get; init; }
    }

    private sealed class CueMatrixSpeedProbe
    {
        public string LetterSpacing { get; init; } = string.Empty;

        public string FontSize { get; init; } = string.Empty;
    }
}
