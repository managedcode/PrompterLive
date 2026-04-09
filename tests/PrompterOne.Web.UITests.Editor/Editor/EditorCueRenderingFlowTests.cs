using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services.Editor;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorCueRenderingFlowTests(StandaloneAppFixture fixture)
{
    private const string CueScenario = "editor-tps-cue-rendering";
    private const string OverlayStepName = "01-editor-cue-overlay";
    private const string MonacoStylingStepName = "02-editor-monaco-styling";

    [Test]
    public async Task EditorScreen_RendersCueAwareOverlayContractsForDeliveryPreview()
    {
        UiScenarioArtifacts.ResetScenario(CueScenario);

        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetTextAsync(
                page,
                """
                ## [Cue Demo|140WPM|neutral]
                ### [Delivery Block|140WPM|neutral]
                [loud][building]Rise together[/building][/loud] and [soft]listen[stress]ing[/stress][/soft].
                """);

            var highlight = page.GetByTestId(UiTestIds.Editor.SourceHighlight);
            var probe = await highlight.EvaluateAsync<EditorCueProbe>(
                $$"""
                host => {
                    const nodes = [...host.querySelectorAll('*')];
                    const loud = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') === '{{TpsVisualCueContracts.VolumeLoud}}');
                    const soft = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.VolumeAttributeName}}') === '{{TpsVisualCueContracts.VolumeSoft}}');
                    const building = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.DeliveryAttributeName}}') === '{{TpsVisualCueContracts.DeliveryModeBuilding}}');
                    const stress = nodes.find(node =>
                        node?.getAttribute('{{TpsVisualCueContracts.StressAttributeName}}') === '{{TpsVisualCueContracts.StressAttributeValue}}');

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

            await Assert.That(probe.LoudVolume).IsEqualTo(TpsVisualCueContracts.VolumeLoud);
            await Assert.That(probe.SoftVolume).IsEqualTo(TpsVisualCueContracts.VolumeSoft);
            await Assert.That(probe.BuildingDelivery).IsEqualTo(TpsVisualCueContracts.DeliveryModeBuilding);
            await Assert.That(probe.StressValue).IsEqualTo(TpsVisualCueContracts.StressAttributeValue);
            await Assert.That(string.IsNullOrWhiteSpace(probe.LoudScale)).IsFalse();
            await Assert.That(string.IsNullOrWhiteSpace(probe.SoftScale)).IsFalse();

            await UiScenarioArtifacts.CapturePageAsync(page, CueScenario, OverlayStepName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_RendersMonacoCueStylesImmediatelyAfterImport()
    {
        UiScenarioArtifacts.ResetScenario(CueScenario);

        var page = await fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetTextAsync(
                page,
                """
                ## [Cue Import|140WPM|Professional]
                ### [Delivery Block|140WPM|Warm]
                [loud][building]Rise together[/building][/loud] and [soft][emphasis]listen closely[/emphasis][/soft]. //
                [pronunciation:TELE-promp-ter]teleprompter[/pronunciation] [highlight]tonight[/highlight]
                """);

            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const harness = window[args.harnessGlobalName];
                    const state = harness?.getState(args.stageTestId);
                    const classes = state?.decorationClasses ?? [];
                    return classes.some(value => value.includes(args.emphasisClass)) &&
                        classes.some(value => value.includes(args.highlightClass)) &&
                        classes.some(value => value.includes(args.loudClass)) &&
                        classes.some(value => value.includes(args.pauseClass)) &&
                        classes.some(value => value.includes(args.pronunciationClass)) &&
                        classes.some(value => value.includes(args.headerEmotionClass));
                }
                """,
                new
                {
                    emphasisClass = "po-inline-emphasis",
                    harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                    headerEmotionClass = "po-header-emotion",
                    highlightClass = "po-inline-highlight",
                    loudClass = "po-inline-loud",
                    pauseClass = "po-pause-long",
                    pronunciationClass = "po-inline-pronunciation-word",
                    stageTestId = UiTestIds.Editor.SourceStage
                },
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var state = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(state.Text).Contains("## [Cue Import|140WPM|Professional]");
            await Assert.That(HasDecorationToken(state, "po-inline-emphasis")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-highlight")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-loud")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-pause-long")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-pronunciation-word")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-tag")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-header-emotion")).IsTrue();

            await UiScenarioArtifacts.CapturePageAsync(page, CueScenario, MonacoStylingStepName);
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

    private static bool HasDecorationToken(EditorMonacoState state, string decorationToken) =>
        state.DecorationClasses.Any(value => value.Contains(decorationToken, StringComparison.Ordinal));
}
