using System.Globalization;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterCueRenderingFlowTests(StandaloneAppFixture fixture)
{
    private const string CueScenario = "teleprompter-tps-cue-rendering";
    private const int InspirationCardIndex = 6;
    private const string StepName = "01-teleprompter-cue-rendering";
    private const string CueTextStepName = "02-teleprompter-cue-text";

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
}
