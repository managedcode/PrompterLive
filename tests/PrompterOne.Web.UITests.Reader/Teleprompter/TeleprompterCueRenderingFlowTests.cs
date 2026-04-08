using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class TeleprompterCueRenderingFlowTests(StandaloneAppFixture fixture)
{
    private const string CueScenario = "teleprompter-tps-cue-rendering";
    private const int InspirationCardIndex = 6;
    private const string StepName = "01-teleprompter-cue-rendering";

    [Test]
    public async Task TeleprompterDemo_RendersTypographyDrivenCueVariablesForVolumeAndBuilding()
    {
        UiScenarioArtifacts.ResetScenario(CueScenario);

        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.TeleprompterDemo);
            await Expect(page.GetByTestId(UiTestIds.Teleprompter.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            var cardText = page.GetByTestId(UiTestIds.Teleprompter.CardText(InspirationCardIndex));
            var probe = await cardText.EvaluateAsync<TeleprompterCueProbe>(
                $$"""
                host => {
                    const nodes = [...host.querySelectorAll('[{{BrowserTestConstants.Html.DataTestAttribute}}]')];
                    const soft = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') === '{{TpsVisualCueContracts.VolumeSoft}}');
                    const loud = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') === '{{TpsVisualCueContracts.VolumeLoud}}');
                    const buildingWords = nodes.filter(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.DeliveryAttributeName}}') === '{{TpsVisualCueContracts.DeliveryModeBuilding}}');

                    const readScale = element => {
                        if (!(element instanceof HTMLElement)) {
                            return '';
                        }

                        return getComputedStyle(element).getPropertyValue('{{TpsVisualCueContracts.CueScaleVariableName}}').trim();
                    };

                    return {
                        softVolume: soft?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') ?? '',
                        loudVolume: loud?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') ?? '',
                        firstBuildingDelivery: buildingWords[0]?.getAttribute('{{TpsVisualCueContracts.DeliveryAttributeName}}') ?? '',
                        lastBuildingDelivery: buildingWords.at(-1)?.getAttribute('{{TpsVisualCueContracts.DeliveryAttributeName}}') ?? '',
                        softScale: readScale(soft),
                        loudScale: readScale(loud),
                        firstBuildingScale: readScale(buildingWords[0]),
                        lastBuildingScale: readScale(buildingWords.at(-1))
                    };
                }
                """);

            await Assert.That(probe.SoftVolume).IsEqualTo(TpsVisualCueContracts.VolumeSoft);
            await Assert.That(probe.LoudVolume).IsEqualTo(TpsVisualCueContracts.VolumeLoud);
            await Assert.That(probe.FirstBuildingDelivery).IsEqualTo(TpsVisualCueContracts.DeliveryModeBuilding);
            await Assert.That(probe.LastBuildingDelivery).IsEqualTo(TpsVisualCueContracts.DeliveryModeBuilding);
            await Assert.That(string.IsNullOrWhiteSpace(probe.SoftScale)).IsFalse();
            await Assert.That(string.IsNullOrWhiteSpace(probe.LoudScale)).IsFalse();
            await Assert.That(string.IsNullOrWhiteSpace(probe.FirstBuildingScale)).IsFalse();
            await Assert.That(string.IsNullOrWhiteSpace(probe.LastBuildingScale)).IsFalse();

            await UiScenarioArtifacts.CapturePageAsync(page, CueScenario, StepName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private sealed class TeleprompterCueProbe
    {
        public string SoftVolume { get; init; } = string.Empty;

        public string LoudVolume { get; init; } = string.Empty;

        public string FirstBuildingDelivery { get; init; } = string.Empty;

        public string LastBuildingDelivery { get; init; } = string.Empty;

        public string SoftScale { get; init; } = string.Empty;

        public string LoudScale { get; init; } = string.Empty;

        public string FirstBuildingScale { get; init; } = string.Empty;

        public string LastBuildingScale { get; init; } = string.Empty;
    }
}
