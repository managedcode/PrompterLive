using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Editor;

namespace PrompterOne.Core.Tests;

public sealed class ScriptImportDescriptorServiceTests
{
    private const string ImportedTitle = "System Design and Software Architecture for Vibe Coders";
    private const string ImportedBody =
        """
        ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        Services talk over real boundaries.
        """;

    private const string ImportedFrontMatterDocument =
        """
        ---
        title: "System Design and Software Architecture for Vibe Coders"
        profile: Actor
        duration: "145:00"
        base_wpm: 140
        author: "Konstantin Semenenko"
        created: "2026-03-25"
        version: "1.0"
        ---

        ## [Episode 2 - How Systems Talk to Each Other|140WPM|Professional]
        ### [Opening|140WPM|Warm]
        Services talk over real boundaries.
        """;

    [Test]
    [Arguments("episode.tps")]
    [Arguments("episode.tps.md")]
    [Arguments("episode.md.tps")]
    [Arguments("episode.md")]
    [Arguments("episode.txt")]
    [Arguments("EPISODE.TPS.MD")]
    [Arguments("nested/path/episode.tps")]
    [Arguments("C:\\scripts\\episode.md")]
    public void CanImport_ReturnsTrue_ForSupportedScriptFileNames(string fileName)
    {
        var service = new ScriptImportDescriptorService();

        Assert.True(service.CanImport(fileName));
    }

    [Test]
    [Arguments("")]
    [Arguments("episode")]
    [Arguments("episode.docx")]
    [Arguments("episode.tps.bak")]
    public void CanImport_ReturnsFalse_ForUnsupportedScriptFileNames(string fileName)
    {
        var service = new ScriptImportDescriptorService();

        Assert.False(service.CanImport(fileName));
    }

    [Test]
    public void Build_UsesFrontMatterTitle_AndPreservesOriginalSupportedDocumentName()
    {
        var service = new ScriptImportDescriptorService();

        var descriptor = service.Build("system-design.md.tps", ImportedFrontMatterDocument);

        Assert.Equal(ImportedTitle, descriptor.Title);
        Assert.Equal("system-design.md.tps", descriptor.DocumentName);
        Assert.Equal(ImportedFrontMatterDocument, descriptor.Text);
    }

    [Test]
    public void Build_UsesCompoundFileStem_WhenFrontMatterTitleIsMissing()
    {
        var service = new ScriptImportDescriptorService();

        var descriptor = service.Build("Episode 2 - Systems Talk.tps.md", ImportedBody);

        Assert.Equal("Episode 2 - Systems Talk", descriptor.Title);
        Assert.Equal("Episode 2 - Systems Talk.tps.md", descriptor.DocumentName);
        Assert.Equal(ImportedBody, descriptor.Text);
    }

    [Test]
    public void Build_StripsDirectorySegments_BeforePreservingSupportedDocumentName()
    {
        var service = new ScriptImportDescriptorService();

        var descriptor = service.Build("nested/scripts/System Design.md.tps", ImportedFrontMatterDocument);

        Assert.Equal(ImportedTitle, descriptor.Title);
        Assert.Equal("System Design.md.tps", descriptor.DocumentName);
    }

    [Test]
    [Arguments(".md")]
    [Arguments(".txt")]
    [Arguments(".tps")]
    public void Build_UsesUntitledScriptTitle_WhenSupportedFileStemIsEmpty(string fileName)
    {
        var service = new ScriptImportDescriptorService();

        var descriptor = service.Build(fileName, ImportedBody);

        Assert.Equal(ScriptWorkspaceState.UntitledScriptTitle, descriptor.Title);
        Assert.Equal(fileName, descriptor.DocumentName);
    }

    [Test]
    [Arguments("")]
    [Arguments("episode")]
    [Arguments("episode.docx")]
    public void Build_ThrowsArgumentException_ForUnsupportedFileName(string fileName)
    {
        var service = new ScriptImportDescriptorService();

        var exception = Assert.Throws<ArgumentException>(() => service.Build(fileName, ImportedBody));

        Assert.Equal("fileName", exception.ParamName);
    }
}
