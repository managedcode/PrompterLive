using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorDragDropFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private const string DocumentSeparator = "\n\n";
    private const string ReplaceFileName = "Dropped System Design.tps.md";
    private const string ReplaceTitle = "Dropped System Design";
    private const string ReplaceVisibleBody =
        """
        ## [Dropped Episode|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        Dropped documents should render immediately.
        """;
    private const string ReplaceFileText =
        """
        ---
        title: "Dropped System Design"
        profile: Actor
        duration: "45:00"
        ---

        ## [Dropped Episode|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        Dropped documents should render immediately.
        """;
    private const string AppendFileName = "appendix-notes.md";
    private const string AppendVisibleBody =
        """
        ## [Dropped Appendix|160WPM|Focused]
        ### [Closing|155WPM|Professional]
        The dropped appendix lands at the end of the current draft.
        """;
    private const string AppendFileText =
        """
        ---
        title: "Ignored While Appending"
        profile: Actor
        ---

        ## [Dropped Appendix|160WPM|Focused]
        ### [Closing|155WPM|Professional]
        The dropped appendix lands at the end of the current draft.
        """;
    private const string UnsupportedFileName = "unsupported-drop.docx";
    private const string UnsupportedFileText = "This drop should be rejected.";

    [Test]
    public Task EditorScreen_DropOnEmptyDraft_ReplacesTextAndSupportsUndoRedo() =>
        RunPageAsync(async page =>
        {
            await EditorIsolatedDraftDriver.OpenBlankDraftAsync(page);
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            await Expect(sourceInput).ToHaveValueAsync(string.Empty);

            await EditorMonacoDriver.DropFilesAsync(
                page,
                new EditorMonacoDriver.DroppedFileDescriptor(ReplaceFileName, ReplaceFileText));

            await EditorIsolatedDraftDriver.WaitForAssignedScriptRouteAsync(page);
            await Expect(page.GetByTestId(UiTestIds.Editor.Page))
                .ToBeVisibleAsync(new() { Timeout = BrowserTestConstants.Timing.ExtendedVisibleTimeoutMs });
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(sourceInput).ToHaveValueAsync(ReplaceVisibleBody);
            await Expect(page.GetByTestId(UiTestIds.Header.Title)).ToHaveTextAsync(ReplaceTitle);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.Undo),
                noWaitAfter: true);
            await Expect(sourceInput).ToHaveValueAsync(string.Empty);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.Redo),
                noWaitAfter: true);
            await Expect(sourceInput).ToHaveValueAsync(ReplaceVisibleBody);
        });

    [Test]
    public Task EditorScreen_DropOnExistingDraft_AppendsVisibleBodyAndSupportsUndoRedo() =>
        RunPageAsync(async page =>
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.QuantumId);
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            var initialText = await sourceInput.InputValueAsync();
            var expectedText = BuildExpectedAppendedText(initialText, AppendVisibleBody);

            await EditorMonacoDriver.DropFilesAsync(
                page,
                new EditorMonacoDriver.DroppedFileDescriptor(AppendFileName, AppendFileText));

            await Expect(sourceInput).ToHaveValueAsync(expectedText);
            var appendedText = await sourceInput.InputValueAsync();
            await Assert.That(appendedText).DoesNotContain("title:");
            await Assert.That(appendedText).DoesNotContain("---");

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.Undo),
                noWaitAfter: true);
            await Expect(sourceInput).ToHaveValueAsync(initialText);

            await UiInteractionDriver.ClickAndContinueAsync(
                page.GetByTestId(UiTestIds.Editor.Redo),
                noWaitAfter: true);
            await Expect(sourceInput).ToHaveValueAsync(expectedText);
        });

    [Test]
    public Task EditorScreen_DropRejectsUnsupportedExtensions_AndKeepsDraftUnchanged() =>
        RunPageAsync(async page =>
        {
            await EditorIsolatedDraftDriver.CreateSeededDraftAsync(page, BrowserTestConstants.Scripts.DemoId);
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            var initialText = await sourceInput.InputValueAsync();

            await EditorMonacoDriver.DropFilesAsync(
                page,
                new EditorMonacoDriver.DroppedFileDescriptor(UnsupportedFileName, UnsupportedFileText));

            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToContainTextAsync(SharedUiText.Text(UiTextKey.ImportScriptMessage));
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToContainTextAsync(SharedUiText.Text(UiTextKey.EditorDropUnsupportedDetail));
            await Expect(sourceInput).ToHaveValueAsync(initialText);
        });

    private static string BuildExpectedAppendedText(string initialText, string appendedText) =>
        string.Concat(
            initialText.TrimEnd(),
            DocumentSeparator,
            appendedText.Trim());
}
