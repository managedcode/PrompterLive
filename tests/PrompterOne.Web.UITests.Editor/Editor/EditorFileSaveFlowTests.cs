using System.Text.Json;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorFileSaveFlowTests(StandaloneAppFixture fixture)
{
    private const string DownloadMode = "download";
    private const string EditedScript =
        """
        ## [Launch Angle|205WPM|focused|1:00-2:00]
        ### [Signal Block|205WPM|professional]
        This saved draft proves the file export path. / [highlight]Keep the styling[/highlight] //
        """;
    private const string ExpectedDocumentName = "test-quantum-computing.tps";
    private const string ExpectedMarkdownDocumentName = "test-quantum-computing.md";
    private const string ExpectedTextDocumentName = "test-quantum-computing.txt";
    private const string FilePickerMode = "file-system";
    private const string HarnessDisableSavePickerScript =
        "() => window.__prompterOneEditorFileSaveHarness.disableSavePicker()";
    private const string HarnessGetSavedFileStateScript =
        "() => window.__prompterOneEditorFileSaveHarness.getSavedFileState()";
    private const string HarnessResetScript =
        "() => window.__prompterOneEditorFileSaveHarness.reset()";
    private const int SavedFilePollAttempts = 20;
    private const int SavedFilePollDelayMs = 50;
    private const string SavedFileTitleLine = "title: \"Quantum Computing\"";

    private readonly StandaloneAppFixture _fixture = fixture;

    [Test]
    public async Task EditorScreen_SaveFile_UsesFilePickerAndWritesCanonicalTpsDocument()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.AddInitScriptAsync(scriptPath: GetEditorFileSaveHarnessScriptPath());
            await EditorRouteDriver.OpenReadyAsync(page, BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetTextAsync(page, EditedScript);

            await Expect(page.GetByTestId(UiTestIds.Header.EditorSaveFile)).ToBeVisibleAsync();
            await page.GetByTestId(UiTestIds.Header.EditorSaveFile).ClickAsync();
            var savedFile = await WaitForSavedFileAsync(page, FilePickerMode);
            var savedText = savedFile.GetProperty("text").GetString() ?? string.Empty;

            await Assert.That(savedFile.GetProperty("mode").GetString()).IsEqualTo(FilePickerMode);
            await Assert.That(savedFile.GetProperty("fileName").GetString()).IsEqualTo(ExpectedDocumentName);
            await Assert.That(savedFile.GetProperty("pickerCallCount").GetInt32()).IsEqualTo(1);
            await Assert.That(savedFile.GetProperty("hasBlob").GetBoolean()).IsTrue();
            await Assert.That(savedText).StartsWith("---");
            await Assert.That(savedText).Contains(SavedFileTitleLine);
            await Assert.That(savedText).Contains(EditedScript);
            await Expect(page.GetByTestId(UiTestIds.Editor.SourceInput)).ToHaveValueAsync(EditedScript);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_SaveFile_FallsBackToDownloadWhenSavePickerIsUnavailable()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.AddInitScriptAsync(scriptPath: GetEditorFileSaveHarnessScriptPath());
            await EditorRouteDriver.OpenReadyAsync(page, BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await page.EvaluateAsync(HarnessResetScript);
            await page.EvaluateAsync(HarnessDisableSavePickerScript);
            await EditorMonacoDriver.SetTextAsync(page, EditedScript);

            await page.GetByTestId(UiTestIds.Header.EditorSaveFile).ClickAsync();
            var savedFile = await WaitForSavedFileAsync(page, DownloadMode);
            var savedText = savedFile.GetProperty("text").GetString() ?? string.Empty;

            await Assert.That(savedFile.GetProperty("mode").GetString()).IsEqualTo(DownloadMode);
            await Assert.That(savedFile.GetProperty("pickerCallCount").GetInt32()).IsEqualTo(0);
            await Assert.That(savedFile.GetProperty("downloadCallCount").GetInt32()).IsEqualTo(1);
            await Assert.That(savedFile.GetProperty("fileName").GetString()).IsEqualTo(ExpectedDocumentName);
            await Assert.That(savedText).Contains(EditedScript);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    [Test]
    public async Task EditorScreen_ExportButtons_WriteMarkdownAndPlainTextFiles()
    {
        var page = await _fixture.NewPageAsync(additionalContext: true);

        try
        {
            await page.AddInitScriptAsync(scriptPath: GetEditorFileSaveHarnessScriptPath());
            await EditorRouteDriver.OpenReadyAsync(page, BrowserTestConstants.Routes.EditorQuantum);
            await EditorMonacoDriver.WaitUntilReadyAsync(page);
            await EditorMonacoDriver.SetTextAsync(page, EditedScript);

            await Expect(page.GetByTestId(UiTestIds.Header.EditorSaveFile)).ToContainTextAsync("Export");
            await Expect(page.GetByTestId(UiTestIds.Header.EditorExportMarkdown)).ToContainTextAsync("Export .md");
            await Expect(page.GetByTestId(UiTestIds.Header.EditorExportPlainText)).ToContainTextAsync("Export .txt");

            await page.GetByTestId(UiTestIds.Header.EditorExportMarkdown).ClickAsync();
            var markdownFile = await WaitForSavedFileAsync(page, FilePickerMode);
            await AssertExportedSourceFileAsync(markdownFile, ExpectedMarkdownDocumentName);

            await page.EvaluateAsync(HarnessResetScript);

            await page.GetByTestId(UiTestIds.Header.EditorExportPlainText).ClickAsync();
            var textFile = await WaitForSavedFileAsync(page, FilePickerMode);
            await AssertExportedSourceFileAsync(textFile, ExpectedTextDocumentName);
        }
        finally
        {
            await page.Context.CloseAsync();
        }
    }

    private static string GetEditorFileSaveHarnessScriptPath() =>
        UiTestAssetPaths.GetEditorFileSaveHarnessScriptPath();

    private static async Task<JsonElement> WaitForSavedFileAsync(Microsoft.Playwright.IPage page, string mode)
    {
        JsonElement savedFile = default;

        for (var attempt = 0; attempt < SavedFilePollAttempts; attempt++)
        {
            savedFile = await page.EvaluateAsync<JsonElement>(HarnessGetSavedFileStateScript);
            if (savedFile.GetProperty("hasBlob").GetBoolean()
                && string.Equals(mode, savedFile.GetProperty("mode").GetString(), StringComparison.Ordinal))
            {
                return savedFile;
            }

            await page.WaitForTimeoutAsync(SavedFilePollDelayMs);
        }

        return savedFile;
    }

    private static async Task AssertExportedSourceFileAsync(JsonElement savedFile, string expectedFileName)
    {
        await Assert.That(savedFile.GetProperty("mode").GetString()).IsEqualTo(FilePickerMode);
        await Assert.That(savedFile.GetProperty("fileName").GetString()).IsEqualTo(expectedFileName);
        await Assert.That(savedFile.GetProperty("pickerCallCount").GetInt32()).IsEqualTo(1);
        await Assert.That(savedFile.GetProperty("hasBlob").GetBoolean()).IsTrue();
        await Assert.That(savedFile.GetProperty("text").GetString()).IsEqualTo(EditedScript);
    }
}
