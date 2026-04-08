using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorDragDropFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private const string DocumentSeparator = "\n\n";
    private const string DropImportMessage = "Unable to import this script.";
    private const string DropUnsupportedDetail = "Drop a .tps, .tps.md, .md.tps, .md, or .txt file onto the editor.";
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
            await page.GotoAsync(BrowserTestConstants.Routes.Editor);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(string.Empty);

            await EditorMonacoDriver.DropFilesAsync(
                page,
                new EditorMonacoDriver.DroppedFileDescriptor(ReplaceFileName, ReplaceFileText));

            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(ReplaceVisibleBody);
            await Expect(page.GetByTestId(UiTestIds.Header.Title)).ToHaveTextAsync(ReplaceTitle);

            await EditorMonacoDriver.FocusAsync(page);
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Undo);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(string.Empty);

            await EditorMonacoDriver.FocusAsync(page);
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Redo);
            await Expect(EditorMonacoDriver.SourceInput(page)).ToHaveValueAsync(ReplaceVisibleBody);
        });

    [Test]
    public Task EditorScreen_DropOnExistingDraft_AppendsVisibleBodyAndSupportsUndoRedo() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
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

            await EditorMonacoDriver.FocusAsync(page);
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Undo);
            await Expect(sourceInput).ToHaveValueAsync(initialText);

            await EditorMonacoDriver.FocusAsync(page);
            await page.Keyboard.PressAsync(BrowserTestConstants.Keyboard.Redo);
            await Expect(sourceInput).ToHaveValueAsync(expectedText);
        });

    [Test]
    public Task EditorScreen_DropRejectsUnsupportedExtensions_AndKeepsDraftUnchanged() =>
        RunPageAsync(async page =>
        {
            await page.GotoAsync(BrowserTestConstants.Routes.EditorDemo);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            var sourceInput = EditorMonacoDriver.SourceInput(page);
            var initialText = await sourceInput.InputValueAsync();

            await EditorMonacoDriver.DropFilesAsync(
                page,
                new EditorMonacoDriver.DroppedFileDescriptor(UnsupportedFileName, UnsupportedFileText));

            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToBeVisibleAsync();
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToContainTextAsync(DropImportMessage);
            await Expect(page.GetByTestId(UiTestIds.Diagnostics.Banner)).ToContainTextAsync(DropUnsupportedDetail);
            await Expect(sourceInput).ToHaveValueAsync(initialText);
        });

    private static string BuildExpectedAppendedText(string initialText, string appendedText) =>
        string.Concat(
            initialText.TrimEnd(),
            DocumentSeparator,
            appendedText.Trim());
}
