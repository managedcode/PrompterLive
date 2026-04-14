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
    private const string StepName = "01-teleprompter-cue-rendering";
    private const string CueTextStepName = "02-teleprompter-cue-text";
    private static readonly CueMatrixCapture[] CueMatrixCaptures =
    [
        new(0, "01-structure-baseline", "Structure baseline"),
        new(1, "02-pause-slash", "Pause slash", ExpectedPauseCount: 1),
        new(2, "03-pause-double-slash", "Pause double slash", ExpectedPauseCount: 1),
        new(3, "04-pause-500ms", "Pause 500ms", ExpectedPauseCount: 1),
        new(4, "05-pause-1s", "Pause 1s", ExpectedPauseCount: 1),
        new(5, "06-breath", "Breath", ExpectedBreathCount: 1),
        new(6, "07-speed-xslow", "[xslow]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueXslow),
        new(7, "08-speed-slow", "[slow]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueSlow),
        new(8, "09-speed-normal", "[normal]", TpsVisualCueContracts.SpeedAttributeName, ExpectedNoAttribute: true),
        new(9, "10-speed-fast", "[fast]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueFast),
        new(10, "11-speed-xfast", "[xfast]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueXfast),
        new(11, "12-speed-180wpm", "[180WPM]", TpsVisualCueContracts.SpeedAttributeName, TpsVisualCueContracts.SpeedCueFast),
        new(12, "13-volume-loud", "[loud]", TpsVisualCueContracts.VolumeAttributeName, TpsVisualCueContracts.VolumeLoud),
        new(13, "14-volume-soft", "[soft]", TpsVisualCueContracts.VolumeAttributeName, TpsVisualCueContracts.VolumeSoft),
        new(14, "15-volume-whisper", "[whisper]", TpsVisualCueContracts.VolumeAttributeName, TpsVisualCueContracts.VolumeWhisper),
        new(15, "16-emotion-warm", "[warm]", TpsVisualCueContracts.EmotionAttributeName, "warm"),
        new(16, "17-emotion-urgent", "[urgent]", TpsVisualCueContracts.EmotionAttributeName, "urgent"),
        new(17, "18-emotion-excited", "[excited]", TpsVisualCueContracts.EmotionAttributeName, "excited"),
        new(18, "19-emotion-happy", "[happy]", TpsVisualCueContracts.EmotionAttributeName, "happy"),
        new(19, "20-emotion-sad", "[sad]", TpsVisualCueContracts.EmotionAttributeName, "sad"),
        new(20, "21-emotion-calm", "[calm]", TpsVisualCueContracts.EmotionAttributeName, "calm"),
        new(21, "22-emotion-energetic", "[energetic]", TpsVisualCueContracts.EmotionAttributeName, "energetic"),
        new(22, "23-emotion-professional", "[professional]", TpsVisualCueContracts.EmotionAttributeName, "professional"),
        new(23, "24-emotion-focused", "[focused]", TpsVisualCueContracts.EmotionAttributeName, "focused"),
        new(24, "25-emotion-concerned", "[concerned]", TpsVisualCueContracts.EmotionAttributeName, "concerned"),
        new(25, "26-emotion-motivational", "[motivational]", TpsVisualCueContracts.EmotionAttributeName, "motivational"),
        new(26, "27-emotion-neutral", "[neutral]", TpsVisualCueContracts.EmotionAttributeName, "neutral"),
        new(27, "28-delivery-aside", "[aside]", TpsVisualCueContracts.DeliveryAttributeName, "aside"),
        new(28, "29-delivery-rhetorical", "[rhetorical]", TpsVisualCueContracts.DeliveryAttributeName, "rhetorical"),
        new(29, "30-delivery-building", "[building]", TpsVisualCueContracts.DeliveryAttributeName, TpsVisualCueContracts.DeliveryModeBuilding),
        new(30, "31-delivery-sarcasm", "[sarcasm]", TpsVisualCueContracts.DeliveryAttributeName, "sarcasm"),
        new(31, "32-articulation-legato", "[legato]", TpsVisualCueContracts.ArticulationAttributeName, TpsVisualCueContracts.ArticulationLegato),
        new(32, "33-articulation-staccato", "[staccato]", TpsVisualCueContracts.ArticulationAttributeName, TpsVisualCueContracts.ArticulationStaccato),
        new(33, "34-contour-energy", "[energy:8]", TpsVisualCueContracts.EnergyAttributeName, "8"),
        new(34, "35-contour-melody", "[melody:3]", TpsVisualCueContracts.MelodyAttributeName, "3"),
        new(35, "36-editorial-highlight", "[highlight]", TpsVisualCueContracts.HighlightAttributeName, TpsVisualCueContracts.HighlightAttributeValue),
        new(36, "37-editorial-emphasis", "[emphasis]", ExpectEmphasis: true),
        new(37, "38-markdown-bold", "Markdown bold", ExpectEmphasis: true),
        new(38, "39-markdown-italic", "Markdown italic", ExpectEmphasis: true),
        new(39, "40-guide-pronunciation", "[pronunciation:guide]", UiDataAttributes.Teleprompter.Pronunciation, "guide"),
        new(40, "41-guide-phonetic", "[phonetic:IPA]", UiDataAttributes.Teleprompter.Pronunciation, "IPA"),
        new(41, "42-guide-stress", "[stress:rising]", TpsVisualCueContracts.StressAttributeName, TpsVisualCueContracts.StressAttributeValue),
        new(42, "43-edit-point", "[edit_point]"),
        new(43, "44-edit-point-medium", "[edit_point:medium]"),
        new(44, "45-edit-point-high", "[edit_point:high]"),
        new(45, "46-metadata-speaker", "Speaker metadata"),
        new(46, "47-metadata-archetype", "Archetype metadata")
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
                await UiScenarioArtifacts.CaptureLocatorAsync(readerStage, CueMatrixScenario, capture.StepName);
            }
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
                    "signal",
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
                const words = [...host.querySelectorAll('.rd-w')];
                const target = words.find(candidate => normalize(candidate.textContent) === args.targetWord);
                return {
                    rawTagsVisible: /\[[^\]]+\]/.test(host.textContent ?? ''),
                    targetText: target?.textContent?.trim() ?? '',
                    attributeValue: args.attributeName ? target?.getAttribute(args.attributeName) ?? '' : '',
                    pauseCueCount: host.querySelectorAll('.rd-pause').length,
                    breathCueCount: host.querySelectorAll('[data-tps-breath="true"]').length,
                    emphasisGroupCount: host.querySelectorAll('[data-emphasis="true"]').length
                };
            }
            """,
            new
            {
                attributeName = capture.AttributeName,
                targetWord = "signal"
            });

        await Assert.That(probe.RawTagsVisible)
            .IsFalse()
            .Because($"Expected raw TPS tags to be hidden in cue example '{capture.CueLabel}'.");
        await Assert.That(probe.TargetText)
            .IsEqualTo("signal")
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
            }
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
                const nodes = Array.from(host.querySelectorAll(`[data-test^="${args.wordPrefix}"]`));
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
        bool ExpectedNoAttribute = false);

    private sealed class CueMatrixCardProbe
    {
        public bool RawTagsVisible { get; init; }

        public string TargetText { get; init; } = string.Empty;

        public string AttributeValue { get; init; } = string.Empty;

        public int PauseCueCount { get; init; }

        public int BreathCueCount { get; init; }

        public int EmphasisGroupCount { get; init; }
    }
}
