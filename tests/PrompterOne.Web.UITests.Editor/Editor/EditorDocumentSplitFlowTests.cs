using PrompterOne.Shared.Contracts;
using PrompterOne.Testing.Editor;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorDocumentSplitFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    [Test]
    public Task EditorScreen_SplitBySegmentHeadingCreatesLibraryScriptsWithoutReplacingCurrentDraft() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.SplitFeedbackScenario);
            await EditorIsolatedDraftDriver.CreateDraftAsync(page, EditorSplitFeedbackTestData.SplitSource);
            await page.GetByTestId(UiTestIds.Editor.ToolsTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.ToolsPanel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitSegment)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitSegmentActionLabel);

            var splitActionLivesInToolsPanel = await page.EvaluateAsync<bool>(
                """
                args => {
                    const toolsPanel = document.querySelector(`[data-test="${args.toolsPanelTestId}"]`);
                    const splitAction = document.querySelector(`[data-test="${args.splitActionTestId}"]`);
                    return Boolean(toolsPanel && splitAction && toolsPanel.contains(splitAction));
                }
                """,
                new
                {
                    toolsPanelTestId = UiTestIds.Editor.ToolsPanel,
                    splitActionTestId = UiTestIds.Editor.SplitSegment
                });
            await Assert.That(splitActionLivesInToolsPanel).IsTrue();

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.SplitSegment),
                noWaitAfter: true);

            await Expect(page.GetByTestId(UiTestIds.Editor.SplitStatus)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.EditorFlow.SplitFeedbackVisibleTimeoutMs
            });
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultTitle)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitFeedbackTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultSummary)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitFeedbackSummary);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultBadge)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitFeedbackBadge);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultLibrary)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitFeedbackSiblingDestination);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultCurrentDraft)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitFeedbackDraftNote);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultItem(0))).ToContainTextAsync(EditorSplitFeedbackTestData.EpisodeOneTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultItem(1))).ToContainTextAsync(EditorSplitFeedbackTestData.EpisodeTwoTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultOpenLibrary)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitActionLabel);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.SplitFeedbackScenario,
                BrowserTestConstants.EditorFlow.SplitFeedbackStep);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(EditorSplitFeedbackTestData.SplitSource);
            await Assert.That(new Uri(page.Url).AbsolutePath).IsEqualTo(AppRoutes.Editor);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.SplitResultOpenLibrary),
                noWaitAfter: true);
            await ShellRouteDriver.WaitForLibraryReadyAsync(page);
            await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();
            var libraryPage = page.GetByTestId(UiTestIds.Library.Page);

            await Expect(libraryPage).ToContainTextAsync(EditorSplitFeedbackTestData.EpisodeOneTitle);
            await Expect(libraryPage).ToContainTextAsync(EditorSplitFeedbackTestData.EpisodeTwoTitle);
        });

    [Test]
    public Task EditorScreen_SplitBySpeakerCreatesLibraryScriptsWithoutReplacingCurrentDraft() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.SplitFeedbackScenario);
            await EditorIsolatedDraftDriver.CreateDraftAsync(page, EditorSplitFeedbackTestData.SplitBySpeakerSource);
            await page.GetByTestId(UiTestIds.Editor.ToolsTab).ClickAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.ToolsPanel)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitSpeaker)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitSpeakerActionLabel);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.SplitSpeaker),
                noWaitAfter: true);

            await Expect(page.GetByTestId(UiTestIds.Editor.SplitStatus)).ToBeVisibleAsync(new()
            {
                Timeout = BrowserTestConstants.EditorFlow.SplitFeedbackVisibleTimeoutMs
            });
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultTitle)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitFeedbackTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultSummary)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitFeedbackSummary);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultBadge)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitSpeakerBadge);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultLibrary)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitFeedbackSiblingDestination);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultCurrentDraft)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitFeedbackDraftNote);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultItem(0))).ToContainTextAsync(EditorSplitFeedbackTestData.SpeakerCreatedTitles[0]);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultItem(1))).ToContainTextAsync(EditorSplitFeedbackTestData.SpeakerCreatedTitles[1]);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultOpenLibrary)).ToHaveTextAsync(EditorSplitFeedbackTestData.SplitActionLabel);
            await UiScenarioArtifacts.CapturePageAsync(
                page,
                BrowserTestConstants.EditorFlow.SplitFeedbackScenario,
                BrowserTestConstants.EditorFlow.SplitSpeakerStep);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(EditorSplitFeedbackTestData.SplitBySpeakerSource);
            await Assert.That(new Uri(page.Url).AbsolutePath).IsEqualTo(AppRoutes.Editor);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.SplitResultOpenLibrary),
                noWaitAfter: true);
            await ShellRouteDriver.WaitForLibraryReadyAsync(page);
            await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();
            var libraryPage = page.GetByTestId(UiTestIds.Library.Page);

            await Expect(libraryPage).ToContainTextAsync(EditorSplitFeedbackTestData.SpeakerCreatedTitles[0]);
            await Expect(libraryPage).ToContainTextAsync(EditorSplitFeedbackTestData.JordanTitle);
        });
}
