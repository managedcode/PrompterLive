using Bunit;
using PrompterLive.Core.Services.Samples;
using PrompterLive.Shared.Components.Library;
using PrompterLive.Shared.Pages;
using PrompterLive.Shared.Services.Library;
using PrompterLive.Shared.Tests;

namespace PrompterLive.App.Tests;

public sealed class LibraryFolderInteractionTests : BunitContext
{
    private readonly AppHarness _harness;

    public LibraryFolderInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Fact]
    public async Task LibraryPage_CreatesFolderInsideSelectedParent()
    {
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.Find("[data-testid='library-folder-create-tile']").Click();
        cut.Find("[data-testid='library-new-folder-name']").Input("Roadshows");
        cut.Find("[data-testid='library-new-folder-parent']").Change(SampleLibraryFolderCatalog.PresentationsFolderId);
        cut.Find("[data-testid='library-new-folder-submit']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Roadshows", cut.Markup);
            Assert.Contains("library-folder-roadshows", cut.Markup);
        });

        var createdFolder = (await _harness.FolderRepository.ListAsync())
            .Single(folder => folder.Name == "Roadshows");

        Assert.Equal(SampleLibraryFolderCatalog.PresentationsFolderId, createdFolder.ParentId);
    }

    [Fact]
    public async Task LibraryPage_MovesScriptIntoFolder_AndUpdatesVisibleCards()
    {
        var roadshowsFolder = await _harness.FolderRepository.CreateAsync(
            "Roadshows",
            SampleLibraryFolderCatalog.PresentationsFolderId);
        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() => Assert.Contains("Product Launch", cut.Markup));

        cut.Find("[data-testid='library-card-menu-rsvp-tech-demo']").Click();
        cut.Find($"[data-testid='library-move-rsvp-tech-demo-{roadshowsFolder.Id}']").Click();

        cut.WaitForAssertion(() =>
        {
            var document = _harness.Repository.GetAsync(SampleScriptCatalog.DemoSampleId).GetAwaiter().GetResult();
            Assert.Equal(roadshowsFolder.Id, document?.FolderId);
        });

        cut.Find($"[data-testid='library-folder-{roadshowsFolder.Id}']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Product Launch", cut.Markup);
            Assert.DoesNotContain("Security Incident", cut.Markup);
            Assert.Contains("library-folder-roadshows", cut.Markup);
        });
    }

    [Fact]
    public async Task LibraryPage_RestoresPersistedFolderSelectionAfterReload()
    {
        await _harness.FolderRepository.InitializeAsync(SampleLibraryFolderCatalog.CreateSeedFolders());
        await _harness.Repository.InitializeAsync(SampleScriptCatalog.CreateSeedDocuments());
        var roadshowsFolder = await _harness.FolderRepository.CreateAsync(
            "Roadshows",
            SampleLibraryFolderCatalog.PresentationsFolderId);
        await _harness.Repository.MoveToFolderAsync(SampleScriptCatalog.DemoSampleId, roadshowsFolder.Id);
        _harness.JsRuntime.SavedValues["prompterlive.library"] = new LibraryViewState(
            SelectedFolderId: roadshowsFolder.Id,
            SortMode: LibrarySortMode.Date,
            ExpandedFolderIds: [SampleLibraryFolderCatalog.PresentationsFolderId, roadshowsFolder.Id]);

        var cut = Render<LibraryPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Product Launch", cut.Markup);
            Assert.DoesNotContain("Security Incident", cut.Markup);
            Assert.Contains("library-folder-roadshows", cut.Markup);
            Assert.Contains("active", cut.Find("[data-testid='library-sort-date']").ClassName);
        });
    }
}
