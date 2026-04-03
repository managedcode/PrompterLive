using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class TeleprompterCueRenderingFlowTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string CueScenario = "teleprompter-tps-cue-rendering";
    private const int InspirationCardIndex = 6;
    private const string StepName = "01-teleprompter-cue-rendering";

    [Fact]
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
                    const soft = host.querySelector('[{{TpsVisualCueContracts.VolumeAttributeName}}="{{TpsVisualCueContracts.VolumeSoft}}"]');
                    const loud = host.querySelector('[{{TpsVisualCueContracts.VolumeAttributeName}}="{{TpsVisualCueContracts.VolumeLoud}}"]');
                    const buildingWords = [...host.querySelectorAll('[{{TpsVisualCueContracts.DeliveryAttributeName}}="{{TpsVisualCueContracts.DeliveryModeBuilding}}"]')];

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

            Assert.Equal(TpsVisualCueContracts.VolumeSoft, probe.SoftVolume);
            Assert.Equal(TpsVisualCueContracts.VolumeLoud, probe.LoudVolume);
            Assert.Equal(TpsVisualCueContracts.DeliveryModeBuilding, probe.FirstBuildingDelivery);
            Assert.Equal(TpsVisualCueContracts.DeliveryModeBuilding, probe.LastBuildingDelivery);
            Assert.False(string.IsNullOrWhiteSpace(probe.SoftScale));
            Assert.False(string.IsNullOrWhiteSpace(probe.LoudScale));
            Assert.False(string.IsNullOrWhiteSpace(probe.FirstBuildingScale));
            Assert.False(string.IsNullOrWhiteSpace(probe.LastBuildingScale));

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
