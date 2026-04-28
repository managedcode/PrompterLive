using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services.Editor;

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
            await EditorIsolatedDraftDriver.OpenBlankDraftAsync(page);
            await EditorMonacoDriver.SetTextAsync(
                page,
                """
                ## [Cue Demo|140WPM|neutral]
                ### [Delivery Block|140WPM|neutral]
                [loud][building]Rise together[/building][/loud] and [soft]listen[stress]ing[/stress][/soft].
                [breath] [legato][energy:8]steady[/energy][/legato] [staccato][melody:4]rhythm[/melody][/staccato].
                """);
            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const harness = window[args.harnessGlobalName];
                    const state = harness?.getState(args.stageTestId);
                    const classes = state?.decorationClasses ?? [];
                    if (!classes.length) {
                        return false;
                    }

                    return args.requiredClasses.every(requiredClass =>
                        classes.some(value => value.includes(requiredClass)));
                }
                """,
                new
                {
                    harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                    requiredClasses = new[]
                    {
                        "po-inline-loud",
                        "po-inline-soft",
                        "po-inline-delivery-building",
                        "po-inline-stress",
                        "po-inline-articulation-legato",
                        "po-inline-articulation-staccato",
                        "po-inline-energy",
                        "po-inline-melody",
                        "po-tag-breath"
                    },
                    stageTestId = UiTestIds.Editor.SourceStage
                },
                new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            var state = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(HasDecorationToken(state, "po-inline-loud")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-soft")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-delivery-building")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-stress")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-articulation-legato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-articulation-staccato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-energy")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-melody")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-tag-breath")).IsTrue();

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
            await EditorIsolatedDraftDriver.OpenBlankDraftAsync(page);
            await EditorMonacoDriver.SetTextAsync(
                page,
                """
                ## [Cue Import|140WPM|Professional]
                ### [Delivery Block|140WPM|Warm]
                [loud][building]Rise together[/building][/loud] and [soft][emphasis]listen closely[/emphasis][/soft]. //
                [pronunciation:TELE-promp-ter]teleprompter[/pronunciation] [highlight]tonight[/highlight]
                [legato][energy:8]steady[/energy][/legato] [staccato][melody:4]rhythm[/melody][/staccato]
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
                        classes.some(value => value.includes(args.legatoClass)) &&
                        classes.some(value => value.includes(args.energyClass)) &&
                        classes.some(value => value.includes(args.staccatoClass)) &&
                        classes.some(value => value.includes(args.melodyClass)) &&
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
                    energyClass = "po-inline-energy",
                    legatoClass = "po-inline-articulation-legato",
                    loudClass = "po-inline-loud",
                    melodyClass = "po-inline-melody",
                    pauseClass = "po-pause-long",
                    pronunciationClass = "po-inline-pronunciation-word",
                    staccatoClass = "po-inline-articulation-staccato",
                    stageTestId = UiTestIds.Editor.SourceStage
                },
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var state = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(state.Text).Contains("## [Cue Import|140WPM|Professional]");
            await Assert.That(HasDecorationToken(state, "po-inline-emphasis")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-highlight")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-loud")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-articulation-legato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-energy")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-articulation-staccato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-melody")).IsTrue();
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

    private static bool HasDecorationToken(EditorMonacoState state, string decorationToken) =>
        state.DecorationClasses.Any(value => value.Contains(decorationToken, StringComparison.Ordinal));
}
