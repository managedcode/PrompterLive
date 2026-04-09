using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Services.Editor;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorMonacoTagToneFlowTests(StandaloneAppFixture fixture)
{
    private const string ScenarioName = "editor-monaco-tag-tones";
    private const string StepName = "01-semantic-tag-tones";
    private const string LoudTagClass = "po-tag-volume-loud";
    private const string SoftTagClass = "po-tag-volume-soft";
    private const string HighlightTagClass = "po-tag-highlight";
    private const string PronunciationTagClass = "po-tag-pronunciation";

    [Test]
    public async Task EditorScreen_TintsSemanticTagsToMatchCueFamiliesWithoutFullStrength()
    {
        UiScenarioArtifacts.ResetScenario(ScenarioName);

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
                [loud]Urgent[/loud] [soft]listen[/soft] [highlight]tonight[/highlight]
                [pronunciation:TELE-promp-ter]teleprompter[/pronunciation]
                """);

            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const harness = window[args.harnessGlobalName];
                    const classes = harness?.getState(args.stageTestId)?.decorationClasses ?? [];
                    return classes.some(value => value.includes(args.loudTagClass)) &&
                        classes.some(value => value.includes(args.softTagClass)) &&
                        classes.some(value => value.includes(args.highlightTagClass)) &&
                        classes.some(value => value.includes(args.pronunciationTagClass));
                }
                """,
                new
                {
                    harnessGlobalName = EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                    highlightTagClass = HighlightTagClass,
                    loudTagClass = LoudTagClass,
                    pronunciationTagClass = PronunciationTagClass,
                    softTagClass = SoftTagClass,
                    stageTestId = UiTestIds.Editor.SourceStage
                },
                new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            var state = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(HasDecorationToken(state, LoudTagClass)).IsTrue();
            await Assert.That(HasDecorationToken(state, SoftTagClass)).IsTrue();
            await Assert.That(HasDecorationToken(state, HighlightTagClass)).IsTrue();
            await Assert.That(HasDecorationToken(state, PronunciationTagClass)).IsTrue();
            await Assert.That(state.Text).Contains("[loud]Urgent[/loud]");
            await Assert.That(state.Text).Contains("[soft]listen[/soft]");
            await Assert.That(state.Text).Contains("[highlight]tonight[/highlight]");
            await Assert.That(state.Text).Contains("[pronunciation:TELE-promp-ter]teleprompter[/pronunciation]");

            await UiScenarioArtifacts.CapturePageAsync(page, ScenarioName, StepName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static bool HasDecorationToken(EditorMonacoState state, string token) =>
        state.DecorationClasses.Any(value => value.Contains(token, StringComparison.Ordinal));
}
