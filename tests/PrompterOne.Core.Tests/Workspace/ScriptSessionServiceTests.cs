using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Preview;
using PrompterOne.Core.Services.Workspace;

namespace PrompterOne.Core.Tests;

public sealed class ScriptSessionServiceTests
{
    private const string UntitledScriptDocumentName = "untitled-script.tps";
    private const string UntitledScriptTitle = "Untitled Script";

    [Fact]
    public async Task InitializeAsync_DoesNotSeedLibraryAndBuildsEmptyDraft()
    {
        var repository = new InMemoryScriptRepository();
        var session = CreateSession(repository);

        await session.InitializeAsync();

        var library = await repository.ListAsync();

        Assert.Empty(library);
        Assert.Equal(UntitledScriptTitle, session.State.Title);
        Assert.Equal(UntitledScriptDocumentName, session.State.DocumentName);
        Assert.Equal(string.Empty, session.State.Text);
        Assert.Equal(0, session.State.WordCount);
        Assert.Empty(session.State.PreviewSegments);
        Assert.Null(session.State.ErrorMessage);
    }

    [Fact]
    public async Task NewAsync_CreatesEmptyUntitledDraft()
    {
        var repository = new InMemoryScriptRepository();
        var session = CreateSession(repository);

        await session.InitializeAsync();
        await session.NewAsync();

        Assert.Equal(string.Empty, session.State.ScriptId);
        Assert.Equal(UntitledScriptTitle, session.State.Title);
        Assert.Equal(string.Empty, session.State.Text);
        Assert.Equal(UntitledScriptDocumentName, session.State.DocumentName);
        Assert.Equal(0, session.State.WordCount);
        Assert.Empty(session.State.PreviewSegments);
        Assert.Null(session.State.ErrorMessage);
    }

    [Fact]
    public async Task OpenAsync_LoadsProvidedDocument()
    {
        var repository = new InMemoryScriptRepository();
        var session = CreateSession(repository);
        var document = CoreTestSeedData.CreateDocuments()
            .Single(item => string.Equals(item.Id, CoreTestSeedData.Scripts.DemoId, StringComparison.Ordinal));

        await session.InitializeAsync();
        await session.OpenAsync(document);

        Assert.Equal(CoreTestSeedData.Scripts.DemoId, session.State.ScriptId);
        Assert.Equal("Product Launch", session.State.Title);
        Assert.Contains(session.State.PreviewSegments, segment => segment.Title == "Intro");
        Assert.Contains(session.State.PreviewSegments, segment => segment.Title == "Call to Action");
        Assert.True(session.State.EstimatedDuration > TimeSpan.Zero);
    }

    [Fact]
    public async Task SaveAsync_PersistsDraftAndKeepsCurrentSessionState()
    {
        var repository = new InMemoryScriptRepository();
        var session = CreateSession(repository);

        await session.InitializeAsync();
        await session.UpdateDraftAsync(
            "Camera Check",
            """
            ---
            title: "Camera Check"
            base_wpm: 145
            ---

            ## [Open|145WPM|focused]
            ### [Slate|145WPM]
            Camera one is live and audio delay is calibrated.
            """);

        var saved = await session.SaveAsync();
        var stored = await repository.GetAsync(saved.Id);

        Assert.NotNull(stored);
        Assert.Equal(saved.Id, session.State.ScriptId);
        Assert.Equal("Camera Check", stored.Title);
        Assert.Equal("camera-check.tps", stored.DocumentName);
        Assert.Contains(session.State.PreviewSegments, segment => segment.Title == "Open");
    }

    private static ScriptSessionService CreateSession(InMemoryScriptRepository repository)
    {
        var parser = new TpsParser();
        var compiler = new ScriptCompiler();
        var previewService = new ScriptPreviewService(parser, compiler);
        return new ScriptSessionService(repository, parser, compiler, previewService);
    }
}
