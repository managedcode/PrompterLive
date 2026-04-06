using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;
using System.Threading.Tasks;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorDocumentSplitFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture){
    private const string EpisodeOneCardId = "untitled-script-split-01-episode-1-how-to-think-about-systems";
    private const string EpisodeOneTitle = "Episode 1 - How to Think About Systems";
    private const string EpisodeTwoCardId = "untitled-script-split-02-episode-2-how-systems-talk-to-each-other";
    private const string EpisodeTwoTitle = "Episode 2 - How Systems Talk to Each Other";
    private const string SplitFeedbackBadge = "From ##";
    private const string SplitFeedbackActionLabel = "Open in Library";
    private const string SplitFeedbackDestination = "New scripts were saved to the library.";
    private const string SplitFeedbackDraftNote = "The current draft stayed open.";
    private const string SplitFeedbackSummary = "2 new scripts created.";
    private const string SplitFeedbackTitle = "Split complete";
    private const string SplitSource =
        """
        ## [Episode 1 - How to Think About Systems|140WPM|Professional]
        Before you write code, / you need to think about the system. //

        ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional]
        APIs, events, and retries matter. //
        """;

    [Test]
    public Task EditorScreen_SplitBySegmentHeadingCreatesLibraryScriptsWithoutReplacingCurrentDraft() =>
        RunPageAsync(async page =>
        {
            UiScenarioArtifacts.ResetScenario(BrowserTestConstants.EditorFlow.SplitFeedbackScenario);
            await page.GotoAsync(BrowserTestConstants.Routes.Editor);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page)).ToBeVisibleAsync();
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetTextAsync(page, SplitSource);

            await page.GetByTestId(UiTestIds.Editor.SplitSegment).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Editor.SplitStatus)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultTitle)).ToHaveTextAsync(SplitFeedbackTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultSummary)).ToHaveTextAsync(SplitFeedbackSummary);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultBadge)).ToHaveTextAsync(SplitFeedbackBadge);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultLibrary)).ToHaveTextAsync(SplitFeedbackDestination);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultCurrentDraft)).ToHaveTextAsync(SplitFeedbackDraftNote);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultItem(0))).ToContainTextAsync(EpisodeOneTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultItem(1))).ToContainTextAsync(EpisodeTwoTitle);
            await Expect(page.GetByTestId(UiTestIds.Editor.SplitResultOpenLibrary)).ToHaveTextAsync(SplitFeedbackActionLabel);
            await UiScenarioArtifacts.CaptureLocatorAsync(
                page.GetByTestId(UiTestIds.Editor.SplitStatus),
                BrowserTestConstants.EditorFlow.SplitFeedbackScenario,
                BrowserTestConstants.EditorFlow.SplitFeedbackStep);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(SplitSource);
            await Assert.That(new Uri(page.Url).AbsolutePath).IsEqualTo(AppRoutes.Editor);

            await page.GetByTestId(UiTestIds.Editor.SplitResultOpenLibrary).ClickAsync();
            await page.WaitForURLAsync(BrowserTestConstants.Routes.Pattern(AppRoutes.Library));
            await Expect(page.GetByTestId(UiTestIds.Library.Page)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Library.FolderAll).ClickAsync();

            await Expect(page.GetByTestId(UiTestIds.Library.Card(EpisodeOneCardId))).ToContainTextAsync(EpisodeOneTitle);
            await Expect(page.GetByTestId(UiTestIds.Library.Card(EpisodeTwoCardId))).ToContainTextAsync(EpisodeTwoTitle);
        });
}