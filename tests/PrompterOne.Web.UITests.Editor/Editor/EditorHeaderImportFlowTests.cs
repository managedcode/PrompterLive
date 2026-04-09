using System.IO.Compression;
using System.Text;
using PrompterOne.Shared.Contracts;
using static Microsoft.Playwright.Assertions;

namespace PrompterOne.Web.UITests;

[ClassDataSource<StandaloneAppFixture>(Shared = SharedType.PerClass)]
public sealed class EditorHeaderImportFlowTests(StandaloneAppFixture fixture) : AppUiTestBase(fixture)
{
    private const string ImportedDocxFileName = "Imported Design Review From File Name With A Long Header Title That Should Clamp Cleanly In Editor.docx";
    private const string ImportedTitle = "Imported Design Review From File Name With A Long Header Title That Should Clamp Cleanly In Editor";
    private const string ImportedHeading = "Converted heading should stay inside the editor body instead of becoming the shell title";
    private const string ImportedParagraph = "MarkItDown should convert this DOCX paragraph into Markdown for the editor.";
    private const string ImportedStepName = "01-editor-docx-import";
    private const string ScenarioName = "editor-header-import-flow";

    [Test]
    public Task EditorHeader_ImportDocx_UsesFileStemTitleAndClampFriendlyChrome() =>
        RunPageAsync(async page =>
        {
            var importPath = await CreateDocxAsync(ImportedDocxFileName, ImportedParagraph);

            try
            {
                await EditorIsolatedDraftDriver.OpenBlankDraftAsync(page);

                await page.GetByTestId(UiTestIds.Header.EditorImportScriptInput)
                    .SetInputFilesAsync(importPath);

                var headerTitle = page.GetByTestId(UiTestIds.Header.Title);
                await Expect(headerTitle).ToHaveTextAsync(ImportedTitle);
                await Expect(headerTitle).ToHaveAttributeAsync("title", ImportedTitle);

                var sourceText = await EditorMonacoDriver.SourceInput(page).InputValueAsync();
                await Assert.That(sourceText).Contains(ImportedHeading);
                await Assert.That(sourceText).Contains(ImportedParagraph);

                var headerTextOverflow = await headerTitle.EvaluateAsync<string>(
                    "element => getComputedStyle(element).textOverflow");
                var headerOverflow = await headerTitle.EvaluateAsync<string>(
                    "element => getComputedStyle(element).overflow");
                await Assert.That(headerTextOverflow).IsEqualTo("ellipsis");
                await Assert.That(headerOverflow).IsEqualTo("hidden");

                var firstSegment = page.GetByTestId(UiTestIds.Editor.SegmentNavigation(0));
                await Expect(firstSegment).ToBeVisibleAsync();
                var segmentName = page.Locator(
                    $"""[{BrowserTestConstants.Html.DataTestAttribute}="{UiTestIds.Editor.SegmentNavigation(0)}"] .ed-tree-seg__name""");
                await Expect(segmentName).ToBeVisibleAsync();
                var segmentTextOverflow = await segmentName.EvaluateAsync<string>(
                    "element => getComputedStyle(element).textOverflow");
                await Assert.That(segmentTextOverflow).IsEqualTo("ellipsis");
                await UiScenarioArtifacts.CapturePageAsync(page, ScenarioName, ImportedStepName);
            }
            finally
            {
                DeleteImportedDocument(importPath);
            }
        });

    private static async Task<string> CreateDocxAsync(string fileName, string paragraph)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, fileName);
        await using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        WriteEntry(
            archive,
            "[Content_Types].xml",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
              <Default Extension="xml" ContentType="application/xml" />
              <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml" />
            </Types>
            """);
        WriteEntry(
            archive,
            "_rels/.rels",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1"
                            Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"
                            Target="word/document.xml" />
            </Relationships>
            """);
        WriteEntry(
            archive,
            "word/document.xml",
            $"""
             <?xml version="1.0" encoding="UTF-8"?>
             <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
               <w:body>
                 <w:p>
                   <w:r>
                     <w:t>{EscapeXml(ImportedHeading)}</w:t>
                   </w:r>
                 </w:p>
                 <w:p>
                   <w:r>
                     <w:t>{EscapeXml(paragraph)}</w:t>
                   </w:r>
                 </w:p>
               </w:body>
             </w:document>
             """);

        await stream.FlushAsync();
        return path;
    }

    private static void DeleteImportedDocument(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string EscapeXml(string value) =>
        value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);

    private static void WriteEntry(ZipArchive archive, string entryName, string contents)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(contents);
    }
}
