using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.App.UITests;

public sealed class EditorCueRenderingFlowTests(StandaloneAppFixture fixture) : IClassFixture<StandaloneAppFixture>
{
    private const string CueScenario = "editor-tps-cue-rendering";
    private const string StepName = "01-editor-cue-overlay";

    [Fact]
    public async Task EditorScreen_RendersCueAwareOverlayContractsForDeliveryPreview()
    {
        UiScenarioArtifacts.ResetScenario(CueScenario);

        var page = await fixture.NewPageAsync();

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Editor.SourceInput).EvaluateAsync(
                """
                (element, value) => {
                    element.focus();
                    element.value = value;
                    element.dispatchEvent(new Event("input", { bubbles: true }));
                    element.setSelectionRange(0, 0);
                    element.dispatchEvent(new Event("select", { bubbles: true }));
                }
                """,
                """
                ## [Cue Demo|140WPM|neutral]
                ### [Delivery Block|140WPM|neutral]
                [loud][building]Rise together[/building][/loud] and [soft]listen[stress]ing[/stress][/soft].
                """);

            var highlight = page.GetByTestId(UiTestIds.Editor.SourceHighlight);
            var probe = await highlight.EvaluateAsync<EditorCueProbe>(
                $$"""
                host => {
                    const loud = host.querySelector('[{{TpsVisualCueContracts.VolumeAttributeName}}="{{TpsVisualCueContracts.VolumeLoud}}"]');
                    const soft = host.querySelector('[{{TpsVisualCueContracts.VolumeAttributeName}}="{{TpsVisualCueContracts.VolumeSoft}}"]');
                    const building = host.querySelector('[{{TpsVisualCueContracts.DeliveryAttributeName}}="{{TpsVisualCueContracts.DeliveryModeBuilding}}"]');
                    const stress = host.querySelector('[{{TpsVisualCueContracts.StressAttributeName}}="{{TpsVisualCueContracts.StressAttributeValue}}"]');

                    const readScale = element => {
                        if (!(element instanceof HTMLElement)) {
                            return '';
                        }

                        return getComputedStyle(element).getPropertyValue('{{TpsVisualCueContracts.CueScaleVariableName}}').trim();
                    };

                    return {
                        loudVolume: loud?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') ?? '',
                        softVolume: soft?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') ?? '',
                        buildingDelivery: building?.getAttribute('{{TpsVisualCueContracts.DeliveryAttributeName}}') ?? '',
                        stressValue: stress?.getAttribute('{{TpsVisualCueContracts.StressAttributeName}}') ?? '',
                        loudScale: readScale(loud),
                        softScale: readScale(soft)
                    };
                }
                """);

            Assert.Equal(TpsVisualCueContracts.VolumeLoud, probe.LoudVolume);
            Assert.Equal(TpsVisualCueContracts.VolumeSoft, probe.SoftVolume);
            Assert.Equal(TpsVisualCueContracts.DeliveryModeBuilding, probe.BuildingDelivery);
            Assert.Equal(TpsVisualCueContracts.StressAttributeValue, probe.StressValue);
            Assert.False(string.IsNullOrWhiteSpace(probe.LoudScale));
            Assert.False(string.IsNullOrWhiteSpace(probe.SoftScale));

            await UiScenarioArtifacts.CapturePageAsync(page, CueScenario, StepName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private sealed class EditorCueProbe
    {
        public string LoudVolume { get; init; } = string.Empty;

        public string SoftVolume { get; init; } = string.Empty;

        public string BuildingDelivery { get; init; } = string.Empty;

        public string StressValue { get; init; } = string.Empty;

        public string LoudScale { get; init; } = string.Empty;

        public string SoftScale { get; init; } = string.Empty;
    }
}
