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
                        "tps-loud",
                        "po-inline-soft",
                        "tps-soft",
                        "po-inline-delivery-building",
                        "tps-building",
                        "po-inline-stress",
                        "tps-stress",
                        "po-inline-articulation-legato",
                        "tps-legato",
                        "po-inline-articulation-staccato",
                        "tps-staccato",
                        "po-inline-energy",
                        "tps-energy",
                        "po-inline-melody",
                        "tps-melody",
                        "po-tag-breath"
                    },
                    stageTestId = UiTestIds.Editor.SourceStage
                },
                new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });

            var state = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(HasDecorationToken(state, "po-inline-loud")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-loud")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-soft")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-soft")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-delivery-building")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-building")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-stress")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-stress")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-articulation-legato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-legato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-articulation-staccato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-staccato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-energy")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-energy")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-melody")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-melody")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-tag-breath")).IsTrue();
            await Assert.That(HasDecorationHover(state, "Energy contour")).IsTrue();
            await Assert.That(HasDecorationHover(state, "Melody contour")).IsTrue();
            await Assert.That(HasDecorationHover(state, "Volume: deliver this as loud")).IsTrue();
            await Assert.That(HasDecorationHover(state, "Articulation: apply legato")).IsTrue();

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
            const string emphasizedLine = "[loud][building]Rise together[/building][/loud] and [soft][emphasis]listen closely[/emphasis][/soft]. //";
            const string highlightedLine = "[pronunciation:TELE-promp-ter]teleprompter[/pronunciation] [highlight]tonight[/highlight]";
            const string contourLine = "[legato][energy:8]steady[/energy][/legato] [staccato][melody:4]rhythm[/melody][/staccato]";
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
                        classes.some(value => value.includes(args.emphasisSharedClass)) &&
                        classes.some(value => value.includes(args.highlightClass)) &&
                        classes.some(value => value.includes(args.highlightSharedClass)) &&
                        classes.some(value => value.includes(args.loudClass)) &&
                        classes.some(value => value.includes(args.loudSharedClass)) &&
                        classes.some(value => value.includes(args.legatoClass)) &&
                        classes.some(value => value.includes(args.legatoSharedClass)) &&
                        classes.some(value => value.includes(args.energyClass)) &&
                        classes.some(value => value.includes(args.energySharedClass)) &&
                        classes.some(value => value.includes(args.staccatoClass)) &&
                        classes.some(value => value.includes(args.staccatoSharedClass)) &&
                        classes.some(value => value.includes(args.melodyClass)) &&
                        classes.some(value => value.includes(args.melodySharedClass)) &&
                        classes.some(value => value.includes(args.pauseClass)) &&
                        classes.some(value => value.includes(args.pronunciationClass)) &&
                        classes.some(value => value.includes(args.pronunciationSharedClass)) &&
                        classes.some(value => value.includes(args.headerEmotionClass));
                }
                """,
                new
                {
                    emphasisClass = "po-inline-emphasis",
                    emphasisSharedClass = "tps-emphasis",
                    harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                    headerEmotionClass = "po-header-emotion",
                    highlightClass = "po-inline-highlight",
                    highlightSharedClass = "tps-highlight",
                    energyClass = "po-inline-energy",
                    energySharedClass = "tps-energy",
                    legatoClass = "po-inline-articulation-legato",
                    legatoSharedClass = "tps-legato",
                    loudClass = "po-inline-loud",
                    loudSharedClass = "tps-loud",
                    melodyClass = "po-inline-melody",
                    melodySharedClass = "tps-melody",
                    pauseClass = "po-pause-long",
                    pronunciationClass = "po-inline-pronunciation-word",
                    pronunciationSharedClass = "tps-pronunciation",
                    staccatoClass = "po-inline-articulation-staccato",
                    staccatoSharedClass = "tps-staccato",
                    stageTestId = UiTestIds.Editor.SourceStage
                },
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var state = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(state.Text).Contains("## [Cue Import|140WPM|Professional]");
            await Assert.That(HasDecorationToken(state, "po-inline-emphasis")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-emphasis")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-highlight")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-highlight")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-loud")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-loud")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-articulation-legato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-legato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-energy")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-energy")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-articulation-staccato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-staccato")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-melody")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-melody")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-pause-long")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-inline-pronunciation-word")).IsTrue();
            await Assert.That(HasDecorationToken(state, "tps-pronunciation")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-tag")).IsTrue();
            await Assert.That(HasDecorationToken(state, "po-header-emotion")).IsTrue();
            await Assert.That(HasDecorationHover(state, "Pronunciation guide")).IsTrue();
            await Assert.That(HasDecorationHover(state, "Highlight: mark this word or phrase")).IsTrue();

            var emphasisHover = await EditorMonacoDriver.GetHoverAsync(page, 3, FindColumn(emphasizedLine, "listen closely"));
            var highlightHover = await EditorMonacoDriver.GetHoverAsync(page, 4, FindColumn(highlightedLine, "tonight"));
            var energyHover = await EditorMonacoDriver.GetHoverAsync(page, 5, FindColumn(contourLine, "steady"));
            var melodyHover = await EditorMonacoDriver.GetHoverAsync(page, 5, FindColumn(contourLine, "rhythm"));

            await Assert.That(emphasisHover).IsNotNull();
            await Assert.That(highlightHover).IsNotNull();
            await Assert.That(energyHover).IsNotNull();
            await Assert.That(melodyHover).IsNotNull();
            await Assert.That(emphasisHover!.Contents).Contains(content => content.Contains("Emphasis wrapper", StringComparison.Ordinal));
            await Assert.That(highlightHover!.Contents).Contains(content => content.Contains("Highlight wrapper", StringComparison.Ordinal));
            await Assert.That(energyHover!.Contents).Contains(content => content.Contains("Energy contour", StringComparison.Ordinal));
            await Assert.That(melodyHover!.Contents).Contains(content => content.Contains("Melody contour", StringComparison.Ordinal));

            var energyPoint = await EditorMonacoDriver.GetRenderedDecorationCoordinatesAsync(page, "po-inline-energy", "steady");
            await page.Mouse.MoveAsync((float)energyPoint.X, (float)energyPoint.Y);
            await Expect(page.Locator(".monaco-hover:not(.hidden)").First).ToContainTextAsync("Energy contour", new()
            {
                Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs
            });

            await UiScenarioArtifacts.CapturePageAsync(page, CueScenario, MonacoStylingStepName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static bool HasDecorationToken(EditorMonacoState state, string decorationToken) =>
        state.DecorationClasses.Any(value => value.Contains(decorationToken, StringComparison.Ordinal));

    private static bool HasDecorationHover(EditorMonacoState state, string hoverText) =>
        state.DecorationHoverMessages.Any(value => value.Contains(hoverText, StringComparison.Ordinal));

    private static int FindColumn(string line, string fragment)
    {
        var index = line.IndexOf(fragment, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new InvalidOperationException($"Unable to locate \"{fragment}\" inside the Monaco cue-rendering probe line.");
        }

        return index + 2;
    }
}
