using PrompterLive.Core.Services;
using PrompterLive.Core.Services.Preview;
using PrompterLive.Core.Services.Samples;
using PrompterLive.Core.Services.Workspace;

namespace PrompterLive.Core.Tests;

public sealed class ScriptSessionServiceTests
{
    [Fact]
    public async Task InitializeAsync_SeedsLibraryAndBuildsStarterDraft()
    {
        var repository = new InMemoryScriptRepository();
        var session = CreateSession(repository);

        await session.InitializeAsync();

        var library = await repository.ListAsync();

        Assert.True(library.Count >= 2);
        Assert.Equal("Fresh Take", session.State.Title);
        Assert.Equal("fresh-take.tps", session.State.DocumentName);
        Assert.True(session.State.WordCount > 0);
        Assert.NotEmpty(session.State.PreviewSegments);
        Assert.Null(session.State.ErrorMessage);
    }

    [Fact]
    public async Task LoadSampleAsync_UsesEmbeddedDemoScript()
    {
        var repository = new InMemoryScriptRepository();
        var session = CreateSession(repository);

        await session.InitializeAsync();
        await session.LoadSampleAsync(SampleScriptCatalog.DemoSampleId);

        Assert.Equal(SampleScriptCatalog.DemoSampleId, session.State.ScriptId);
        Assert.Equal("RSVP Technology Demo", session.State.Title);
        Assert.Contains(session.State.PreviewSegments, segment => segment.Title == "Intro");
        Assert.Contains(session.State.PreviewSegments, segment => segment.Title == "Call to Action");
        Assert.True(session.State.EstimatedDuration > TimeSpan.Zero);
    }

    [Fact]
    public async Task SaveAsync_PersistsDraftAndReloadsSavedDocument()
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
