using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorScriptGraphViewTests(StandaloneAppFixture fixture)
{
    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_GraphTabRendersScriptKnowledgeGraph()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);

            await page.GetByTestId(UiTestIds.Editor.GraphTab).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Editor.GraphPanel))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphSummary))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphNode).First)
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await Expect(page.GetByTestId(UiTestIds.Editor.GraphCanvas))
                .ToHaveAttributeAsync(
                    BrowserTestConstants.Editor.GraphReadyAttributeName,
                    BrowserTestConstants.Editor.GraphReadyAttributeValue,
                    new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });

            await page.GetByTestId(UiTestIds.Editor.GraphNode).First.ClickAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            var selectedState = await EditorMonacoDriver.GetStateAsync(page);
            await Assert.That(selectedState.Selection.End).IsGreaterThan(selectedState.Selection.Start);

            await page.GetByTestId(UiTestIds.Header.AiSpotlight).ClickAsync();
            await page.GetByTestId(UiTestIds.AiSpotlight.PromptInput).FillAsync("rewrite selected graph range");
            await page.GetByTestId(UiTestIds.AiSpotlight.Submit).ClickAsync();
            await page.GetByTestId(UiTestIds.AiSpotlight.RunPlan).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.AiSpotlight.ApprovalState))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.DefaultVisibleTimeoutMs });
            await page.GetByTestId(UiTestIds.AiSpotlight.ApprovalApprove).ClickAsync();
            await page.WaitForFunctionAsync(
                """
                (args) => {
                    const state = window[args.harnessGlobalName]?.getState(args.testId);
                    return state?.text?.includes("[warm]");
                }
                """,
                new
                {
                    harnessGlobalName = PrompterOne.Shared.Services.Editor.EditorMonacoRuntimeContract.BrowserHarnessGlobalName,
                    testId = UiTestIds.Editor.SourceStage
                },
                new() { Timeout = BrowserTestConstants.Timing.EditorMutationTimeoutMs });
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }
}
