using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PrompterOne.Core.Services.Editor;

namespace PrompterOne.Core.Tests;

public sealed class ScriptDocumentImportServiceTests
{
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string ImportedDocxFileName = "Imported Design Review From File Name.docx";
    private const string ImportedDocxTitle = "Imported Design Review From File Name";
    private const string ImportedHeading = "Converted heading should stay in the editor body";
    private const string ImportedParagraph = "MarkItDown should convert this DOCX paragraph into Markdown for the editor.";
    private const string FrontMatterMarkdownFileName = "Quarterly Town Hall.md";
    private const string FrontMatterMarkdownTitle = "Front matter title should win";
    private const string FrontMatterMarkdownText =
        """
        ---
        title: "Front matter title should win"
        ---

        ## [Opening|140WPM|Professional]
        ### [First block|140WPM|Warm]
        Imported markdown should preserve the explicit front matter title in the editor.
        """;
    private const string NativeScriptFileName = "camera-check.tps";
    private const string NativeScriptText =
        """
        ## [Camera Check|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        Native TPS imports should stay untouched.
        """;
    private const string PlainTextFileName = "Quarterly Town Hall Notes.txt";
    private const string PlainTextImportDocumentName = "Quarterly Town Hall Notes.tps.md";
    private const string PlainTextImportTitle = "Quarterly Town Hall Notes";
    private const string PlainTextImportText =
        """
        ## [Quarterly Town Hall|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        Plain text imports should become editor-ready TPS markdown documents.
        """;

    [Test]
    [Arguments("episode.tps")]
    [Arguments("episode.md")]
    [Arguments("episode.txt")]
    [Arguments("episode.pdf")]
    [Arguments("episode.docx")]
    [Arguments("episode.html")]
    public void CanImport_ReturnsTrue_ForSupportedPickerFileNames(string fileName)
    {
        var service = new ScriptDocumentImportService(new ScriptImportDescriptorService());

        Assert.True(service.CanImport(fileName));
    }

    [Test]
    [Arguments("")]
    [Arguments("episode")]
    [Arguments("episode.exe")]
    public void CanImport_ReturnsFalse_ForUnsupportedPickerFileNames(string fileName)
    {
        var service = new ScriptDocumentImportService(new ScriptImportDescriptorService());

        Assert.False(service.CanImport(fileName));
    }

    [Test]
    public async Task ImportAsync_PlainTextCanonicalizesDocumentName_AndPreservesBody()
    {
        var service = new ScriptDocumentImportService(new ScriptImportDescriptorService());
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(PlainTextImportText));

        var descriptor = await service.ImportAsync(stream, PlainTextFileName, "text/plain");

        Assert.Equal(PlainTextImportTitle, descriptor.Title);
        Assert.Equal(PlainTextImportDocumentName, descriptor.DocumentName);
        Assert.Equal(PlainTextImportText, descriptor.Text);
    }

    [Test]
    public async Task ImportAsync_NativeTpsPreservesDocumentName_AndText()
    {
        var service = new ScriptDocumentImportService(new ScriptImportDescriptorService());
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(NativeScriptText));

        var descriptor = await service.ImportAsync(stream, NativeScriptFileName, "text/plain");

        Assert.Equal("camera-check", descriptor.Title);
        Assert.Equal(NativeScriptFileName, descriptor.DocumentName);
        Assert.Equal(NativeScriptText, descriptor.Text);
    }

    [Test]
    public async Task ImportAsync_MarkdownImport_PreservesFrontMatterTitle_WhenProvided()
    {
        var service = new ScriptDocumentImportService(new ScriptImportDescriptorService());
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(FrontMatterMarkdownText));

        var descriptor = await service.ImportAsync(stream, FrontMatterMarkdownFileName, "text/markdown");

        Assert.Equal(FrontMatterMarkdownTitle, descriptor.Title);
        Assert.Equal("Quarterly Town Hall.tps.md", descriptor.DocumentName);
        Assert.Equal(FrontMatterMarkdownText, descriptor.Text);
    }

    [Test]
    public async Task ImportAsync_DocxConvertsToMarkdown_AndUsesFileStemTitle()
    {
        var service = new ScriptDocumentImportService(new ScriptImportDescriptorService());
        await using var stream = CreateDocxStream();

        var descriptor = await service.ImportAsync(stream, ImportedDocxFileName, DocxContentType);

        Assert.Equal(ImportedDocxTitle, descriptor.Title);
        Assert.Equal("Imported Design Review From File Name.tps.md", descriptor.DocumentName);
        Assert.Contains(ImportedHeading, descriptor.Text, StringComparison.Ordinal);
        Assert.Contains(ImportedParagraph, descriptor.Text, StringComparison.Ordinal);
    }

    private static MemoryStream CreateDocxStream()
    {
        var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(
                    CreateParagraph(ImportedHeading),
                    CreateParagraph(ImportedParagraph)));
        }

        stream.Position = 0;
        return stream;
    }

    private static Paragraph CreateParagraph(string text) =>
        new(new Run(new Text(text)));
}
